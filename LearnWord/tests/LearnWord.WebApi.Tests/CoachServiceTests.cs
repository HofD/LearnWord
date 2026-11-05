using System.Text.Json;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.BL.Services;
using LearnWord.DAL.Models;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Options;
using LearnWord.WebApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace LearnWord.WebApi.Tests;

public class CoachServiceTests
{
    private static CoachService Service(CoachTestDatabase db, IAiCoachProvider? provider = null, bool enabled = true)
        => new(db.Context, new CoachContextBuilder(db.Context, new LearnerStateCalculator()), provider ?? new FakeAiCoachProvider(),
            OptionsFactory.Create(new AiCoachOptions { Enabled = enabled }), NullLogger<CoachService>.Instance);

    [Fact]
    public async Task NewPromptVersionDoesNotReuseOldNoneAndKeepsHistoricalAnalysis()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var old = new CoachAnalysis { Id = Guid.NewGuid(), SessionId = session.Id, CollectionId = session.CollectionId,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Status = "Completed", Decision = "None",
            Generation = 1, PromptVersion = "coach-v4" };
        db.Context.CoachAnalyses.Add(old);
        await db.Context.SaveChangesAsync();
        var provider = new RecordingProvider();
        var service = Service(db, provider);
        var result = await service.Analyze(session.Id, new());
        Assert.Equal("coach-v5", result.PromptVersion);
        Assert.NotEqual(old.Id, result.Id);
        Assert.Equal("Intervention", result.Decision);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(result.Id, (await service.Analyze(session.Id, new())).Id);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(2, await db.Context.CoachAnalyses.CountAsync());
        Assert.Equal("None", (await db.Context.CoachAnalyses.SingleAsync(a => a.Id == old.Id)).Decision);
    }

    [Fact]
    public async Task CompletedAnalysisIsDurableAndDoesNotPayForRepeatedGeneration()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new RecordingProvider();
        var service = Service(db, provider);
        var first = await service.Analyze(session.Id, new AnalyzeCoachRequest { Language = "en" });
        db.Context.ChangeTracker.Clear();
        var again = await service.Analyze(session.Id, new AnalyzeCoachRequest { Language = "ru" });
        var read = await service.Analysis(session.Id);
        Assert.Equal("Completed", first.Status);
        Assert.Equal("Intervention", first.Decision);
        Assert.Equal(first.Id, again.Id);
        Assert.Equal(first.Id, read.Id);
        Assert.Equal(first.CreatedAt, read.CreatedAt);
        Assert.Equal(1, provider.Calls);
        Assert.Single(await db.Context.Set<CoachAnalysis>().ToListAsync());
        Assert.Equal(first.Interventions.Select(i => i.Id), read.Interventions.Select(i => i.Id));
        var stored = await db.Context.Set<CoachAnalysis>().SingleAsync();
        Assert.Equal(provider.Provider, stored.Provider);
        Assert.Equal(provider.Model, stored.Model);
        Assert.Equal(CoachLimits.PromptVersion, stored.PromptVersion);
        Assert.Equal(CoachLimits.SchemaVersion, stored.SchemaVersion);
    }

    [Fact]
    public async Task InsufficientEvidenceReturnsNoneWithoutCallingProviderOrChangingLearning()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed(failures: 1);
        var provider = new RecordingProvider();
        var result = await Service(db, provider).Analyze(session.Id, new());
        Assert.Equal("Completed", result.Status);
        Assert.Equal("None", result.Decision);
        Assert.Empty(result.Interventions);
        Assert.Equal(0, provider.Calls);
        Assert.Equal(1, await db.Context.LearningAttempts.CountAsync());
        Assert.NotNull((await db.Context.LearningSessions.SingleAsync()).CompletedAt);
    }

    [Fact]
    public async Task ProviderNoneIsAcceptedWithoutInventingAdvice()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var result = await Service(db, new RecordingProvider { Response = "{\"decision\":\"None\",\"summary\":\"No persistent issue.\",\"observations\":[],\"interventions\":[]}" }).Analyze(session.Id, new());
        Assert.Equal("Completed", result.Status);
        Assert.Equal("None", result.Decision);
        Assert.Empty(result.Observations);
        Assert.Empty(await db.Context.Set<CoachIntervention>().ToListAsync());
    }

    [Fact]
    public async Task DisabledCoachNeverCallsProviderAndLeavesCompletedSessionSuccessful()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new RecordingProvider();
        var result = await Service(db, provider, enabled: false).Analyze(session.Id, new());
        Assert.Equal("Disabled", result.Status);
        Assert.Equal(0, provider.Calls);
        Assert.NotNull((await db.Context.LearningSessions.SingleAsync()).CompletedAt);
        Assert.Empty(await db.Context.Set<CoachIntervention>().ToListAsync());
        Assert.Empty(await db.Context.Set<CoachAnalysis>().ToListAsync());
        var disabledRead = await Service(db, provider, enabled: false).Analysis(session.Id);
        Assert.Equal("Disabled", disabledRead.Status);
        Assert.Empty(disabledRead.Interventions);
        Assert.Equal(0, provider.Calls);
        var enabled = await Service(db, provider).Analyze(session.Id, new());
        Assert.Equal("Completed", enabled.Status);
        Assert.Equal("Intervention", enabled.Decision);
        Assert.Equal(1, provider.Calls);
        Assert.Single(await db.Context.Set<CoachAnalysis>().ToListAsync());
    }

    [Fact]
    public async Task FeatureFlagOffSuppressesCachedAdviceWithoutDestroyingItAndReenableReusesIt()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new RecordingProvider();
        var original = await Service(db, provider).Analyze(session.Id, new());
        var interventionIds = original.Interventions.Select(i => i.Id).ToArray();
        Assert.NotEmpty(interventionIds);
        var disabled = Service(db, provider, enabled: false);
        foreach (var result in new[] { await disabled.Analyze(session.Id, new()), await disabled.Analysis(session.Id) })
        {
            Assert.Equal("Disabled", result.Status);
            Assert.Equal("None", result.Decision);
            Assert.Empty(result.Summary);
            Assert.Empty(result.Observations);
            Assert.Empty(result.Interventions);
        }
        Assert.Equal(1, provider.Calls);
        Assert.Equal("Completed", (await db.Context.Set<CoachAnalysis>().SingleAsync()).Status);
        Assert.Equal(interventionIds.Length, await db.Context.Set<CoachIntervention>().CountAsync());
        var resumed = await Service(db, provider).Analyze(session.Id, new());
        Assert.Equal(original.Id, resumed.Id);
        Assert.Equal("Completed", resumed.Status);
        Assert.Equal(interventionIds, resumed.Interventions.Select(i => i.Id));
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task LegacyPersistedDisabledAnalysisCanGenerateAfterReenableWithoutUsingRetryAllowance()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var legacy = new CoachAnalysis { Id = Guid.NewGuid(), SessionId = session.Id, CollectionId = session.CollectionId,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5), UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            Status = "Disabled", Generation = 1, RetryCount = 0, PromptVersion = CoachLimits.PromptVersion };
        db.Context.Set<CoachAnalysis>().Add(legacy);
        await db.Context.SaveChangesAsync();
        var provider = new RecordingProvider();
        var resumed = await Service(db, provider).Analyze(session.Id, new());
        Assert.Equal(legacy.Id, resumed.Id);
        Assert.Equal("Completed", resumed.Status);
        Assert.Equal(1, provider.Calls);
        var stored = await db.Context.Set<CoachAnalysis>().SingleAsync();
        Assert.Equal(2, stored.Generation);
        Assert.Equal(0, stored.RetryCount);
    }

    [Theory]
    [InlineData("ai_provider_rate_limited")]
    [InlineData("ai_provider_unavailable")]
    [InlineData("ai_provider_configuration_error")]
    [InlineData("ai_provider_invalid_response")]
    public async Task ProviderFailuresArePersistedAsNoncriticalAndRetryIsControlled(string failureCode)
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new RecordingProvider { Failure = new UpstreamServiceException("secret diagnostic", failureCode) };
        var service = Service(db, provider);
        var failed = await service.Analyze(session.Id, new());
        Assert.Equal("Failed", failed.Status);
        Assert.Equal(failureCode, failed.FailureCode);
        Assert.Empty(failed.Interventions);
        Assert.NotNull((await db.Context.LearningSessions.SingleAsync()).CompletedAt);
        await service.Analyze(session.Id, new());
        Assert.Equal(1, provider.Calls);
        var stored = await db.Context.Set<CoachAnalysis>().SingleAsync();
        stored.UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(-CoachLimits.RetrySeconds - 1);
        await db.Context.SaveChangesAsync();
        provider.Failure = null;
        var retried = await service.Analyze(session.Id, new());
        Assert.Equal(failed.Id, retried.Id);
        Assert.Equal("Completed", retried.Status);
        Assert.Equal(2, provider.Calls);
        Assert.DoesNotContain("secret diagnostic", JsonSerializer.Serialize(retried));
    }

    [Theory]
    [InlineData("not JSON")]
    [InlineData("{\"decision\":\"Intervention\",\"summary\":\"\",\"observations\":[],\"interventions\":[]}")]
    public async Task InvalidOutputCannotPersistPartialInterventionsOrMutateLearnerEvidence(string response)
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var before = await Snapshot(db);
        var result = await Service(db, new RecordingProvider { Response = response }).Analyze(session.Id, new());
        Assert.Equal("Failed", result.Status);
        Assert.Empty(await db.Context.Set<CoachIntervention>().ToListAsync());
        Assert.Equal(before, await Snapshot(db));
    }

    [Fact]
    public async Task PendingAnalysisIsReusedWithoutStartingDuplicateCall()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new RecordingProvider();
        var pending = new CoachAnalysis { Id = Guid.NewGuid(), SessionId = session.Id, CollectionId = session.CollectionId,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, Status = "Pending", Generation = 1, PromptVersion = CoachLimits.PromptVersion };
        db.Context.Set<CoachAnalysis>().Add(pending);
        await db.Context.SaveChangesAsync();
        var result = await Service(db, provider).Analyze(session.Id, new());
        Assert.Equal(pending.Id, result.Id);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(0, provider.Calls);
        Assert.Single(await db.Context.Set<CoachAnalysis>().ToListAsync());
    }

    [Fact]
    public async Task ExpiredPendingLeaseCanBeRecoveredWithoutCreatingAnotherAnalysis()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new RecordingProvider();
        var pending = new CoachAnalysis { Id = Guid.NewGuid(), SessionId = session.Id, CollectionId = session.CollectionId,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10), UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(-CoachLimits.LeaseSeconds - 1),
            Status = "Pending", Generation = 1, PromptVersion = CoachLimits.PromptVersion };
        db.Context.Set<CoachAnalysis>().Add(pending);
        await db.Context.SaveChangesAsync();
        var result = await Service(db, provider).Analyze(session.Id, new());
        Assert.Equal(pending.Id, result.Id);
        Assert.Equal("Completed", result.Status);
        Assert.Equal(1, provider.Calls);
        var stored = await db.Context.Set<CoachAnalysis>().SingleAsync();
        Assert.Equal(2, stored.Generation);
        Assert.Equal(1, stored.RetryCount);
        Assert.Equal(2, await db.Context.Set<CoachIntervention>().CountAsync());
    }

    [Fact]
    public async Task StaleGenerationWorkerCannotCommitAnOlderResult()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new RecordingProvider {
            BeforeResult = async _ => {
                // Simulate another worker claiming an expired lease while the first provider call is in flight.
                var claimed = await db.Context.Set<CoachAnalysis>().SingleAsync();
                claimed.Generation++;
                claimed.UpdatedAt = DateTimeOffset.UtcNow;
                await db.Context.SaveChangesAsync();
            }
        };
        var service = Service(db, provider);
        var result = await service.Analyze(session.Id, new());
        Assert.Equal("Pending", result.Status);
        Assert.Empty(result.Interventions);
        Assert.Empty(await db.Context.Set<CoachIntervention>().ToListAsync());
        Assert.Equal(2, (await db.Context.Set<CoachAnalysis>().SingleAsync()).Generation);
        await service.Analyze(session.Id, new());
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task RetryExhaustionPreventsUnlimitedPaidProviderCalls()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new RecordingProvider();
        var failed = new CoachAnalysis { Id = Guid.NewGuid(), SessionId = session.Id, CollectionId = session.CollectionId,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10), UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            Status = "Failed", Generation = CoachLimits.MaxRetries + 1, RetryCount = CoachLimits.MaxRetries,
            FailureCode = "ai_provider_unavailable", PromptVersion = CoachLimits.PromptVersion };
        db.Context.Set<CoachAnalysis>().Add(failed);
        await db.Context.SaveChangesAsync();
        var result = await Service(db, provider).Analyze(session.Id, new());
        Assert.Equal(failed.Id, result.Id);
        Assert.Equal("Failed", result.Status);
        Assert.Equal("ai_provider_unavailable", result.FailureCode);
        Assert.Equal(0, provider.Calls);
        Assert.Single(await db.Context.Set<CoachAnalysis>().ToListAsync());
    }

    [Fact]
    public async Task CardEditDuringProviderCallRejectsEntireResultAsStale()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new RecordingProvider {
            BeforeResult = async _ => {
                var card = await db.Context.Cards.SingleAsync();
                card.LearningRevision++;
                await db.Context.SaveChangesAsync();
            }
        };
        var result = await Service(db, provider).Analyze(session.Id, new());
        Assert.Equal("Failed", result.Status);
        Assert.Equal("coach_context_stale", result.FailureCode);
        Assert.Empty(result.Interventions);
        Assert.Empty(await db.Context.Set<CoachIntervention>().ToListAsync());
        Assert.Empty(await db.Context.Set<InterventionAttempt>().ToListAsync());
        Assert.Equal(2, await db.Context.LearningAttempts.CountAsync());
        Assert.NotNull((await db.Context.LearningSessions.SingleAsync()).CompletedAt);
    }

    [Fact]
    public async Task LessonAcceptAndCompleteAreDurableIdempotentAndLeaveSrsUntouched()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var service = Service(db);
        var analysis = await service.Analyze(session.Id, new());
        var lesson = analysis.Interventions.Single(i => i.Type == "MicroLesson");
        var before = await Snapshot(db);
        var accepted = await service.Accept(lesson.Id);
        var twice = await service.Accept(lesson.Id);
        Assert.Equal("Accepted", accepted.Status);
        Assert.Equal(accepted.StartedAt, twice.StartedAt);
        var complete = await service.Complete(lesson.Id);
        var completeAgain = await service.Complete(lesson.Id);
        Assert.Equal("Completed", complete.Status);
        Assert.Equal(complete.CompletedAt, completeAgain.CompletedAt);
        Assert.Equal("NoFollowUpEvidence", complete.Evaluation.Status);
        Assert.Equal(before, await Snapshot(db));
        Assert.Empty(await db.Context.Set<InterventionAttempt>().ToListAsync());
    }

    [Fact]
    public async Task DismissedInterventionCannotBeStartedOrCompleted()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var service = Service(db);
        var lesson = (await service.Analyze(session.Id, new())).Interventions.Single(i => i.Type == "MicroLesson");
        Assert.Equal("Dismissed", (await service.Dismiss(lesson.Id)).Status);
        Assert.Equal("Dismissed", (await service.Dismiss(lesson.Id)).Status);
        await Assert.ThrowsAsync<ConflictException>(() => service.Accept(lesson.Id));
        await Assert.ThrowsAsync<ConflictException>(() => service.Complete(lesson.Id));
    }

    [Fact]
    public async Task LessonCannotCompleteBeforeUserAcceptsAndPracticeCannotUseLessonCompleteEndpoint()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var service = Service(db);
        var analysis = await service.Analyze(session.Id, new());
        await Assert.ThrowsAsync<ConflictException>(() => service.Complete(analysis.Interventions.Single(i => i.Type == "MicroLesson").Id));
        var practice = analysis.Interventions.Single(i => i.Type == "ExtraPractice");
        await service.Accept(practice.Id);
        await Assert.ThrowsAsync<ConflictException>(() => service.Complete(practice.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PracticeAnswersAreSeparateEvidenceAndNeverMutateSrsOrLearnerModel(bool correct)
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var service = Service(db);
        var practice = (await service.Analyze(session.Id, new())).Interventions.Single(i => i.Type == "ExtraPractice");
        var before = await Snapshot(db);
        var accepted = await service.Accept(practice.Id);
        Assert.DoesNotContain("correctOptionId", JsonSerializer.Serialize(accepted, CoachOutputValidator.Json));
        Assert.DoesNotContain("explanation", JsonSerializer.Serialize(accepted.Exercises, CoachOutputValidator.Json));
        var payload = JsonSerializer.Deserialize<CoachInterventionResult>((await db.Context.Set<CoachIntervention>().SingleAsync(i => i.Id == practice.Id)).PayloadJson, CoachOutputValidator.Json)!;
        CoachAnswerDto? result = null;
        foreach (var exercise in accepted.Exercises)
        {
            var expected = payload.Exercises.Single(e => e.Id == exercise.Id).CorrectOptionId;
            result = await service.Answer(practice.Id, exercise.Id, new CoachAnswerRequest { OptionId = correct ? expected : exercise.Options.First(o => o.Id != expected).Id, ResponseTimeMs = 1200 });
            Assert.Equal(correct, result.Correct);
            Assert.Equal(correct, result.Intervention.Exercises.Single(e => e.Id == exercise.Id).Correct);
        }
        Assert.NotNull(result);
        Assert.Equal("Completed", result!.Intervention.Status);
        Assert.Equal(accepted.Exercises.Count, await db.Context.Set<InterventionAttempt>().CountAsync());
        Assert.All(await db.Context.Set<InterventionAttempt>().ToListAsync(), a => { Assert.Equal(correct, a.Correct); Assert.Equal(1200, a.ResponseTimeMs); });
        Assert.Equal(before, await Snapshot(db));
    }

    [Fact]
    public async Task PracticeRequiresAcceptanceValidExerciseAndValidOption()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var service = Service(db);
        var practice = (await service.Analyze(session.Id, new())).Interventions.Single(i => i.Type == "ExtraPractice");
        var exercise = practice.Exercises[0];
        await Assert.ThrowsAsync<ConflictException>(() => service.Answer(practice.Id, exercise.Id, new() { OptionId = "a" }));
        await service.Accept(practice.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => service.Answer(practice.Id, Guid.NewGuid(), new() { OptionId = "a" }));
        await Assert.ThrowsAsync<BadRequestException>(() => service.Answer(practice.Id, exercise.Id, new() { OptionId = "unknown" }));
        await Assert.ThrowsAsync<BadRequestException>(() => service.Answer(practice.Id, exercise.Id, new() { OptionId = "a", ResponseTimeMs = -1 }));
        Assert.Empty(await db.Context.Set<InterventionAttempt>().ToListAsync());
    }

    [Fact]
    public async Task AnswerRetryReusesStoredResultButChangedAnswerConflicts()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var service = Service(db);
        var practice = (await service.Analyze(session.Id, new())).Interventions.Single(i => i.Type == "ExtraPractice");
        await service.Accept(practice.Id);
        var exercise = practice.Exercises[0];
        var first = await service.Answer(practice.Id, exercise.Id, new() { OptionId = "a", ResponseTimeMs = 100 });
        var retry = await service.Answer(practice.Id, exercise.Id, new() { OptionId = "a", ResponseTimeMs = 100 });
        Assert.Equal(first.Correct, retry.Correct);
        Assert.Single(await db.Context.Set<InterventionAttempt>().ToListAsync());
        await Assert.ThrowsAsync<ConflictException>(() => service.Answer(practice.Id, exercise.Id, new() { OptionId = "b", ResponseTimeMs = 100 }));
    }

    [Fact]
    public async Task CardRevisionChangeMakesSavedAdviceStaleAndBlocksAllMutations()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var service = Service(db);
        var analysis = await service.Analyze(session.Id, new());
        var storedCard = await db.Context.Cards.SingleAsync();
        storedCard.LearningRevision++;
        await db.Context.SaveChangesAsync();
        foreach (var intervention in analysis.Interventions)
        {
            var stale = await service.Intervention(intervention.Id);
            Assert.Equal("Stale", stale.Status);
            Assert.Null(stale.Lesson);
            Assert.Empty(stale.Exercises);
            await Assert.ThrowsAsync<ConflictException>(() => service.Accept(intervention.Id));
        }
        Assert.Empty(await db.Context.Set<InterventionAttempt>().ToListAsync());
    }

    [Fact]
    public async Task DeletedCardOrCollectionCannotReturnOldContentOrAcceptWork()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var service = Service(db);
        var analysis = await service.Analyze(session.Id, new());
        var card = await db.Context.Cards.SingleAsync();
        card.DeletedAt = DateTimeOffset.UtcNow;
        await db.Context.SaveChangesAsync();
        var intervention = await service.Intervention(analysis.Interventions[0].Id);
        Assert.Equal("Unavailable", intervention.Status);
        Assert.Null(intervention.Lesson);
        Assert.Empty(intervention.Exercises);
        await Assert.ThrowsAsync<NotFoundException>(() => service.Accept(intervention.Id));
        var collection = await db.Context.Collections.SingleAsync();
        collection.DeletedAt = DateTimeOffset.UtcNow;
        await db.Context.SaveChangesAsync();
        await Assert.ThrowsAsync<NotFoundException>(() => service.Analysis(session.Id));
    }

    [Theory]
    [InlineData(ReviewOutcome.Good, "FollowUpSuccess")]
    [InlineData(ReviewOutcome.Again, "FollowUpFailure")]
    public async Task EvaluationUsesOnlyRealCurrentRevisionNormalAttemptsAfterCompletion(ReviewOutcome outcome, string status)
    {
        await using var db = await CoachTestDatabase.Create();
        var (card, session) = await db.Seed();
        var service = Service(db);
        var lesson = (await service.Analyze(session.Id, new())).Interventions.Single(i => i.Type == "MicroLesson");
        await service.Accept(lesson.Id);
        var done = await service.Complete(lesson.Id);
        db.Context.LearningAttempts.Add(new LearningAttempt { Id = Guid.NewGuid(), CardId = card.Id, LearningRevision = card.LearningRevision + 1,
            Outcome = ReviewOutcome.Easy, ReviewedAt = done.CompletedAt!.Value.AddSeconds(1), PreviousDueDate = db.Now, NewDueDate = db.Now });
        await db.Context.SaveChangesAsync();
        Assert.Equal("NoFollowUpEvidence", (await service.Intervention(lesson.Id)).Evaluation.Status);
        var followUp = new LearningAttempt { Id = Guid.NewGuid(), CardId = card.Id, LearningRevision = card.LearningRevision,
            Outcome = outcome, ReviewedAt = done.CompletedAt.Value.AddSeconds(2), PreviousDueDate = db.Now, NewDueDate = db.Now };
        db.Context.LearningAttempts.Add(followUp);
        await db.Context.SaveChangesAsync();
        var evaluation = (await service.Intervention(lesson.Id)).Evaluation;
        Assert.Equal(status, evaluation.Status);
        Assert.Equal(1, evaluation.PostInterventionAttempts);
        Assert.Equal(outcome.ToString(), evaluation.FirstPostInterventionOutcome);
        Assert.Equal(followUp.ReviewedAt, evaluation.FirstPostInterventionReviewedAt);
    }

    private static async Task<string> Snapshot(CoachTestDatabase db)
    {
        db.Context.ChangeTracker.Clear();
        var card = await db.Context.Cards.AsNoTracking().SingleAsync();
        var state = await db.Context.LearnerCardStates.AsNoTracking().SingleAsync();
        var attempts = await db.Context.LearningAttempts.AsNoTracking().OrderBy(a => a.ReviewedAt).ToListAsync();
        return JsonSerializer.Serialize(new { card.DueDate, card.IntervalDays, card.EaseFactor, card.ReviewCount, card.LastReviewedAt, card.LearningRevision, State = state, Attempts = attempts });
    }

    private sealed class RecordingProvider : IAiCoachProvider
    {
        public string Provider => "Fake";
        public string Model => "test-coach-v1";
        public int Calls { get; private set; }
        public string? Response { get; set; }
        public Exception? Failure { get; set; }
        public Func<CoachSessionContext, Task>? BeforeResult { get; set; }
        public async Task<string> Analyze(CoachSessionContext context, CancellationToken cancellationToken)
        {
            Calls++;
            if (Failure != null) throw Failure;
            if (BeforeResult != null) await BeforeResult(context);
            return Response == null ? await new FakeAiCoachProvider().Analyze(context, cancellationToken) : Response;
        }
    }
}

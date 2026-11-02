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

public class CoachDiagnosticsTests
{
    private static CoachContextBuilder Builder(CoachTestDatabase db) => new(db.Context, new LearnerStateCalculator());
    private static CoachService Service(CoachTestDatabase db, DiagnosticProvider provider, bool enabled = true)
        => new(db.Context, Builder(db), provider, OptionsFactory.Create(new AiCoachOptions { Enabled = enabled }), NullLogger<CoachService>.Instance);
    private static string Json(object value) => JsonSerializer.Serialize(value, CoachOutputValidator.Json);

    [Theory]
    [InlineData("Included")]
    [InlineData("RevisionMismatch")]
    [InlineData("InsufficientEvidence")]
    [InlineData("NoSessionAttempt")]
    [InlineData("SessionNotProblematic")]
    [InlineData("ContentUnavailable")]
    [InlineData("ContentTooLarge")]
    public async Task SelectionDiagnostics_ExplainActualEligibilityWithoutChangingProviderContract(string reason)
    {
        await using var db = await CoachTestDatabase.Create();
        var (card, session) = await db.Seed(failures: reason == "InsufficientEvidence" ? 1 : 2);
        switch (reason)
        {
            case "RevisionMismatch": (await db.Context.Cards.SingleAsync()).LearningRevision++; break;
            case "NoSessionAttempt": (await db.Context.LearningAttempts.SingleAsync(a => a.SessionId == session.Id)).SessionId = null; break;
            case "SessionNotProblematic":
                var attempt = await db.Context.LearningAttempts.SingleAsync(a => a.SessionId == session.Id);
                attempt.Outcome = ReviewOutcome.Good; attempt.ResponseTimeMs = 1200; break;
            case "ContentUnavailable": (await db.Context.Cards.SingleAsync()).DeletedAt = db.Now; break;
            case "ContentTooLarge": (await db.Context.Words.SingleAsync()).Value = new string('x', CoachLimits.ShortText + 1); break;
        }
        await db.Context.SaveChangesAsync();
        var saveCalls = db.SaveCalls;
        var result = await Builder(db).BuildWithDiagnostics(session.Id, "en");
        var diagnostic = Assert.Single(result.DiagnosticsContext.Cards);
        Assert.Equal(card.Id, diagnostic.CardId);
        Assert.Equal(card.LearningRevision, diagnostic.SessionRevision);
        Assert.Equal(reason, diagnostic.SelectionReason);
        Assert.Equal(reason == "Included" ? 1 : 0, result.DiagnosticsContext.CandidateCount);
        Assert.Equal(reason == "Included" ? 1 : 0, result.Context.Cards.Count);
        Assert.True(diagnostic.RecentAttempts.Count <= CoachLimits.RecentAttempts);
        Assert.Equal(saveCalls, db.SaveCalls);
        Assert.DoesNotContain("selectionReason", Json(result.Context));
        Assert.DoesNotContain("diagnostics", Json(result.Context), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CandidateLimitMarksOnlyEligibleOverflowAndDoesNotTreatSkippedFailuresAsUnknown()
    {
        await using var db = await CoachTestDatabase.Create();
        var first = await db.Seed();
        for (var i = 0; i < 6; i++) await AddMember(db, first.Session, i + 1);
        // A nonproblematic card after the limit still receives its actual reason.
        var lastId = await AddMember(db, first.Session, 7);
        var finalAttempt = await db.Context.LearningAttempts.SingleAsync(a => a.SessionId == first.Session.Id && a.CardId == lastId);
        finalAttempt.Outcome = ReviewOutcome.Easy; finalAttempt.ResponseTimeMs = 1000;
        await db.Context.SaveChangesAsync();
        var result = await Builder(db).BuildWithDiagnostics(first.Session.Id, "en");
        Assert.Equal(CoachLimits.Cards, result.Context.Cards.Count);
        Assert.Equal(CoachLimits.Cards, result.DiagnosticsContext.CandidateCount);
        Assert.Equal(5, result.DiagnosticsContext.Cards.Count(c => c.SelectionReason == "Included"));
        Assert.Equal(2, result.DiagnosticsContext.Cards.Count(c => c.SelectionReason == "CandidateLimit"));
        Assert.Equal("SessionNotProblematic", result.DiagnosticsContext.Cards.Single(c => c.CardId == lastId).SelectionReason);
    }

    [Fact]
    public async Task DiagnosticsAreBoundedAndReportRevisionCountsWithoutOldContentOrAttemptIdentifiers()
    {
        await using var db = await CoachTestDatabase.Create();
        var (card, session) = await db.Seed(revision: 3, failures: 15, value: "PRIVATE_WORD_MARKER");
        db.Context.LearningAttempts.Add(new LearningAttempt { Id = Guid.NewGuid(), CardId = card.Id, LearningRevision = 1,
            ReviewedAt = db.Now.AddDays(-2), Outcome = ReviewOutcome.Again, PreviousDueDate = db.Now, NewDueDate = db.Now });
        await db.Context.SaveChangesAsync();
        var result = await Builder(db).BuildWithDiagnostics(session.Id, "ru");
        var item = Assert.Single(result.DiagnosticsContext.Cards);
        Assert.Equal(15, item.CurrentRevisionEvidenceCount);
        Assert.Equal(1, item.HistoricalRevisionEvidenceCount);
        Assert.Equal(6, item.RecentAttempts.Count);
        Assert.All(item.RecentAttempts, a => { Assert.Equal(3, a.LearningRevision); Assert.True(a.ReviewedAt <= session.CompletedAt); });
        Assert.Equal(session.PlannedCardsCount, result.DiagnosticsContext.Planned);
        Assert.Equal(session.CompletedCardsCount, result.DiagnosticsContext.Completed);
        Assert.Equal(session.SuccessfulReviews, result.DiagnosticsContext.Successful);
        Assert.Equal(session.FailedReviews, result.DiagnosticsContext.Failed);
        var json = Json(result.DiagnosticsContext);
        Assert.DoesNotContain("PRIVATE_WORD_MARKER", json);
        Assert.DoesNotContain("в конце концов", json);
        foreach (var excluded in new[] { "value", "translation", "transcription", "email", "userId", "password", "authorization", "accessToken", "refreshToken", "apiKey", "correctOptionId", "optionId", "evidenceAttemptIds", "sessionId" })
            Assert.DoesNotContain($"\"{excluded}\"", json);
        Assert.DoesNotContain(typeof(CoachDiagnosticAttemptDto).GetProperties(), p => p.Name == "Id");
    }

    [Fact]
    public async Task EvenCorruptedOversizedSessionMembershipCannotCreateUnboundedDiagnosticOutput()
    {
        await using var db = await CoachTestDatabase.Create();
        var first = await db.Seed();
        for (var i = 0; i < 22; i++) await AddMember(db, first.Session, i + 1);
        var result = await Builder(db).BuildWithDiagnostics(first.Session.Id, "en");
        Assert.True(result.DiagnosticsContext.Cards.Count <= 20);
        Assert.True(result.Context.Cards.Count <= 5);
        Assert.All(result.DiagnosticsContext.Cards, c => Assert.True(c.RecentAttempts.Count <= 6));
    }

    [Fact]
    public async Task CapturedSnapshotIsDurableBeforeProviderAndRemainsStableAfterLaterReviewAndEdit()
    {
        await using var db = await CoachTestDatabase.Create();
        var (card, session) = await db.Seed();
        var provider = new DiagnosticProvider();
        provider.BeforeResponse = async context => {
            var row = await db.Context.Set<CoachAnalysis>().AsNoTracking().SingleAsync();
            Assert.NotNull(row.DiagnosticJson);
            var duringCall = JsonSerializer.Deserialize<CoachDiagnosticsDto>(row.DiagnosticJson!, CoachOutputValidator.Json)!;
            Assert.Equal("CapturedAtAnalysis", duringCall.Mode);
            Assert.Equal("Pending", duringCall.Analysis.Status);
            Assert.Equal(1, duringCall.Context.CandidateCount);
            Assert.True(duringCall.ProviderCalled);
            Assert.Equal(Json(context).Length, duringCall.InputCharacters);
            Assert.DoesNotContain("selectionReason", Json(context));
            Assert.DoesNotContain("decisionSource", Json(context));
        };
        var service = Service(db, provider);
        var analysis = await service.Analyze(session.Id, new());
        var first = await service.Diagnostics(session.Id);
        Assert.Equal("CapturedAtAnalysis", first.Mode);
        Assert.Equal(analysis.Id, first.AnalysisId);
        Assert.Equal("Provider", first.Analysis.DecisionSource);
        Assert.Equal("Completed", first.Analysis.Status);
        Assert.Equal("Intervention", first.Analysis.Decision);
        Assert.True(first.ProviderCalled);
        Assert.True(first.InputCharacters > 0);
        Assert.True(first.OutputCharacters > 0);
        Assert.True(first.DurationMs >= 0);
        var original = Json(first);
        var storedCard = await db.Context.Cards.SingleAsync(); storedCard.LearningRevision++;
        var word = await db.Context.Words.SingleAsync(); word.Value = "edited new content";
        db.Context.LearningAttempts.Add(new LearningAttempt { Id = Guid.NewGuid(), CardId = card.Id, LearningRevision = 1,
            Outcome = ReviewOutcome.Good, ReviewedAt = db.Now.AddDays(1), PreviousDueDate = db.Now, NewDueDate = db.Now });
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();
        var saveCalls = db.SaveCalls;
        Assert.Equal(original, Json(await service.Diagnostics(session.Id)));
        Assert.Equal(saveCalls, db.SaveCalls);
        Assert.Equal(1, provider.Calls);
    }

    [Theory]
    [InlineData("NoCandidates")]
    [InlineData("Provider")]
    public async Task NoneDiagnosticsDistinguishDeterministicNoCandidatesFromActualProviderDecision(string source)
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed(failures: source == "NoCandidates" ? 1 : 2);
        var provider = new DiagnosticProvider { Response = "{\"decision\":\"None\",\"summary\":\"No persistent issue.\",\"observations\":[],\"interventions\":[]}" };
        var service = Service(db, provider);
        var learningBefore = await LearningSnapshot(db);
        Assert.Equal("None", (await service.Analyze(session.Id, new())).Decision);
        Assert.Equal(learningBefore, await LearningSnapshot(db));
        var diagnostic = await service.Diagnostics(session.Id);
        Assert.Equal(source, diagnostic.Analysis.DecisionSource);
        Assert.Equal("None", diagnostic.Analysis.Decision);
        Assert.Equal(source == "Provider", diagnostic.ProviderCalled);
        Assert.Equal(source == "Provider" ? 1 : 0, provider.Calls);
        var before = Json(diagnostic);
        await service.Analyze(session.Id, new());
        Assert.Equal(before, Json(await service.Diagnostics(session.Id)));
        Assert.Equal(source == "Provider" ? 1 : 0, provider.Calls);
    }

    [Theory]
    [InlineData("ai_provider_unavailable")]
    [InlineData("ai_provider_rate_limited")]
    public async Task FailureRetainsPreCallSnapshotWithoutRawOutputAndRetryReplacesGenerationSnapshot(string failureCode)
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new DiagnosticProvider { Failure = new UpstreamServiceException("PRIVATE_PROVIDER_ERROR", failureCode) };
        var service = Service(db, provider);
        var result = await service.Analyze(session.Id, new());
        Assert.Equal("Failed", result.Status);
        var failure = await service.Diagnostics(session.Id);
        Assert.Equal("CapturedAtAnalysis", failure.Mode);
        Assert.Equal("Failed", failure.Analysis.DecisionSource);
        Assert.Equal(failureCode, failure.Analysis.FailureCode);
        Assert.Equal(1, failure.Context.CandidateCount);
        Assert.DoesNotContain("PRIVATE_PROVIDER_ERROR", Json(failure));
        var row = await db.Context.Set<CoachAnalysis>().SingleAsync(); row.UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        await db.Context.SaveChangesAsync(); provider.Failure = null;
        var retried = await service.Analyze(session.Id, new());
        var success = await service.Diagnostics(session.Id);
        Assert.Equal(result.Id, retried.Id);
        Assert.Equal(failure.Analysis.Generation + 1, success.Analysis.Generation);
        Assert.Equal("Provider", success.Analysis.DecisionSource);
        Assert.Null(success.Analysis.FailureCode);
        Assert.Equal(2, provider.Calls);
    }

    [Fact]
    public async Task InvalidProviderOutputKeepsEvidenceDiagnosticsButNeverRetainsRawContent()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed(value: "PRIVATE_CARD_VALUE");
        var provider = new DiagnosticProvider { Response = "PRIVATE_RAW_BAD_OUTPUT" };
        var service = Service(db, provider);
        await service.Analyze(session.Id, new());
        var diagnostic = await service.Diagnostics(session.Id);
        Assert.Equal("Failed", diagnostic.Analysis.DecisionSource);
        Assert.Equal("ai_provider_invalid_response", diagnostic.Analysis.FailureCode);
        Assert.DoesNotContain("PRIVATE_RAW_BAD_OUTPUT", Json(diagnostic));
        Assert.DoesNotContain("PRIVATE_CARD_VALUE", Json(diagnostic));
        Assert.Equal("PRIVATE_RAW_BAD_OUTPUT".Length, diagnostic.OutputCharacters);
    }

    [Fact]
    public async Task LegacySnapshotReconstructionIsExplicitReadOnlyAndNeverTriggersProvider()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        db.Context.Set<CoachAnalysis>().Add(new CoachAnalysis { Id = Guid.NewGuid(), SessionId = session.Id, CollectionId = session.CollectionId,
            Status = "Completed", Decision = "None", PromptVersion = CoachLimits.PromptVersion, Generation = 1, CreatedAt = db.Now, UpdatedAt = db.Now });
        await db.Context.SaveChangesAsync(); db.Context.ChangeTracker.Clear();
        var before = await LearningSnapshot(db);
        var saveCalls = db.SaveCalls;
        var provider = new DiagnosticProvider();
        var diagnostic = await Service(db, provider).Diagnostics(session.Id);
        Assert.Equal("ReconstructedNow", diagnostic.Mode);
        Assert.Equal("LegacyUnknown", diagnostic.Analysis.DecisionSource);
        Assert.Null(diagnostic.ProviderCalled);
        Assert.Null(diagnostic.InputCharacters);
        Assert.Null(diagnostic.OutputCharacters);
        Assert.Equal(0, provider.Calls);
        Assert.Equal(saveCalls, db.SaveCalls);
        Assert.Equal(before, await LearningSnapshot(db));
        Assert.Null((await db.Context.Set<CoachAnalysis>().AsNoTracking().SingleAsync()).DiagnosticJson);
    }

    [Fact]
    public async Task MissingAnalysisCanBeDiagnosedWithoutCreatingOneOrRebuildingLearnerCache()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        db.Context.LearnerCardStates.Remove(await db.Context.LearnerCardStates.SingleAsync());
        await db.Context.SaveChangesAsync(); db.Context.ChangeTracker.Clear();
        var saveCalls = db.SaveCalls;
        var provider = new DiagnosticProvider();
        var diagnostic = await Service(db, provider).Diagnostics(session.Id);
        Assert.Equal("ReconstructedNow", diagnostic.Mode);
        Assert.Null(diagnostic.AnalysisId);
        Assert.Equal("LegacyUnknown", diagnostic.Analysis.DecisionSource);
        Assert.Null(diagnostic.ProviderCalled);
        Assert.Equal(0, provider.Calls);
        Assert.Equal(saveCalls, db.SaveCalls);
        Assert.Empty(await db.Context.Set<CoachAnalysis>().ToListAsync());
        Assert.Empty(await db.Context.LearnerCardStates.ToListAsync());
        Assert.Equal(2, await db.Context.LearningAttempts.CountAsync());
    }

    [Fact]
    public async Task DisabledDiagnosticsPerformNoGenerationAndNeverWriteARecord()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new DiagnosticProvider();
        var saveCalls = db.SaveCalls;
        var diagnostic = await Service(db, provider, enabled: false).Diagnostics(session.Id);
        Assert.Equal("Disabled", diagnostic.Analysis.DecisionSource);
        Assert.Equal("Disabled", diagnostic.Analysis.Status);
        Assert.Null(diagnostic.ProviderCalled);
        Assert.Equal(0, provider.Calls);
        Assert.Equal(saveCalls, db.SaveCalls);
        Assert.Empty(await db.Context.Set<CoachAnalysis>().ToListAsync());
    }

    [Fact]
    public async Task ClearedTrackerOnRevisionRaceStillPersistsCapturedSnapshotAndNormalStateIsUntouched()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new DiagnosticProvider { BeforeResponse = async _ => {
            var card = await db.Context.Cards.SingleAsync(); card.LearningRevision++;
            await db.Context.SaveChangesAsync();
        } };
        var service = Service(db, provider);
        var failure = await service.Analyze(session.Id, new());
        var diagnostic = await service.Diagnostics(session.Id);
        Assert.Equal("coach_context_stale", failure.FailureCode);
        Assert.Equal("CapturedAtAnalysis", diagnostic.Mode);
        Assert.Equal(0, Assert.Single(diagnostic.Context.Cards).CurrentRevision);
        Assert.Equal("Included", diagnostic.Context.Cards[0].SelectionReason);
        Assert.Equal("Failed", diagnostic.Analysis.DecisionSource);
        Assert.Equal(2, await db.Context.LearningAttempts.CountAsync());
        Assert.Empty(await db.Context.Set<CoachIntervention>().ToListAsync());
    }

    [Fact]
    public async Task DiagnosticSnapshotExcludesEvenValidatedModelSummaryThatRepeatsPrivateContent()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed(value: "PRIVATE_PROSE_MARKER");
        var provider = new DiagnosticProvider { Response = "{\"decision\":\"None\",\"summary\":\"PRIVATE_PROSE_MARKER\",\"observations\":[],\"interventions\":[]}" };
        var service = Service(db, provider);
        var analysis = await service.Analyze(session.Id, new());
        Assert.Equal("PRIVATE_PROSE_MARKER", analysis.Summary);
        var diagnostic = await service.Diagnostics(session.Id);
        Assert.DoesNotContain("PRIVATE_PROSE_MARKER", Json(diagnostic));
        Assert.DoesNotContain(typeof(CoachDiagnosticAnalysisDto).GetProperties(), p => p.Name == "Summary");
        Assert.DoesNotContain(typeof(CoachAnalysisDto).GetProperties(), p => p.Name is "Diagnostics" or "DiagnosticJson");
        Assert.DoesNotContain(typeof(CoachInterventionDto).GetProperties(), p => p.Name is "Diagnostics" or "DiagnosticJson");
        Assert.DoesNotContain("PRIVATE_PROSE_MARKER", (await db.Context.Set<CoachAnalysis>().SingleAsync()).DiagnosticJson);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupersededProviderWorkerCannotOverwriteNewGenerationDiagnosticSnapshot(bool fails)
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        string? newSnapshot = null;
        var provider = new DiagnosticProvider { BeforeResponse = async _ => {
            var row = await db.Context.Set<CoachAnalysis>().SingleAsync();
            row.Generation++;
            row.UpdatedAt = DateTimeOffset.UtcNow;
            newSnapshot = Json(new CoachDiagnosticsDto { SessionId = session.Id, AnalysisId = row.Id, Mode = "CapturedAtAnalysis", CapturedAt = row.UpdatedAt,
                Analysis = new CoachDiagnosticAnalysisDto { Status = "Pending", DecisionSource = "Pending", Generation = row.Generation },
                Context = new CoachDiagnosticContextDto { CandidateCount = 0 } });
            row.DiagnosticJson = newSnapshot;
            await db.Context.SaveChangesAsync();
        }, Failure = fails ? new UpstreamServiceException("obsolete error", "ai_provider_unavailable") : null };
        var service = Service(db, provider);
        var result = await service.Analyze(session.Id, new());
        Assert.Equal("Pending", result.Status);
        Assert.Equal(newSnapshot, (await db.Context.Set<CoachAnalysis>().AsNoTracking().SingleAsync()).DiagnosticJson);
        var diagnostic = await service.Diagnostics(session.Id);
        Assert.Equal(2, diagnostic.Analysis.Generation);
        Assert.Equal(0, diagnostic.Context.CandidateCount);
        Assert.Equal("Pending", diagnostic.Analysis.DecisionSource);
        Assert.Empty(await db.Context.Set<CoachIntervention>().ToListAsync());
    }

    [Fact]
    public async Task UnexpectedWorkerInterruptionStillLeavesReadablePendingEvidenceSnapshot()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var provider = new DiagnosticProvider { Failure = new InvalidOperationException("simulated worker interruption") };
        var service = Service(db, provider);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Analyze(session.Id, new()));
        var calls = db.SaveCalls;
        var diagnostic = await service.Diagnostics(session.Id);
        Assert.Equal("CapturedAtAnalysis", diagnostic.Mode);
        Assert.Equal("Pending", diagnostic.Analysis.Status);
        Assert.Equal("Pending", diagnostic.Analysis.DecisionSource);
        Assert.Equal(1, diagnostic.Context.CandidateCount);
        Assert.True(diagnostic.ProviderCalled);
        Assert.Null(diagnostic.OutputCharacters);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(calls, db.SaveCalls);
        Assert.DoesNotContain("simulated worker interruption", Json(diagnostic));
    }

    [Fact]
    public async Task LegacyReconstructionAfterCardEditIsClearlyCurrentRatherThanPretendingToBeHistorical()
    {
        await using var db = await CoachTestDatabase.Create();
        var (_, session) = await db.Seed();
        var legacy = new CoachAnalysis { Id = Guid.NewGuid(), SessionId = session.Id, CollectionId = session.CollectionId,
            Status = "Completed", Decision = "None", PromptVersion = "coach-v1", Generation = 1, CreatedAt = db.Now.AddDays(-10), UpdatedAt = db.Now.AddDays(-10) };
        db.Context.Set<CoachAnalysis>().Add(legacy);
        (await db.Context.Cards.SingleAsync()).LearningRevision++;
        await db.Context.SaveChangesAsync();
        var provider = new DiagnosticProvider();
        var calls = db.SaveCalls;
        var diagnostic = await Service(db, provider).Diagnostics(session.Id);
        Assert.Equal(legacy.Id, diagnostic.AnalysisId);
        Assert.Equal("ReconstructedNow", diagnostic.Mode);
        Assert.Equal("LegacyUnknown", diagnostic.Analysis.DecisionSource);
        Assert.True(diagnostic.CapturedAt > legacy.CreatedAt);
        var row = Assert.Single(diagnostic.Context.Cards);
        Assert.Equal(0, row.SessionRevision);
        Assert.Equal(1, row.CurrentRevision);
        Assert.Equal("RevisionMismatch", row.SelectionReason);
        Assert.Equal(calls, db.SaveCalls);
        Assert.Equal(0, provider.Calls);
        Assert.Null((await db.Context.Set<CoachAnalysis>().AsNoTracking().SingleAsync()).DiagnosticJson);
    }

    private static async Task<int> AddMember(CoachTestDatabase db, LearningSession target, int position)
    {
        var added = await db.Seed(value: $"word-{position}");
        var card = await db.Context.Cards.SingleAsync(c => c.Id == added.Card.Id); card.CollectionId = target.CollectionId;
        db.Context.LearningSessionCards.Add(new LearningSessionCard { SessionId = target.Id, CardId = card.Id, LearningRevision = 0, Position = position });
        (await db.Context.LearningAttempts.SingleAsync(a => a.SessionId == added.Session.Id)).SessionId = target.Id;
        await db.Context.SaveChangesAsync(); db.Context.ChangeTracker.Clear();
        return added.Card.Id;
    }
    private static async Task<string> LearningSnapshot(CoachTestDatabase db)
        => Json(new { Cards = await db.Context.Cards.AsNoTracking().OrderBy(c => c.Id).Select(c => new { c.Id, c.DueDate, c.EaseFactor, c.IntervalDays, c.ReviewCount, c.LearningRevision }).ToListAsync(),
            States = await db.Context.LearnerCardStates.AsNoTracking().ToListAsync(), Attempts = await db.Context.LearningAttempts.AsNoTracking().OrderBy(a => a.Id).ToListAsync() });

    private sealed class DiagnosticProvider : IAiCoachProvider
    {
        public string Provider => "Fake";
        public string Model => "diagnostic-test-v1";
        public int Calls { get; private set; }
        public Exception? Failure { get; set; }
        public string? Response { get; set; }
        public Func<CoachSessionContext, Task>? BeforeResponse { get; set; }
        public async Task<string> Analyze(CoachSessionContext context, CancellationToken cancellationToken)
        {
            Calls++;
            if (BeforeResponse != null) await BeforeResponse(context);
            if (Failure != null) throw Failure;
            return Response ?? await new FakeAiCoachProvider().Analyze(context, cancellationToken);
        }
    }
}

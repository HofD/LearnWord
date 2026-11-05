using System.Text.Json;
using LearnWord.CoachEval;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Services;
using Xunit;
namespace LearnWord.WebApi.Tests;

public class CoachBehaviorEvaluationTests
{
    private const string None = """{"decision":"None","summary":"Keep practicing.","observations":[],"interventions":[]}""";

    [Fact]
    public async Task AlwaysNoneIsStructurallyValidButFailsTheBehaviorBenchmark()
    {
        var report = await Evaluation.Run(["test-model"], 2, true, _ => new Reply(None));
        Assert.Equal(12, report.PlannedRuns); Assert.Equal(12, report.CompletedRuns);
        Assert.Equal(4, report.Passed); Assert.Equal(8, report.MissedHelp);
        Assert.Equal(0, report.ContractFailures); Assert.Equal(1, report.ExitCode);
        Assert.All(report.Results, r => Assert.True(r.ContractValid));
    }

    [Fact]
    public void SummaryAloneDoesNotCountAsAnIntervention()
    {
        var test = Fixtures.All().Single(c => c.Id == "six-again-en");
        var result = Evaluation.Score(test, "model", 1, None);
        Assert.Equal("missed_help", result.Outcome); Assert.True(result.ContractValid);
    }

    [Fact]
    public async Task FakeSelfCheckPassIsClearlySeparateFromRealModelEvaluation()
    {
        var report = await Evaluation.Run(["Fake"], 1, false, _ => new FakeAiCoachProvider());
        Assert.Equal("FakeSelfCheck", report.Mode); Assert.Equal(6, report.Passed); Assert.Equal(0, report.ExitCode);
        Assert.Contains(report.Results, r => r.CaseId.Contains("ru"));
        Assert.Contains(report.Results, r => r.CaseId.Contains("multiword"));
    }

    [Fact]
    public void MalformedOutputIsAContractFailureNotAnAbstention()
    {
        var result = Evaluation.Score(Fixtures.All()[0], "model", 1, "```PRIVATE_OUTPUT");
        Assert.Equal("contract_failure", result.Outcome); Assert.False(result.ContractValid);
        Assert.Equal("json_malformed", result.ValidationCode);
        Assert.DoesNotContain("PRIVATE_OUTPUT", JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task UnknownCardReferencesFailTheProductionValidator()
    {
        var test = Fixtures.All().Single(c => c.Id == "six-again-en");
        var result = JsonSerializer.Deserialize<CoachAnalysisResult>(await new FakeAiCoachProvider().Analyze(test.Context, default), CoachOutputValidator.Json)!;
        result.Observations[0].CardId = 9999;
        var scored = Evaluation.Score(test, "model", 1, JsonSerializer.Serialize(result, CoachOutputValidator.Json));
        Assert.Equal("contract_failure", scored.Outcome); Assert.Equal("card_unknown", scored.ValidationCode);
    }

    [Fact]
    public async Task WrongPairedTranslationOnMultiwordCardFailsGrounding()
    {
        var test = Fixtures.All().Single(c => c.Id == "multiword-again-en");
        var result = JsonSerializer.Deserialize<CoachAnalysisResult>(await new FakeAiCoachProvider().Analyze(test.Context, default), CoachOutputValidator.Json)!;
        var exercise = result.Interventions.Single(i => i.Type == "ExtraPractice").Exercises[0];
        exercise.Options.Single(o => o.Id == exercise.CorrectOptionId).Text = "никогда";
        var scored = Evaluation.Score(test, "model", 1, JsonSerializer.Serialize(result, CoachOutputValidator.Json));
        Assert.Equal("contract_failure", scored.Outcome); Assert.Equal("exercise_answer_mismatch", scored.ValidationCode);
    }

    [Fact]
    public async Task ValidButUnwantedAdviceInAControlCaseIsNotAMissedHelpFailure()
    {
        var repeated = Fixtures.All().Single(c => c.Id == "six-again-en");
        // Same valid grounded response, but explicitly a control criterion: do not confuse
        // validity with whether this benchmark expects help in a particular case.
        var control = new EvaluationCase("control", false, repeated.Context);
        var scored = Evaluation.Score(control, "model", 1, await new FakeAiCoachProvider().Analyze(repeated.Context, default));
        Assert.Equal("unexpected_help", scored.Outcome); Assert.True(scored.ContractValid);
    }

    [Fact]
    public async Task ProviderFailuresRemainInTheDenominatorAndReportsDoNotRetainExceptions()
    {
        var report = await Evaluation.Run(["model"], 1, true, _ => new Failing());
        Assert.Equal(6, report.PlannedRuns); Assert.Equal(6, report.CompletedRuns);
        Assert.Equal(6, report.ProviderFailures); Assert.Equal(0, report.Passed); Assert.Equal(1, report.ExitCode);
        var group = Assert.Single(report.ModelSummaries); Assert.Equal(6, group.PlannedRuns); Assert.Equal(6, group.ProviderFailures);
        Assert.DoesNotContain("PRIVATE_KEY", JsonSerializer.Serialize(report));
        Assert.DoesNotContain("PRIVATE_BODY", JsonSerializer.Serialize(report));
    }

    [Fact]
    public async Task CancellationNeverReportsAnIncompleteRunAsPassing()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        var calls = 0;
        var report = await Evaluation.Run(["model"], 1, true, _ => { calls++; return new Reply(None); }, cts.Token);
        Assert.True(report.Cancelled); Assert.Equal(0, report.CompletedRuns); Assert.Equal(130, report.ExitCode);
    }

    [Theory]
    [InlineData("--live")]
    [InlineData("--model", "model")]
    [InlineData("--repeat", "0")]
    [InlineData("--repeat", "6")]
    [InlineData("--repeat", "PRIVATE_NUMBER")]
    [InlineData("--unknown")]
    [InlineData("--output", "/src/PRIVATE.json")]
    [InlineData("--output", "../PRIVATE.json")]
    [InlineData("--output", "PRIVATE.txt")]
    [InlineData("--live", "--model", "PRIVATE MODEL")]
    [InlineData("--live", "--model", "a", "--model", "a")]
    public void InvalidConfigurationIsRejectedBeforeAnyProviderIsCreated(params string[] args)
        => Assert.Throws<ArgumentException>(() => EvaluationOptions.Parse(args, "test-key"));

    [Fact]
    public void LiveNeedsAnExplicitKeyAndModelAndHasBoundedPlannedRequests()
    {
        Assert.Throws<ArgumentException>(() => EvaluationOptions.Parse(["--live", "--model", "a"], null));
        Assert.Throws<ArgumentException>(() => EvaluationOptions.Parse(["--live", "--model", "a", "--model", "b", "--model", "c", "--repeat", "5"], "key"));
        Assert.Throws<ArgumentException>(() => EvaluationOptions.Parse(["--live", "--model", "a", "--model", "b", "--model", "c", "--model", "d"], "key"));
        var options = EvaluationOptions.Parse(["--live", "--model", "google/gemma-4-26b-a4b-it", "--model", "another/model", "--repeat", "5"], "key");
        Assert.Equal(60, options.PlannedCalls); Assert.True(options.Live);
        Assert.Equal(18, EvaluationOptions.Parse([], null).PlannedCalls);
    }

    [Fact]
    public void SyntheticFixturesAreRepeatableAndDoNotContainProductionIdentifiers()
    {
        Assert.Equal(JsonSerializer.Serialize(Fixtures.All()), JsonSerializer.Serialize(Fixtures.All()));
        foreach (var fixture in Fixtures.All())
        {
            Assert.Single(fixture.Context.Cards);
            Assert.Equal(7, fixture.Context.Cards[0].CardId);
            Assert.Equal(2, fixture.Context.Cards[0].LearningRevision);
            var card = fixture.Context.Cards[0];
            Assert.Equal("Observed", card.CurrentLearnerState.Source);
            Assert.Equal(card.CurrentRevisionEvidenceCount, card.CurrentLearnerState.AttemptsCount);
            Assert.Equal(card.SessionAttempt.Outcome, card.CurrentLearnerState.LastOutcome);
            Assert.Equal(card.SessionAttempt.Id, card.RecentAttempts[0].Id);
            Assert.Equal(fixture.Context.Cards[0].SessionAttempt.Id, fixture.Context.Cards[0].RecentAttempts[0].Id);
        }
    }

    [Fact]
    public async Task EmptyOrUnboundedRunsCannotAccidentallyPass()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Evaluation.Run([], 1, false, _ => new Reply(None)));
        await Assert.ThrowsAsync<ArgumentException>(() => Evaluation.Run(["Fake"], 0, false, _ => new Reply(None)));
    }

    [Fact]
    public async Task ProgressStartsBeforeProviderAndPublishesResultBeforeNextRequest()
    {
        using var cancelled = new CancellationTokenSource();
        var events = new List<EvaluationProgress>();
        var provider = new CheckingReply(() => {
            Assert.Single(events);
            Assert.Equal(1, events[0].RunNumber);
            Assert.Null(events[0].Result);
        });
        var report = await Evaluation.Run(["model"], 1, true, _ => provider, cancelled.Token, progress: update => {
            events.Add(update);
            if (update.Result != null) cancelled.Cancel();
        });
        Assert.Equal(2, events.Count);
        Assert.Equal("pass", events[1].Result!.Outcome);
        Assert.Equal(1, report.CompletedRuns);
        Assert.True(report.Cancelled);
    }

    [Fact]
    public async Task FailedRequestsAlsoPublishImmediateProgressResults()
    {
        var events = new List<EvaluationProgress>();
        var report = await Evaluation.Run(["model"], 1, true, _ => new Failing(), progress: events.Add);
        Assert.Equal(12, events.Count);
        for (var index = 0; index < 6; index++)
        {
            Assert.Null(events[index * 2].Result);
            Assert.Equal(index + 1, events[index * 2].RunNumber);
            Assert.Equal("provider_failure", events[index * 2 + 1].Result!.Outcome);
        }
        Assert.Equal(6, report.ProviderFailures);
    }

    private sealed class CheckingReply(Action check) : IAiCoachProvider
    {
        public string Provider => "Test"; public string Model => "test-model";
        public Task<string> Analyze(CoachSessionContext context, CancellationToken cancellationToken)
        { check(); return Task.FromResult(None); }
    }

    private sealed class Reply(string response) : IAiCoachProvider
    {
        public string Provider => "Test"; public string Model => "test-model";
        public Task<string> Analyze(CoachSessionContext context, CancellationToken cancellationToken) => Task.FromResult(response);
    }
    private sealed class Failing : IAiCoachProvider
    {
        public string Provider => "Test"; public string Model => "test-model";
        public Task<string> Analyze(CoachSessionContext context, CancellationToken cancellationToken)
            => throw new UpstreamServiceException("PRIVATE_KEY PRIVATE_BODY", "ai_provider_unavailable");
    }
}

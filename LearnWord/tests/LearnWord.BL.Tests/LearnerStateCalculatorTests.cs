using System.Text.Json;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Services;
using LearnWord.DAL.Models;
using Xunit;

namespace LearnWord.BL.Tests;

public class LearnerStateCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);
    private readonly LearnerStateCalculator calculator = new();

    [Fact]
    public void FreshCard_IsNewWithoutFabricatedEvidence()
    {
        var card = Fresh();
        var state = calculator.Calculate(card, [], Now);
        Assert.Equal(LearnerStatus.New, state.Status);
        Assert.Equal("NoHistory", state.Source);
        Assert.Equal(0, state.AttemptsCount);
        Assert.Null(state.LastOutcome);
        Assert.Null(state.LastAttemptAt);
        Assert.Equal(0m, state.MasteryScore);
    }

    [Fact]
    public void LegacySchedule_IsConservativeAndDoesNotInventAttemptsOrDates()
    {
        var card = Fresh();
        card.ReviewCount = 50;
        card.IntervalDays = 200;
        card.LastReviewedAt = Now.AddDays(-2);
        card.DueDate = Now.AddDays(198);
        card.Learnt = true;
        var before = JsonSerializer.Serialize(card);
        var state = calculator.Calculate(card, [], Now);
        Assert.Equal("LegacyInferred", state.Source);
        Assert.Equal(LearnerStatus.Learning, state.Status);
        Assert.InRange(state.MasteryScore, 0m, .4m);
        Assert.Equal(0, state.AttemptsCount);
        Assert.Equal(0, state.SuccessfulAttempts);
        Assert.Null(state.LastOutcome);
        Assert.Null(state.LastAttemptAt);
        Assert.Equal(before, JsonSerializer.Serialize(card));
    }

    [Theory]
    [InlineData(ReviewOutcome.Again)]
    [InlineData(ReviewOutcome.Hard)]
    [InlineData(ReviewOutcome.Good)]
    [InlineData(ReviewOutcome.Easy)]
    public void AllOutcomes_AlwaysProduceBoundedDeterministicScores(ReviewOutcome outcome)
    {
        var card = Fresh();
        var attempts = new List<LearningAttempt>();
        for (int i = 0; i < 8; i++)
        {
            attempts.Add(Review(card, outcome, Now.AddDays(i)));
            var state = calculator.Calculate(card, attempts, Now.AddDays(i + 400));
            Assert.InRange(state.MasteryScore, 0m, 1m);
            Assert.InRange(state.DifficultyScore, 0m, 1m);
            Assert.Equal(i + 1, state.AttemptsCount);
            Assert.Equal(state.AttemptsCount, state.SuccessfulAttempts + state.FailedAttempts);
            Assert.Equal(JsonSerializer.Serialize(state), JsonSerializer.Serialize(calculator.Calculate(card, attempts, Now.AddDays(i + 400))));
        }
    }

    [Fact]
    public void OneEasy_IsLearningRatherThanStrong()
    {
        var card = Fresh();
        var attempt = Review(card, ReviewOutcome.Easy, Now);
        var state = calculator.Calculate(card, [attempt], Now);
        Assert.Equal(LearnerStatus.Learning, state.Status);
        Assert.Equal(1, state.AttemptsCount);
        Assert.True(state.MasteryScore < LearnerStateCalculator.StrongMastery);
    }

    [Fact]
    public void IncreasingSpacedGoodReviews_RaiseMasteryAndSustainedEvidenceBecomesStrong()
    {
        var card = Fresh();
        var attempts = new List<LearningAttempt>();
        decimal previous = 0;
        var time = Now;
        LearnerCardState state = null!;
        for (int i = 0; i < 8; i++)
        {
            attempts.Add(Review(card, ReviewOutcome.Good, time));
            state = calculator.Calculate(card, attempts, time);
            Assert.True(state.MasteryScore >= previous);
            previous = state.MasteryScore;
            time = card.DueDate;
        }
        Assert.Equal(LearnerStatus.Strong, state.Status);
        Assert.Equal(8, state.ConsecutiveSuccesses);
        Assert.Equal(0, state.FailedAttempts);
    }

    [Fact]
    public void RecentAgain_MateriallyReducesMasteryAndImmediatelyClassifiesWeak()
    {
        var card = Fresh();
        var attempts = new List<LearningAttempt>();
        for (int i = 0; i < 5; i++) attempts.Add(Review(card, ReviewOutcome.Easy, card.DueDate));
        var before = calculator.Calculate(card, attempts, card.LastReviewedAt!.Value);
        attempts.Add(Review(card, ReviewOutcome.Again, card.DueDate));
        var after = calculator.Calculate(card, attempts, card.LastReviewedAt!.Value);
        Assert.Equal(LearnerStatus.Strong, before.Status);
        Assert.Equal(LearnerStatus.Weak, after.Status);
        Assert.True(after.MasteryScore < before.MasteryScore / 2);
        Assert.True(after.DifficultyScore > before.DifficultyScore);
        Assert.Equal(0, after.ConsecutiveSuccesses);
        Assert.Equal(1, after.ConsecutiveFailures);
    }

    [Fact]
    public void Hard_IsSuccessfulRecallWithLowerQualityThanGood()
    {
        var hard = Fresh();
        var good = Fresh();
        var hardState = calculator.Calculate(hard, [Review(hard, ReviewOutcome.Hard, Now)], Now);
        var goodState = calculator.Calculate(good, [Review(good, ReviewOutcome.Good, Now)], Now);
        Assert.Equal(1, hardState.SuccessfulAttempts);
        Assert.Equal(0, hardState.FailedAttempts);
        Assert.True(hardState.MasteryScore < goodState.MasteryScore);
        Assert.True(hardState.DifficultyScore > goodState.DifficultyScore);
    }

    [Fact]
    public void OverdueEvidence_DecaysButFutureDueDateHasNoEarlyPenalty()
    {
        var card = Fresh();
        var attempts = Enumerable.Range(0, 5).Select(_ => Review(card, ReviewOutcome.Easy, card.DueDate)).ToArray();
        var current = calculator.Calculate(card, attempts, card.LastReviewedAt!.Value);
        var stillNotDue = calculator.Calculate(card, attempts, card.DueDate);
        var overdue = calculator.Calculate(card, attempts, card.DueDate.AddDays(card.IntervalDays * 2 + 30));
        Assert.Equal(current.MasteryScore, stillNotDue.MasteryScore);
        Assert.True(overdue.MasteryScore < current.MasteryScore);
    }

    [Theory]
    [InlineData(3, 3, 7, "3.0", false)]
    [InlineData(4, 2, 7, "3.0", false)]
    [InlineData(4, 3, 6, "3.0", false)]
    [InlineData(4, 3, 7, "2.8", false)]
    [InlineData(4, 3, 7, "3.0", true)]
    public void Strong_RequiresEveryEvidenceBoundary(int attempts, int streak, int interval, string ease, bool strong)
    {
        var card = Fresh();
        card.IntervalDays = interval;
        card.EaseFactor = decimal.Parse(ease, System.Globalization.CultureInfo.InvariantCulture);
        var cached = new LearnerCardState
        {
            CardId = card.Id, AttemptsCount = attempts, SuccessfulAttempts = attempts,
            ConsecutiveSuccesses = streak, OutcomePoints = attempts, LastOutcome = ReviewOutcome.Easy, LastAttemptAt = Now
        };
        var state = calculator.Refresh(card, cached, Now);
        Assert.Equal(strong, state.Status == LearnerStatus.Strong);
        Assert.Equal(attempts, cached.AttemptsCount); // Refresh is side-effect-free.
    }

    [Fact]
    public void Rebuild_SortsHistoryAndIgnoresPriorContentRevision()
    {
        var card = Fresh();
        var first = Review(card, ReviewOutcome.Good, Now);
        var second = Review(card, ReviewOutcome.Again, Now.AddDays(1));
        var beforeReset = calculator.Calculate(card, [second, first], Now.AddDays(1));
        Assert.Equal(ReviewOutcome.Again, beforeReset.LastOutcome);
        Assert.Equal(2, beforeReset.AttemptsCount);
        card.LearningRevision++;
        new SpacedRepetitionScheduler().Reset(card, Now.AddDays(2));
        var reset = calculator.Calculate(card, [first, second], Now.AddDays(2));
        Assert.Equal(LearnerStatus.New, reset.Status);
        Assert.Equal(0, reset.AttemptsCount);
        Assert.Null(reset.LastOutcome);
    }

    private static Card Fresh() => new()
    {
        Id = 42, CollectionId = 1, CreatedAt = Now, DueDate = Now, EaseFactor = 2.5m, Words = []
    };

    private static LearningAttempt Review(Card card, ReviewOutcome outcome, DateTimeOffset at)
    {
        var attempt = new LearningAttempt
        {
            Id = Guid.NewGuid(), CardId = card.Id, LearningRevision = card.LearningRevision,
            Outcome = outcome, ReviewedAt = at, PreviousIntervalDays = card.IntervalDays,
            PreviousEaseFactor = card.EaseFactor, PreviousDueDate = card.DueDate
        };
        new SpacedRepetitionScheduler().ApplyReview(card, outcome, at);
        attempt.NewIntervalDays = card.IntervalDays;
        attempt.NewEaseFactor = card.EaseFactor;
        attempt.NewDueDate = card.DueDate;
        return attempt;
    }
}

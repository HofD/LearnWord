using LearnWord.BL.Models.Dto;
using LearnWord.WebApi.Services;
using Xunit;

namespace LearnWord.WebApi.Tests;

public class CoachCandidateRankerBoundaryTests
{
    private static CoachCardContext Card(int id, params string[] outcomes) => new() {
        CardId = id, CurrentLearnerState = new() { Status = "Learning" },
        SessionAttempt = new() { Outcome = outcomes.Last() },
        RecentAttempts = outcomes.Select((o, i) => new CoachAttemptContext { Outcome = o,
            ReviewedAt = DateTimeOffset.UnixEpoch.AddMinutes(i) }).ToList()
    };

    [Theory]
    [InlineData("Weak", "Again", CoachSeverityCategory.WeakAgain)]
    [InlineData("Learning", "Again", CoachSeverityCategory.Other)]
    [InlineData("Strong", "Again", CoachSeverityCategory.Other)]
    [InlineData("Weak", "Hard", CoachSeverityCategory.Other)]
    [InlineData("Weak", "Good", CoachSeverityCategory.Other)]
    public void WeakAgainRequiresBothWeakStateAndCurrentAgain(string status, string outcome, CoachSeverityCategory expected)
    {
        var card = Card(1, outcome);
        card.CurrentLearnerState.Status = status;
        Assert.Equal(expected, CoachCandidateRanker.Evaluate(card).Category);
    }

    [Theory]
    [InlineData(14999, 15000, 1, CoachSeverityCategory.Other)]
    [InlineData(15000, 15000, 2, CoachSeverityCategory.RepeatedSlow)]
    [InlineData(15000, 15001, 2, CoachSeverityCategory.RepeatedSlow)]
    [InlineData(null, 15000, 1, CoachSeverityCategory.Other)]
    [InlineData(0, 15000, 1, CoachSeverityCategory.Other)]
    [InlineData(-1, 15000, 1, CoachSeverityCategory.Other)]
    public void RepeatedSlowRequiresTwoMeasuredResponsesAtThreshold(int? first, int? second, int count, CoachSeverityCategory expected)
    {
        var card = Card(1, "Good", "Good");
        card.RecentAttempts[0].ResponseTimeMs = first;
        card.RecentAttempts[1].ResponseTimeMs = second;
        var signals = CoachCandidateRanker.Evaluate(card);
        Assert.Equal(count, signals.SlowCount);
        Assert.Equal(expected, signals.Category);
    }

    private static CoachCardContext InCategory(int id, CoachSeverityCategory category)
    {
        var card = category switch {
            CoachSeverityCategory.RepeatedFailure => Card(id, "Again", "Again"),
            CoachSeverityCategory.Regression => Card(id, "Good", "Again"),
            CoachSeverityCategory.WeakAgain => Card(id, "Again"),
            CoachSeverityCategory.RepeatedHard => Card(id, "Hard", "Hard"),
            CoachSeverityCategory.RepeatedSlow => Card(id, "Good", "Good"),
            _ => Card(id, "Hard")
        };
        if (category == CoachSeverityCategory.WeakAgain) card.CurrentLearnerState.Status = "Weak";
        if (category == CoachSeverityCategory.RepeatedSlow)
            foreach (var a in card.RecentAttempts) a.ResponseTimeMs = 15000;
        return card;
    }

    [Fact]
    public void ExclusiveCategoriesOutrankSecondarySignals()
    {
        var cards = Enum.GetValues<CoachSeverityCategory>().Select((c, i) => InCategory(100 - i, c)).ToArray();
        foreach (var card in cards) {
            card.CurrentLearnerState.Status = "Weak";
            card.CurrentLearnerState.MasteryScore = 1m - (int)CoachCandidateRanker.Evaluate(card).Category / 10m;
            foreach (var a in card.RecentAttempts) a.ResponseTimeMs = 20000;
        }
        Assert.Equal(Enum.GetValues<CoachSeverityCategory>(), CoachCandidateRanker.Rank(cards.Reverse()).Select(c => c.Signals.Category));
    }

    [Theory]
    [InlineData(0)] // Again count
    [InlineData(1)] // Hard count
    [InlineData(2)] // Mastery
    [InlineData(3)] // Difficulty
    [InlineData(4)] // Response time
    public void EachCriterionOutranksAllFollowingCriteria(int criterion)
    {
        var winner = Card(99, "Again", "Again");
        var loser = Card(1, "Again", "Again");
        // Later criteria deliberately favour the loser, including CardId.
        winner.CurrentLearnerState.MasteryScore = .9m;
        loser.CurrentLearnerState.MasteryScore = .1m;
        winner.CurrentLearnerState.DifficultyScore = .1m;
        loser.CurrentLearnerState.DifficultyScore = .9m;
        winner.SessionAttempt.ResponseTimeMs = 1000;
        loser.SessionAttempt.ResponseTimeMs = 20000;
        if (criterion == 0) winner.RecentAttempts.Add(new() { Outcome = "Again" });
        if (criterion <= 1) {
            loser.RecentAttempts.Add(new() { Outcome = "Hard" });
            if (criterion == 1) {
                winner.RecentAttempts.Add(new() { Outcome = "Hard" });
                winner.RecentAttempts.Add(new() { Outcome = "Hard" });
            }
        }
        if (criterion == 2) winner.CurrentLearnerState.MasteryScore = 0m;
        if (criterion >= 3) winner.CurrentLearnerState.MasteryScore = loser.CurrentLearnerState.MasteryScore;
        if (criterion == 3) winner.CurrentLearnerState.DifficultyScore = 1m;
        if (criterion == 4) {
            winner.CurrentLearnerState.DifficultyScore = loser.CurrentLearnerState.DifficultyScore;
            winner.SessionAttempt.ResponseTimeMs = 20001;
        }
        Assert.Equal(CoachSeverityCategory.RepeatedFailure, CoachCandidateRanker.Evaluate(winner).Category);
        Assert.Equal(CoachSeverityCategory.RepeatedFailure, CoachCandidateRanker.Evaluate(loser).Category);
        Assert.Equal(new[] { 99, 1 }, CoachCandidateRanker.Rank(new[] { loser, winner }).Select(c => c.Card.CardId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidResponseTimeOutranksMissingOrInvalidMeasurement(int? missing)
    {
        var measured = Card(99, "Hard"); measured.SessionAttempt.ResponseTimeMs = 1;
        var unmeasured = Card(1, "Hard"); unmeasured.SessionAttempt.ResponseTimeMs = missing;
        Assert.Null(CoachCandidateRanker.Evaluate(unmeasured).ResponseTimeMs);
        Assert.Equal(99, CoachCandidateRanker.Rank(new[] { unmeasured, measured }).First().Card.CardId);
    }

    [Theory]
    [InlineData(CoachSeverityCategory.RepeatedFailure)]
    [InlineData(CoachSeverityCategory.Regression)]
    [InlineData(CoachSeverityCategory.WeakAgain)]
    [InlineData(CoachSeverityCategory.RepeatedHard)]
    [InlineData(CoachSeverityCategory.RepeatedSlow)]
    [InlineData(CoachSeverityCategory.Other)]
    public void EqualSeverityUsesCardIdInEveryCategoryRegardlessOfInputOrder(CoachSeverityCategory category)
    {
        var cards = new[] { InCategory(9, category), InCategory(2, category), InCategory(7, category) };
        Assert.All(cards, c => Assert.Equal(category, CoachCandidateRanker.Evaluate(c).Category));
        Assert.Equal(new[] { 2, 7, 9 }, CoachCandidateRanker.Rank(cards).Select(c => c.Card.CardId));
        Assert.Equal(new[] { 2, 7, 9 }, CoachCandidateRanker.Rank(cards.Reverse()).Select(c => c.Card.CardId));
    }

    [Theory]
    [InlineData("Again")]
    [InlineData("Hard")]
    [InlineData("Slow")]
    [InlineData("Regression")]
    public void SeventhOldestAttemptCannotEstablishRecurrenceOrRegression(string signal)
    {
        var outcomes = Enumerable.Repeat("Good", 7).ToArray();
        if (signal == "Again") { outcomes[0] = "Again"; outcomes[6] = "Again"; }
        if (signal == "Hard") { outcomes[0] = "Hard"; outcomes[6] = "Hard"; }
        if (signal == "Regression") { outcomes = Enumerable.Repeat("Again", 7).ToArray(); outcomes[0] = "Good"; }
        var card = Card(1, outcomes);
        if (signal == "Slow") {
            card.RecentAttempts[0].ResponseTimeMs = 15000;
            card.RecentAttempts[6].ResponseTimeMs = 15000;
        }
        var bounded = CoachCandidateRanker.Evaluate(card);
        Assert.Equal(signal == "Again" ? CoachSeverityCategory.Regression
            : signal == "Regression" ? CoachSeverityCategory.RepeatedFailure : CoachSeverityCategory.Other, bounded.Category);
        Assert.Equal(signal == "Regression" ? 6 : signal == "Again" ? 1 : 0, bounded.AgainCount);
        Assert.Equal(signal == "Hard" ? 1 : 0, bounded.HardCount);
        Assert.Equal(signal == "Slow" ? 1 : 0, bounded.SlowCount);
        Assert.Equal(signal == "Again", bounded.Regression);
        // Move the oldest signal into the six-attempt window, independently of input order.
        card.RecentAttempts[0].ReviewedAt = card.RecentAttempts[1].ReviewedAt.AddSeconds(1);
        var expected = signal switch {
            "Again" => CoachSeverityCategory.RepeatedFailure,
            "Hard" => CoachSeverityCategory.RepeatedHard,
            "Slow" => CoachSeverityCategory.RepeatedSlow,
            _ => CoachSeverityCategory.RepeatedFailure
        };
        Assert.Equal(expected, CoachCandidateRanker.Evaluate(card).Category);
        if (signal == "Regression") Assert.True(CoachCandidateRanker.Evaluate(card).Regression);
        card.RecentAttempts.Reverse();
        Assert.Equal(expected, CoachCandidateRanker.Evaluate(card).Category);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, -1)]
    [InlineData(-1, null)]
    [InlineData(15000, 15000)]
    public void EqualOrUnmeasuredResponseTimesFallThroughToCardId(int? first, int? second)
    {
        var high = Card(99, "Hard"); high.SessionAttempt.ResponseTimeMs = first;
        var low = Card(1, "Hard"); low.SessionAttempt.ResponseTimeMs = second;
        Assert.Equal(1, CoachCandidateRanker.Rank(new[] { high, low }).First().Card.CardId);
        Assert.Equal(1, CoachCandidateRanker.Rank(new[] { low, high }).First().Card.CardId);
    }

    [Fact]
    public void PreviousRevisionsDoNotConsumeCurrentRevisionWindow()
    {
        var card = Card(1, "Again", "Again");
        card.RecentAttempts.AddRange(Enumerable.Range(1, 8).Select(i => new CoachAttemptContext {
            LearningRevision = 1, Outcome = "Good", ReviewedAt = DateTimeOffset.UnixEpoch.AddDays(i)
        }));
        var signals = CoachCandidateRanker.Evaluate(card);
        Assert.Equal(CoachSeverityCategory.RepeatedFailure, signals.Category);
        Assert.Equal(2, signals.AgainCount);
        Assert.False(signals.Regression);
    }

    [Fact]
    public void EqualAttemptTimestampsUseAttemptIdForWindowAndChronology()
    {
        var card = Card(1, Enumerable.Repeat("Good", 7).ToArray());
        for (var i = 0; i < 7; i++) {
            card.RecentAttempts[i].ReviewedAt = DateTimeOffset.UnixEpoch;
            card.RecentAttempts[i].Id = Guid.Parse($"00000000-0000-0000-0000-{i + 1:000000000000}");
        }
        card.RecentAttempts[0].Outcome = "Again";
        card.RecentAttempts[6].Outcome = "Again";
        Assert.Equal(CoachSeverityCategory.Regression, CoachCandidateRanker.Evaluate(card).Category);
        card.RecentAttempts.Reverse();
        Assert.Equal(CoachSeverityCategory.Regression, CoachCandidateRanker.Evaluate(card).Category);
    }
}

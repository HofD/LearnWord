using LearnWord.BL.Models.Dto;

namespace LearnWord.WebApi.Services;

// Selection heuristic, using only bounded current-revision evidence. Categories never stack.
public static class CoachCandidateRanker
{
    public static CoachSeveritySignalsDto Evaluate(CoachCardContext card)
    {
        var recent = card.RecentAttempts.Where(a => a.LearningRevision == card.LearningRevision)
            .OrderByDescending(a => a.ReviewedAt).ThenByDescending(a => a.Id)
            .Take(CoachLimits.RecentAttempts).OrderBy(a => a.ReviewedAt).ThenBy(a => a.Id).ToList();
        var again = recent.Count(a => a.Outcome == "Again");
        var hard = recent.Count(a => a.Outcome == "Hard");
        var slow = recent.Count(a => a.ResponseTimeMs is >= 15000);
        var successSeen = false;
        var regression = false;
        foreach (var attempt in recent)
        {
            if (attempt.Outcome == "Again" && successSeen) regression = true;
            if (attempt.Outcome is "Hard" or "Good" or "Easy") successSeen = true;
        }
        var category = again >= 2 ? CoachSeverityCategory.RepeatedFailure
            : regression ? CoachSeverityCategory.Regression
            : card.CurrentLearnerState.Status == "Weak" && card.SessionAttempt.Outcome == "Again" ? CoachSeverityCategory.WeakAgain
            : hard >= 2 ? CoachSeverityCategory.RepeatedHard
            : slow >= 2 ? CoachSeverityCategory.RepeatedSlow : CoachSeverityCategory.Other;
        return new(category, again, hard, slow, regression, card.CurrentLearnerState.MasteryScore,
            card.CurrentLearnerState.DifficultyScore, card.SessionAttempt.ResponseTimeMs is > 0 ? card.SessionAttempt.ResponseTimeMs : null);
    }

    public static IEnumerable<(CoachCardContext Card, CoachSeveritySignalsDto Signals)> Rank(IEnumerable<CoachCardContext> cards)
        => cards.Select(c => (Card: c, Signals: Evaluate(c)))
            .OrderBy(c => c.Signals.Category).ThenByDescending(c => c.Signals.AgainCount)
            .ThenByDescending(c => c.Signals.HardCount).ThenBy(c => c.Signals.MasteryScore)
            .ThenByDescending(c => c.Signals.DifficultyScore).ThenByDescending(c => c.Signals.ResponseTimeMs)
            .ThenBy(c => c.Card.CardId);
}

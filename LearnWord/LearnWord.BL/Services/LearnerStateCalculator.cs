using LearnWord.BL.Models.Dto;
using LearnWord.DAL.Models;

namespace LearnWord.BL.Services;

// Model v1 is an explainable heuristic, not a scientifically validated estimate.
public class LearnerStateCalculator
{
    public const int ModelVersion = 1;
    public const int StrongMinimumAttempts = 4;
    public const int StrongMinimumSuccesses = 3;
    public const int StrongMinimumIntervalDays = 7;
    public const decimal StrongMastery = .70m;
    public const decimal WeakMastery = .25m;

    public LearnerCardState Calculate(Card card, IEnumerable<LearningAttempt> attempts, DateTimeOffset now)
    {
        LearnerCardState state = Bootstrap(card);
        foreach (var attempt in attempts.Where(a => a.LearningRevision == card.LearningRevision)
                     .OrderBy(a => a.ReviewedAt).ThenBy(a => a.Id))
            state = Apply(card, state, attempt, now);
        return Refresh(card, state, now);
    }

    public LearnerCardState Apply(Card card, LearnerCardState? previous, LearningAttempt attempt, DateTimeOffset now)
    {
        var state = previous == null || previous.LearningRevision != card.LearningRevision ? Bootstrap(card) : Copy(previous);
        state.Source = card.ReviewCount > state.AttemptsCount + 1 ? "ObservedWithLegacyBaseline" : "Observed";
        state.AttemptsCount++;
        bool success = attempt.Outcome != ReviewOutcome.Again;
        state.SuccessfulAttempts += success ? 1 : 0;
        state.FailedAttempts += success ? 0 : 1;
        state.ConsecutiveSuccesses = success ? state.ConsecutiveSuccesses + 1 : 0;
        state.ConsecutiveFailures = success ? 0 : state.ConsecutiveFailures + 1;
        state.OutcomePoints += attempt.Outcome switch { ReviewOutcome.Again => 0m, ReviewOutcome.Hard => .5m, ReviewOutcome.Good => .8m, _ => 1m };
        state.LastOutcome = attempt.Outcome;
        state.LastAttemptAt = attempt.ReviewedAt;
        return Refresh(card, state, now);
    }

    public LearnerCardState Refresh(Card card, LearnerCardState? cached, DateTimeOffset now)
    {
        var state = cached == null || cached.LearningRevision != card.LearningRevision ? Bootstrap(card) : Copy(cached);
        if (state.AttemptsCount == 0) return state;
        decimal quality = state.OutcomePoints / state.AttemptsCount;
        decimal evidence = Math.Min(1m, state.AttemptsCount / 8m);
        decimal spacing = Math.Min(1m, Math.Max(0, card.IntervalDays) / 30m);
        decimal ease = Math.Clamp((card.EaseFactor - 1.3m) / 1.7m, 0m, 1m);
        decimal streak = Math.Min(1m, state.ConsecutiveSuccesses / 5m);
        // Grace until the scheduled due date, then gradual decay over at least 30 days.
        decimal overdue = (decimal)Math.Max(0, (now - card.DueDate).TotalDays);
        decimal recency = Math.Max(.5m, 1m - overdue / Math.Max(30m, card.IntervalDays * 2m));
        decimal score = (.40m * quality + .20m * evidence + .20m * spacing + .10m * ease + .10m * streak) * recency;
        if (state.LastOutcome == ReviewOutcome.Again) score *= .35m;
        state.MasteryScore = Math.Clamp(score, 0m, 1m);
        state.DifficultyScore = Math.Clamp(.7m * state.FailedAttempts / state.AttemptsCount + .3m * (1m - quality), 0m, 1m);
        state.Status = state.LastOutcome == ReviewOutcome.Again || (state.AttemptsCount >= 2 && state.MasteryScore < WeakMastery)
            ? LearnerStatus.Weak
            : state.AttemptsCount >= StrongMinimumAttempts && state.ConsecutiveSuccesses >= StrongMinimumSuccesses
              && card.IntervalDays >= StrongMinimumIntervalDays && state.MasteryScore >= StrongMastery
                ? LearnerStatus.Strong : LearnerStatus.Learning;
        return state;
    }

    private static LearnerCardState Bootstrap(Card card) => new()
    {
        CardId = card.Id, LearningRevision = card.LearningRevision, ModelVersion = ModelVersion,
        Source = card.ReviewCount > 0 || card.LastReviewedAt != null || card.IntervalDays > 0 || card.Learnt ? "LegacyInferred" : "NoHistory",
        Status = card.ReviewCount > 0 || card.LastReviewedAt != null || card.IntervalDays > 0 || card.Learnt ? LearnerStatus.Learning : LearnerStatus.New,
        MasteryScore = card.ReviewCount > 0 ? Math.Min(.4m, card.ReviewCount * .05m) : 0m,
        DifficultyScore = 0m
    };

    private static LearnerCardState Copy(LearnerCardState s) => new()
    {
        CardId = s.CardId, LearningRevision = s.LearningRevision, ModelVersion = ModelVersion, Source = s.Source,
        AttemptsCount = s.AttemptsCount, SuccessfulAttempts = s.SuccessfulAttempts, FailedAttempts = s.FailedAttempts,
        ConsecutiveSuccesses = s.ConsecutiveSuccesses, ConsecutiveFailures = s.ConsecutiveFailures,
        OutcomePoints = s.OutcomePoints, LastOutcome = s.LastOutcome, LastAttemptAt = s.LastAttemptAt,
        MasteryScore = s.MasteryScore, DifficultyScore = s.DifficultyScore, Status = s.Status
    };
}

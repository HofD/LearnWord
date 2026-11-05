using LearnWord.BL.Models.Dto;
using LearnWord.BL.Services;
using LearnWord.DAL.Models;
using LearnWord.WebApi.Services;
namespace LearnWord.CoachEval;

public sealed record EvaluationCase(string Id, bool ExpectHelp, CoachSessionContext Context);
public static class Fixtures
{
    public static IReadOnlyList<EvaluationCase> All() => [
        Create("successful-en", false, "en", ["Good", "Easy", "Good", "Easy"]),
        Create("isolated-again-en", false, "en", ["Again"]),
        Create("six-again-en", true, "en", ["Again", "Again", "Again", "Again", "Again", "Again"]),
        Create("repeated-again-ru", true, "ru", ["Again", "Again", "Hard", "Again"]),
        Create("multiword-again-en", true, "en", ["Again", "Again", "Again"], true),
        Create("hard-after-again-en", true, "en", ["Again", "Again", "Again", "Again", "Again", "Hard"])
    ];
    private static EvaluationCase Create(string id, bool help, string language, string[] outcomes, bool multiword = false)
    {
        var completed = DateTimeOffset.Parse("2026-11-01T12:00:00Z");
        var attempts = outcomes.Select((outcome, n) => new CoachAttemptContext {
            Id = Guid.Parse($"00000000-0000-0000-0000-{n + 1:D12}"), Outcome = outcome,
            LearningRevision = 2, ReviewedAt = completed.AddMinutes(n - outcomes.Length), ResponseTimeMs = 8000 }).ToList();
        var content = new List<CoachWordContext> { new() { Value = "eventually", Translation = "в конце концов" } };
        if (multiword) content.Add(new() { Value = "never", Translation = "никогда" });
        // Build consistent observed state with the same deterministic calculator as production.
        var state = new LearnerStateCalculator().Calculate(new Card { Id = 7, LearningRevision = 2,
            ReviewCount = attempts.Count, IntervalDays = help || outcomes[^1] == "Again" ? 0 : 7,
            EaseFactor = 2.5m, DueDate = completed.AddDays(1) },
            attempts.Select(a => new LearningAttempt { Id = a.Id, CardId = 7, LearningRevision = 2,
                Outcome = Enum.Parse<ReviewOutcome>(a.Outcome), ReviewedAt = a.ReviewedAt }), completed);
        var context = new CoachSessionContext {
            Language = language, CompletedAt = completed,
            Summary = new() { Planned = 1, Completed = 1, Successful = outcomes[^1] == "Again" ? 0 : 1, Failed = outcomes[^1] == "Again" ? 1 : 0 },
            Cards = [new CoachCardContext { CardId = 7, LearningRevision = 2, CurrentContent = content,
                CurrentRevisionEvidenceCount = attempts.Count, RecentAttempts = attempts.AsEnumerable().Reverse().ToList(), SessionAttempt = attempts[^1],
                CurrentLearnerState = new LearnerCardStateDto { CardId = 7, LearningRevision = 2,
                    Source = state.Source, ModelVersion = state.ModelVersion, Status = state.Status.ToString(),
                    MasteryScore = state.MasteryScore, DifficultyScore = state.DifficultyScore,
                    AttemptsCount = state.AttemptsCount, SuccessfulAttempts = state.SuccessfulAttempts,
                    FailedAttempts = state.FailedAttempts, ConsecutiveSuccesses = state.ConsecutiveSuccesses,
                    ConsecutiveFailures = state.ConsecutiveFailures, LastOutcome = state.LastOutcome?.ToString(), LastAttemptAt = state.LastAttemptAt } }]
        };
        return new(id, help, context);
    }
}

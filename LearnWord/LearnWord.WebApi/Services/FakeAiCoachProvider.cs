using System.Text.Json;
using LearnWord.BL.Models.Dto;
using LearnWord.WebApi.Abstractions;
namespace LearnWord.WebApi.Services;

public class FakeAiCoachProvider : IAiCoachProvider
{
    public string Provider => "Fake";
    public string Model => "deterministic-coach-v1";
    public Task<string> Analyze(CoachSessionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ru = context.Language == "ru";
        var card = context.Cards.FirstOrDefault(c => c.RecentAttempts.Count(a => a.Outcome == "Again") >= 2);
        var result = new CoachAnalysisResult { Decision = "None" };
        if (card != null)
        {
            var word = card.CurrentContent.First();
            result.Decision = "Intervention";
            result.Summary = ru ? "Одной карточке стоит уделить немного внимания." : "One card could use a little extra attention.";
            result.Observations.Add(new CoachObservationResult { CardId = card.CardId, LearningRevision = card.LearningRevision, Type = "RepeatedFailure",
                EvidenceAttemptIds = card.RecentAttempts.Where(a => a.Outcome == "Again").Select(a => a.Id).ToList(),
                EvidenceSummary = ru ? "Как минимум две попытки отмечены «Снова»." : "At least two reviews were marked Again.",
                Explanation = ru ? "Короткий разбор и отдельная практика могут помочь вспомнить перевод." : "A short explanation and separate practice may help recall the translation." });
            result.Interventions.Add(new CoachInterventionResult { CardId = card.CardId, LearningRevision = card.LearningRevision, Type = "MicroLesson", Title = word.Value,
                Lesson = new CoachLessonDto { Explanation = ru ? $"Связь для повторения: {word.Value} — {word.Translation}." : $"Recall this connection: {word.Value} — {word.Translation}.",
                    Tips = new() { ru ? "Прочитайте пару, затем закройте перевод и попробуйте вспомнить его." : "Read the pair, then cover the translation and try to recall it." } } });
            result.Interventions.Add(new CoachInterventionResult { CardId = card.CardId, LearningRevision = card.LearningRevision, Type = "ExtraPractice",
                Title = ru ? "Короткая практика" : "Short practice", Exercises = new() { new CoachExerciseResult { Prompt = word.Value,
                    Options = new() { new CoachOptionDto { Id = "a", Text = word.Translation }, new CoachOptionDto { Id = "b", Text = ru ? "Другой перевод" : "A different meaning" } },
                    CorrectOptionId = "a", Explanation = $"{word.Value} — {word.Translation}" },
                    new CoachExerciseResult { Prompt = word.Value,
                        Options = new() { new CoachOptionDto { Id = "a", Text = ru ? "Другой перевод" : "A different meaning" }, new CoachOptionDto { Id = "b", Text = word.Translation } },
                        CorrectOptionId = "b", Explanation = $"{word.Value} — {word.Translation}" } } });
        }
        return Task.FromResult(JsonSerializer.Serialize(result, CoachOutputValidator.Json));
    }
}

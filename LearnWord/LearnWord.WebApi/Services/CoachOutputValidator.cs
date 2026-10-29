using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using LearnWord.BL.Models.Errors;
namespace LearnWord.WebApi.Services;

public static class CoachOutputValidator
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 16 };
    public static CoachAnalysisResult ParseAndValidate(string json, CoachSessionContext context)
    {
        try
        {
            Check(!string.IsNullOrWhiteSpace(json) && json.Length <= CoachLimits.ResponseCharacters);
            var result = JsonSerializer.Deserialize<CoachAnalysisResult>(json, Json)!;
            Check(result != null && result.Observations != null && result.Interventions != null);
            Check(result.Decision is "None" or "Intervention");
            Text(result.Summary, CoachLimits.Prose, result.Decision == "None");
            Check(result.Observations.Count <= CoachLimits.Observations && result.Interventions.Count <= CoachLimits.Interventions);
            if (result.Decision == "None") { Check(result.Observations.Count == 0 && result.Interventions.Count == 0); return result; }
            Check(result.Observations.Count > 0 && result.Interventions.Count > 0);
            var seen = new HashSet<int>();
            foreach (var o in result.Observations)
            {
                Check(o != null && seen.Add(o.CardId));
                var card = Card(context, o.CardId, o.LearningRevision);
                Text(o.EvidenceSummary, CoachLimits.Prose); Text(o.Explanation, CoachLimits.Prose);
                Check(o.EvidenceAttemptIds != null && o.EvidenceAttemptIds.Count > 0 && o.EvidenceAttemptIds.Count <= CoachLimits.RecentAttempts && o.EvidenceAttemptIds.Distinct().Count() == o.EvidenceAttemptIds.Count);
                var evidence = o.EvidenceAttemptIds.Select(id => card.RecentAttempts.SingleOrDefault(a => a.Id == id)).ToList();
                Check(evidence.All(a => a != null));
                var failures = evidence.Count(a => a!.Outcome == "Again");
                Check(o.Type switch {
                    "RepeatedFailure" => failures >= 2,
                    "SlowRecall" => evidence.Count(a => a!.ResponseTimeMs >= 15000) >= 2,
                    "LowMastery" => card.CurrentLearnerState.MasteryScore < .4m && card.CurrentRevisionEvidenceCount >= 2,
                    "RepeatedHard" => evidence.Count(a => a!.Outcome == "Hard") >= 2,
                    "WeakAfterMultipleAttempts" => card.CurrentLearnerState.Status == "Weak" && card.CurrentRevisionEvidenceCount >= 2,
                    "RecentRegression" => evidence.Any(a => a!.Outcome == "Again") && evidence.Any(a => (a!.Outcome is "Good" or "Easy") && a.ReviewedAt < evidence.Where(b => b!.Outcome == "Again").Max(b => b!.ReviewedAt)),
                    _ => false });
            }
            var interventionKeys = new HashSet<string>();
            foreach (var i in result.Interventions)
            {
                Check(i != null && interventionKeys.Add($"{i.CardId}:{i.Type}"));
                Card(context, i.CardId, i.LearningRevision);
                Check(seen.Contains(i.CardId)); Text(i.Title, CoachLimits.Title);
                Check(i.Type is "Explanation" or "MicroLesson" or "ExtraPractice");
                Check(i.Exercises != null && i.Exercises.Count <= CoachLimits.Exercises);
                if (i.Type is "Explanation" or "MicroLesson")
                {
                    Check(i.Lesson != null && i.Exercises.Count == 0);
                    Text(i.Lesson.Explanation, CoachLimits.Prose);
                    Check(i.Lesson.Examples != null && i.Lesson.Examples.Count <= CoachLimits.Examples && i.Lesson.Tips != null && i.Lesson.Tips.Count <= CoachLimits.Tips);
                    foreach (var e in i.Lesson.Examples) { Text(e.Source, CoachLimits.ShortText); Text(e.Target, CoachLimits.ShortText); }
                    foreach (var t in i.Lesson.Tips) Text(t, CoachLimits.ShortText);
                }
                else
                {
                    Check(i.Lesson == null && i.Exercises.Count > 0);
                    foreach (var e in i.Exercises)
                    {
                        Check(e != null && e.Id == Guid.Empty && e.Type == "RecognitionChoice"); Text(e.Prompt, CoachLimits.ShortText);
                        Check(e.Options != null && e.Options.Count >= CoachLimits.MinOptions && e.Options.Count <= CoachLimits.MaxOptions);
                        Check(e.Options.Select(o => o.Id).Distinct(StringComparer.Ordinal).Count() == e.Options.Count);
                        Check(e.Options.Select(o => o.Text).Distinct(StringComparer.OrdinalIgnoreCase).Count() == e.Options.Count);
                        foreach (var option in e.Options) { Text(option.Id, 24); Check(option.Id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')); Text(option.Text, CoachLimits.ShortText); }
                        Check(e.Options.Count(o => o.Id == e.CorrectOptionId) == 1);
                        // Recognition answers must be grounded in actual translation content, not invented meanings.
                        var card = Card(context, i.CardId, i.LearningRevision);
                        Check(card.CurrentContent.Any(w => w.Value == e.Prompt && string.Equals(w.Translation.Trim(), e.Options.Single(o => o.Id == e.CorrectOptionId).Text.Trim(), StringComparison.OrdinalIgnoreCase)));
                        Text(e.Explanation, CoachLimits.Prose, true);
                    }
                }
            }
            return result;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or NullReferenceException or ArgumentException)
        { throw Invalid(); }
    }
    private static CoachCardContext Card(CoachSessionContext context, int id, int revision)
    {
        var c = context.Cards.SingleOrDefault(c => c.CardId == id && c.LearningRevision == revision);
        Check(c != null); return c!;
    }
    private static void Text(string value, int max, bool empty = false) => Check(value != null && value.Length <= max && (empty || !string.IsNullOrWhiteSpace(value)) && !value.Any(c => char.IsControl(c) && c is not '\n' and not '\r' and not '\t'));
    private static void Check([DoesNotReturnIf(false)] bool condition) { if (!condition) throw Invalid(); }
    private static UpstreamServiceException Invalid() => new("AI Coach returned invalid structured output.", "ai_provider_invalid_response");
}

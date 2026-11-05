using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace LearnWord.WebApi.Services;

public static class CoachOutputValidator
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 16 };
    public static CoachAnalysisResult ParseAndValidate(string json, CoachSessionContext context)
    {
        Check(!string.IsNullOrWhiteSpace(json), "output_empty", "$");
        Check(json.Length <= CoachLimits.ResponseCharacters, "output_too_large", "$");
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
            var schema = JsonSerializer.SerializeToElement(OpenRouterAiCoachProvider.ResponseSchema());
            Shape(document.RootElement, schema, "$");
        }
        catch (JsonException ex)
        {
            var failure = CoachFailure.Invalid("json_malformed", "$");
            throw CoachFailure.WithDetails(failure, CoachFailure.Details(failure)! with {
                JsonLineNumber = SafePosition(ex.LineNumber), JsonBytePositionInLine = SafePosition(ex.BytePositionInLine) });
        }
        CoachAnalysisResult result;
        try { result = JsonSerializer.Deserialize<CoachAnalysisResult>(json, Json)!; }
        catch (JsonException) { throw CoachFailure.Invalid("schema_value_invalid", "$"); }
        Text(result.Summary, CoachLimits.Prose, "$.summary", result.Decision == "None");
        if (result.Decision == "None")
        {
            Check(result.Observations.Count == 0, "none_has_observations", "$.observations");
            Check(result.Interventions.Count == 0, "none_has_interventions", "$.interventions"); return result;
        }
        Check(result.Observations.Count > 0, "intervention_requires_observations", "$.observations");
        Check(result.Interventions.Count > 0, "intervention_requires_interventions", "$.interventions");
        var seen = new HashSet<int>();
        for (var n = 0; n < result.Observations.Count; n++)
        {
            var o = result.Observations[n]; var p = $"$.observations[{n}]";
            Check(seen.Add(o.CardId), "observation_duplicate_card", p + ".cardId");
            var card = Card(context, o.CardId, o.LearningRevision, p);
            Text(o.EvidenceSummary, CoachLimits.Prose, p + ".evidenceSummary"); Text(o.Explanation, CoachLimits.Prose, p + ".explanation");
            Check(o.EvidenceAttemptIds.Distinct().Count() == o.EvidenceAttemptIds.Count, "evidence_duplicate", p + ".evidenceAttemptIds");
            var evidence = new List<CoachAttemptContext>();
            for (var a = 0; a < o.EvidenceAttemptIds.Count; a++)
            {
                var attempt = card.RecentAttempts.SingleOrDefault(x => x.Id == o.EvidenceAttemptIds[a]);
                Check(attempt != null, "evidence_unknown_attempt", p + $".evidenceAttemptIds[{a}]"); evidence.Add(attempt);
            }
            Check(o.Type switch {
                "RepeatedFailure" => evidence.Count(a => a.Outcome == "Again") >= 2,
                "SlowRecall" => evidence.Count(a => a.ResponseTimeMs >= 15000) >= 2,
                "LowMastery" => card.CurrentLearnerState.MasteryScore < .4m && card.CurrentRevisionEvidenceCount >= 2,
                "RepeatedHard" => evidence.Count(a => a.Outcome == "Hard") >= 2,
                "WeakAfterMultipleAttempts" => card.CurrentLearnerState.Status == "Weak" && card.CurrentRevisionEvidenceCount >= 2,
                "RecentRegression" => evidence.Any(a => a.Outcome == "Again") && evidence.Any(a => (a.Outcome is "Good" or "Easy") && a.ReviewedAt < evidence.Where(b => b.Outcome == "Again").Max(b => b.ReviewedAt)),
                _ => false }, "observation_evidence_insufficient", p + ".type");
        }
        var interventionKeys = new HashSet<string>();
        for (var n = 0; n < result.Interventions.Count; n++)
        {
            var i = result.Interventions[n]; var p = $"$.interventions[{n}]";
            Check(interventionKeys.Add($"{i.CardId}:{i.Type}"), "intervention_duplicate", p);
            var card = Card(context, i.CardId, i.LearningRevision, p);
            Check(seen.Contains(i.CardId), "intervention_without_observation", p + ".cardId"); Text(i.Title, CoachLimits.Title, p + ".title");
            if (i.Type is "Explanation" or "MicroLesson")
            {
                Check(i.Lesson != null, "lesson_required", p + ".lesson");
                Check(i.Exercises.Count == 0, "lesson_has_exercises", p + ".exercises");
                Text(i.Lesson.Explanation, CoachLimits.Prose, p + ".lesson.explanation");
                for (var e = 0; e < i.Lesson.Examples.Count; e++)
                { Text(i.Lesson.Examples[e].Source, CoachLimits.ShortText, p + $".lesson.examples[{e}].source"); Text(i.Lesson.Examples[e].Target, CoachLimits.ShortText, p + $".lesson.examples[{e}].target"); }
                for (var t = 0; t < i.Lesson.Tips.Count; t++) Text(i.Lesson.Tips[t], CoachLimits.ShortText, p + $".lesson.tips[{t}]");
            }
            else
            {
                Check(i.Lesson == null, "practice_has_lesson", p + ".lesson");
                Check(i.Exercises.Count > 0, "practice_requires_exercises", p + ".exercises");
                for (var x = 0; x < i.Exercises.Count; x++)
                {
                    var e = i.Exercises[x]; var ep = p + $".exercises[{x}]";
                    Check(e.Id == Guid.Empty, "exercise_id_not_allowed", ep + ".id"); Text(e.Prompt, CoachLimits.ShortText, ep + ".prompt");
                    Check(e.Options.Select(o => o.Id).Distinct(StringComparer.Ordinal).Count() == e.Options.Count, "option_id_duplicate", ep + ".options");
                    Check(e.Options.Select(o => o.Text).Distinct(StringComparer.OrdinalIgnoreCase).Count() == e.Options.Count, "option_text_duplicate", ep + ".options");
                    for (var z = 0; z < e.Options.Count; z++)
                    {
                        var option = e.Options[z]; var op = ep + $".options[{z}]";
                        Text(option.Id, 24, op + ".id"); Check(option.Id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'), "option_id_invalid", op + ".id"); Text(option.Text, CoachLimits.ShortText, op + ".text");
                    }
                    Check(e.Options.Count(o => o.Id == e.CorrectOptionId) == 1, "correct_option_unknown", ep + ".correctOptionId");
                    var words = card.CurrentContent.Where(w => w.Value == e.Prompt).ToList();
                    Check(words.Count > 0, "exercise_prompt_mismatch", ep + ".prompt");
                    Check(words.Any(w => string.Equals(w.Translation.Trim(), e.Options.Single(o => o.Id == e.CorrectOptionId).Text.Trim(), StringComparison.OrdinalIgnoreCase)), "exercise_answer_mismatch", ep + ".correctOptionId");
                    Text(e.Explanation, CoachLimits.Prose, ep + ".explanation", true);
                }
            }
        }
        return result;
    }
    // Traverse only server-owned schema keys. Unknown keys are reported at their containing object;
    // neither exception text nor an attacker-controlled property name can enter diagnostic paths.
    private static void Shape(JsonElement value, JsonElement schema, string path)
    {
        if (schema.TryGetProperty("anyOf", out var alternatives))
        {
            // Alternatives are server-owned intervention objects, selected only by their known type enum.
            // Do not try branches and swallow errors: that would hide a malformed selected alternative.
            Check(value.ValueKind == JsonValueKind.Object, "schema_type_invalid", path);
            Check(value.TryGetProperty("type", out var discriminator), "schema_required_missing", path + ".type");
            Check(discriminator.ValueKind == JsonValueKind.String, "schema_type_invalid", path + ".type");
            JsonElement? selected = null;
            foreach (var alternative in alternatives.EnumerateArray())
                if (alternative.GetProperty("properties").GetProperty("type").GetProperty("enum")
                    .EnumerateArray().Any(t => t.GetString() == discriminator.GetString())) selected = alternative;
            Check(selected.HasValue, "enum_invalid", path + ".type");
            schema = selected.Value;
        }
        var type = schema.GetProperty("type").GetString();
        Check(type switch { "object" => value.ValueKind == JsonValueKind.Object, "array" => value.ValueKind == JsonValueKind.Array, "string" => value.ValueKind == JsonValueKind.String, "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _), "null" => value.ValueKind == JsonValueKind.Null, _ => false }, "schema_type_invalid", path);
        if (type == "object")
        {
            var fields = schema.GetProperty("properties"); var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                // Stored/fake exercise DTOs historically serialize an optional empty server id.
                var optionalId = path.Contains(".exercises[") && !path.Contains(".options[") && property.Name == "id";
                Check(fields.TryGetProperty(property.Name, out _) || optionalId, "schema_unknown_property", path);
                Check(seen.Add(property.Name), "schema_duplicate_property", path);
                if (optionalId)
                { Check(property.Value.ValueKind == JsonValueKind.String && property.Value.TryGetGuid(out _), "schema_value_invalid", path + ".id"); }
            }
            foreach (var name in schema.GetProperty("required").EnumerateArray())
                Check(value.TryGetProperty(name.GetString()!, out _), "schema_required_missing", path + "." + name.GetString());
            if (fields.TryGetProperty("lesson", out var lessonSchema) && fields.TryGetProperty("exercises", out var exercisesSchema))
            {
                // Keep established diagnostic rules while enforcing these same constraints in the wire schema.
                var practice = lessonSchema.GetProperty("type").GetString() == "null";
                Check(!practice || value.GetProperty("lesson").ValueKind == JsonValueKind.Null, "practice_has_lesson", path + ".lesson");
                Check(practice || value.GetProperty("lesson").ValueKind != JsonValueKind.Null, "lesson_required", path + ".lesson");
                var exercises = value.GetProperty("exercises");
                if (exercises.ValueKind == JsonValueKind.Array)
                {
                    Check(practice || exercises.GetArrayLength() == 0, "lesson_has_exercises", path + ".exercises");
                    Check(!practice || exercises.GetArrayLength() > 0, "practice_requires_exercises", path + ".exercises");
                }
            }
            foreach (var field in fields.EnumerateObject())
                if (value.TryGetProperty(field.Name, out var child)) Shape(child, field.Value, path + "." + field.Name);
        }
        else if (type == "array")
        {
            var length = value.GetArrayLength();
            Check(!schema.TryGetProperty("maxItems", out var max) || length <= max.GetInt32(), "array_too_large", path);
            Check(!schema.TryGetProperty("minItems", out var min) || length >= min.GetInt32(), "array_too_small", path);
            for (var i = 0; i < length; i++) Shape(value[i], schema.GetProperty("items"), path + $"[{i}]");
        }
        else if (type == "string")
        {
            if (schema.TryGetProperty("enum", out var allowed)) Check(allowed.EnumerateArray().Any(a => a.GetString() == value.GetString()), "enum_invalid", path);
            if (path.Contains(".evidenceAttemptIds[")) Check(value.TryGetGuid(out _), "schema_value_invalid", path);
            if (schema.TryGetProperty("maxLength", out var max)) Check(value.GetString()!.Length <= max.GetInt32(), "text_too_long", path);
        }
    }
    private static CoachCardContext Card(CoachSessionContext context, int id, int revision, string path)
    {
        var c = context.Cards.SingleOrDefault(c => c.CardId == id);
        Check(c != null, "card_unknown", path + ".cardId");
        Check(c.LearningRevision == revision, "card_revision_mismatch", path + ".learningRevision"); return c;
    }
    private static void Text(string value, int max, string path, bool empty = false)
    {
        Check(value.Length <= max, "text_too_long", path);
        Check(empty || !string.IsNullOrWhiteSpace(value), "text_empty", path);
        Check(!value.Any(c => char.IsControl(c) && c is not '\n' and not '\r' and not '\t'), "text_control_character", path);
    }
    private static void Check([DoesNotReturnIf(false)] bool condition, string code, string path) { if (!condition) throw CoachFailure.Invalid(code, path); }
    private static long? SafePosition(long? position) => position is >= 0 and <= CoachLimits.ResponseCharacters * 8 ? position : null;
}

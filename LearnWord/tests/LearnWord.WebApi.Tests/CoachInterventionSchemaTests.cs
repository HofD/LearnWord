using System.Text.Json;
using System.Text.Json.Nodes;
using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Services;
using Xunit;

namespace LearnWord.WebApi.Tests;

public class CoachInterventionSchemaTests
{
    [Theory]
    [InlineData("Explanation")]
    [InlineData("MicroLesson")]
    [InlineData("ExtraPractice")]
    public void AllSupportedAlternativesRetainTheirOwnValidPayload(string type)
    {
        var result = CoachOutputValidatorTests.Result(type == "ExtraPractice");
        result.Interventions[0].Type = type;
        var actual = CoachOutputValidator.ParseAndValidate(CoachOutputValidatorTests.Serialize(result), CoachOutputValidatorTests.Context());
        Assert.Equal(type, Assert.Single(actual.Interventions).Type);
    }

    [Theory]
    [InlineData("Explanation")]
    [InlineData("MicroLesson")]
    public void ACompleteLessonCannotAlsoCarryPracticeEvenIfBothPartsAreIndividuallyValid(string type)
    {
        var output = CoachOutputValidatorTests.Result();
        output.Interventions[0].Type = type;
        output.Interventions[0].Exercises = CoachOutputValidatorTests.Result(true).Interventions[0].Exercises;
        Failure(Node(output), "lesson_has_exercises", "$.interventions[0].exercises");
    }

    [Fact]
    public void SeparateLessonAndPracticeForTheSameCardAreValidWithOneObservation()
    {
        var result = CoachOutputValidatorTests.Result();
        result.Interventions.Add(CoachOutputValidatorTests.Result(true).Interventions[0]);
        var actual = CoachOutputValidator.ParseAndValidate(CoachOutputValidatorTests.Serialize(result), CoachOutputValidatorTests.Context());
        Assert.Single(actual.Observations);
        Assert.Equal(2, actual.Interventions.Count);
    }

    [Theory]
    [InlineData("lesson-null", "lesson_required", "$.interventions[0].lesson")]
    [InlineData("practice-lesson", "practice_has_lesson", "$.interventions[0].lesson")]
    [InlineData("practice-empty", "practice_requires_exercises", "$.interventions[0].exercises")]
    [InlineData("practice-too-many", "array_too_large", "$.interventions[0].exercises")]
    [InlineData("type-missing", "schema_required_missing", "$.interventions[0].type")]
    [InlineData("type-unknown", "enum_invalid", "$.interventions[0].type")]
    [InlineData("type-null", "schema_type_invalid", "$.interventions[0].type")]
    [InlineData("type-object", "schema_type_invalid", "$.interventions[0].type")]
    [InlineData("unknown-practice-key", "schema_unknown_property", "$.interventions[0]")]
    [InlineData("unknown-lesson-key", "schema_unknown_property", "$.interventions[0]")]
    [InlineData("missing-practice-lesson", "schema_required_missing", "$.interventions[0].lesson")]
    [InlineData("missing-lesson-exercises", "schema_required_missing", "$.interventions[0].exercises")]
    [InlineData("null-intervention", "schema_type_invalid", "$.interventions[0]")]
    public void SelectedAlternativeRejectsInvalidFieldsWithoutTryingAnotherBranch(string mutation, string code, string path)
    {
        var practice = mutation.StartsWith("practice") || mutation is "unknown-practice-key" or "missing-practice-lesson";
        var output = Node(CoachOutputValidatorTests.Result(practice));
        var intervention = output["interventions"]![0]!.AsObject();
        switch (mutation)
        {
            case "lesson-null": intervention["lesson"] = null; break;
            case "practice-lesson": intervention["lesson"] = Node(CoachOutputValidatorTests.Result())["interventions"]![0]!["lesson"]!.DeepClone(); break;
            case "practice-empty": intervention["exercises"] = new JsonArray(); break;
            case "practice-too-many":
                var exercises = intervention["exercises"]!.AsArray();
                while (exercises.Count <= CoachLimits.Exercises) exercises.Add(exercises[0]!.DeepClone());
                break;
            case "type-missing": intervention.Remove("type"); break;
            case "type-unknown": intervention["type"] = "PRIVATE_UNKNOWN_TYPE"; break;
            case "type-null": intervention["type"] = null; break;
            case "type-object": intervention["type"] = new JsonObject { ["PRIVATE_KEY"] = "PRIVATE_VALUE" }; break;
            case "unknown-practice-key": case "unknown-lesson-key": intervention["PRIVATE_KEY"] = "PRIVATE_VALUE"; break;
            case "missing-practice-lesson": intervention.Remove("lesson"); break;
            case "missing-lesson-exercises": intervention.Remove("exercises"); break;
            case "null-intervention": output["interventions"]![0] = null; break;
        }
        Failure(output, code, path);
    }

    [Fact]
    public void DifferentSupportedReasonsStillCannotDuplicateObservationForOneCard()
    {
        var result = CoachOutputValidatorTests.Result();
        var duplicate = CoachOutputValidatorTests.Result().Observations[0];
        duplicate.Type = "LowMastery";
        result.Observations.Add(duplicate);
        Failure(Node(result), "observation_duplicate_card", "$.observations[1].cardId");
    }

    [Fact]
    public void WireSchemaExpressesMutuallyExclusiveBranchesAndDoesNotClaimUniqueItemsMeansUniqueCard()
    {
        var schema = JsonSerializer.SerializeToElement(OpenRouterAiCoachProvider.ResponseSchema());
        var properties = schema.GetProperty("properties");
        var observations = properties.GetProperty("observations");
        Assert.Contains("one observation per cardId", observations.GetProperty("description").GetString());
        Assert.False(observations.TryGetProperty("uniqueItems", out _));
        Assert.Contains("Unique across observations", observations.GetProperty("items").GetProperty("properties").GetProperty("cardId").GetProperty("description").GetString());
        var branches = properties.GetProperty("interventions").GetProperty("items").GetProperty("anyOf");
        Assert.Equal(2, branches.GetArrayLength());
        foreach (var branch in branches.EnumerateArray())
        {
            Assert.False(branch.GetProperty("additionalProperties").GetBoolean());
            Assert.Equal(new[] { "cardId", "learningRevision", "type", "title", "lesson", "exercises" }, branch.GetProperty("required").EnumerateArray().Select(x => x.GetString()));
        }
        var lesson = branches[0].GetProperty("properties");
        Assert.Equal(new[] { "Explanation", "MicroLesson" }, lesson.GetProperty("type").GetProperty("enum").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal("object", lesson.GetProperty("lesson").GetProperty("type").GetString());
        Assert.Equal(0, lesson.GetProperty("exercises").GetProperty("maxItems").GetInt32());
        var practice = branches[1].GetProperty("properties");
        Assert.Equal("ExtraPractice", practice.GetProperty("type").GetProperty("enum")[0].GetString());
        Assert.Equal("null", practice.GetProperty("lesson").GetProperty("type").GetString());
        Assert.Equal(1, practice.GetProperty("exercises").GetProperty("minItems").GetInt32());
        Assert.Equal(CoachLimits.Exercises, practice.GetProperty("exercises").GetProperty("maxItems").GetInt32());
        Assert.Equal(lesson.GetProperty("exercises").GetProperty("items").GetRawText(), practice.GetProperty("exercises").GetProperty("items").GetRawText());
        Assert.Contains("at most ONE observation per cardId", OpenRouterAiCoachProvider.SystemPrompt);
        Assert.Contains("separate interventions", OpenRouterAiCoachProvider.SystemPrompt);
    }

    private static JsonNode Node(CoachAnalysisResult result) => JsonNode.Parse(CoachOutputValidatorTests.Serialize(result))!;
    private static void Failure(JsonNode output, string code, string path)
    {
        var error = Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(output.ToJsonString(), CoachOutputValidatorTests.Context()));
        Assert.Equal("ai_provider_invalid_response", error.ErrorCode);
        var details = CoachFailure.Details(error)!;
        Assert.Equal(code, details.ValidationCode);
        Assert.Equal(path, details.ValidationPath);
        Assert.DoesNotContain("PRIVATE_", details.ToString());
    }
}

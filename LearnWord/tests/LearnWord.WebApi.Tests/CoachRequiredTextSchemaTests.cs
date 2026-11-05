using System.Text.Json;
using System.Text.Json.Nodes;
using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Services;
using Xunit;

namespace LearnWord.WebApi.Tests;

public class CoachRequiredTextSchemaTests
{
    [Theory]
    [InlineData(false, "/observations/0/evidenceSummary")]
    [InlineData(false, "/observations/0/explanation")]
    [InlineData(false, "/observations/0/evidenceAttemptIds/0")]
    [InlineData(false, "/interventions/0/title")]
    [InlineData(false, "/interventions/0/lesson/explanation")]
    [InlineData(false, "/interventions/0/lesson/examples/0/source")]
    [InlineData(false, "/interventions/0/lesson/examples/0/target")]
    [InlineData(false, "/interventions/0/lesson/tips/0")]
    [InlineData(true, "/interventions/0/exercises/0/prompt")]
    [InlineData(true, "/interventions/0/exercises/0/options/0/id")]
    [InlineData(true, "/interventions/0/exercises/0/options/0/text")]
    [InlineData(true, "/interventions/0/exercises/0/correctOptionId")]
    public void RequiredTextCannotBeEmptyAndKeepsKnownDiagnosticPath(bool practice, string pointer)
    {
        var output = Node(practice);
        Set(output, pointer, "");
        Failure(output, "text_empty", Path(pointer));
    }

    [Theory]
    [InlineData(false, "/observations/0/evidenceSummary")]
    [InlineData(false, "/observations/0/explanation")]
    [InlineData(false, "/interventions/0/title")]
    [InlineData(false, "/interventions/0/lesson/explanation")]
    [InlineData(false, "/interventions/0/lesson/examples/0/source")]
    [InlineData(false, "/interventions/0/lesson/examples/0/target")]
    [InlineData(false, "/interventions/0/lesson/tips/0")]
    [InlineData(true, "/interventions/0/exercises/0/prompt")]
    [InlineData(true, "/interventions/0/exercises/0/options/0/text")]
    public void WhitespaceStillFailsSemanticValidationEvenThoughMinLengthAloneAllowsIt(bool practice, string pointer)
    {
        var output = Node(practice);
        Set(output, pointer, " \t\r\n");
        Failure(output, "text_empty", Path(pointer));
    }

    [Theory]
    [InlineData("missing", "schema_required_missing")]
    [InlineData("null", "schema_type_invalid")]
    [InlineData("number", "schema_type_invalid")]
    public void RequiredExplanationRetainsMissingNullAndTypeChecks(string mutation, string code)
    {
        var output = Node(false);
        var observation = output["observations"]![0]!.AsObject();
        if (mutation == "missing") observation.Remove("explanation");
        else observation["explanation"] = mutation == "null" ? null : JsonValue.Create(123);
        Failure(output, code, "$.observations[0].explanation");
    }

    [Fact]
    public void NoneStillAllowsAnEmptySummary()
    {
        var result = CoachOutputValidator.ParseAndValidate("""{"decision":"None","summary":"","observations":[],"interventions":[]}""", CoachOutputValidatorTests.Context());
        Assert.Equal("", result.Summary);
    }

    [Fact]
    public void PracticeStillAllowsAnEmptyExerciseExplanation()
    {
        var output = Node(true);
        Set(output, "/interventions/0/exercises/0/explanation", "");
        var result = CoachOutputValidator.ParseAndValidate(output.ToJsonString(), CoachOutputValidatorTests.Context());
        Assert.Equal("", result.Interventions[0].Exercises[0].Explanation);
    }

    [Fact]
    public void InterventionSummaryRemainsConditionallyRequired()
    {
        var output = Node(false);
        Set(output, "/summary", "");
        Failure(output, "text_empty", "$.summary");
    }

    [Fact]
    public void OptionalLessonExampleAndTipArraysCanRemainEmpty()
    {
        var output = CoachOutputValidatorTests.Result();
        output.Interventions[0].Lesson!.Examples.Clear();
        output.Interventions[0].Lesson!.Tips.Clear();
        Assert.Empty(CoachOutputValidator.ParseAndValidate(CoachOutputValidatorTests.Serialize(output), CoachOutputValidatorTests.Context()).Interventions[0].Lesson!.Examples);
    }

    [Fact]
    public void GroundedOneCharacterTextRemainsValid()
    {
        var output = CoachOutputValidatorTests.Result(true);
        var context = CoachOutputValidatorTests.Context();
        context.Cards[0].CurrentContent[0].Value = "x";
        context.Cards[0].CurrentContent[0].Translation = "y";
        output.Summary = "s"; output.Observations[0].EvidenceSummary = "e"; output.Observations[0].Explanation = "r";
        output.Interventions[0].Title = "t";
        var exercise = output.Interventions[0].Exercises[0];
        exercise.Prompt = "x"; exercise.Options[0].Text = "y"; exercise.Options[1].Text = "z"; exercise.Explanation = "";
        Assert.Equal("Intervention", CoachOutputValidator.ParseAndValidate(CoachOutputValidatorTests.Serialize(output), context).Decision);
    }

    [Fact]
    public void WireSchemaConstrainsEveryRequiredTextLeafButPreservesIntentionalEmptyFields()
    {
        var schema = JsonSerializer.SerializeToNode(OpenRouterAiCoachProvider.ResponseSchema())!;
        var properties = schema["properties"]!;
        var observation = properties["observations"]!["items"]!["properties"]!;
        Min(observation["evidenceSummary"]!, 1); Min(observation["explanation"]!, 1);
        Min(properties["observations"]!["items"]!["properties"]!["evidenceAttemptIds"]!["items"]!, 1);
        Min(properties["summary"]!, 0);
        foreach (var branch in properties["interventions"]!["items"]!["anyOf"]!.AsArray())
        {
            var fields = branch!["properties"]!;
            Min(fields["title"]!, 1);
            var exercise = fields["exercises"]!["items"]!["properties"]!;
            Min(exercise["prompt"]!, 1); Min(exercise["correctOptionId"]!, 1); Min(exercise["explanation"]!, 0);
            Min(exercise["options"]!["items"]!["properties"]!["id"]!, 1);
            Min(exercise["options"]!["items"]!["properties"]!["text"]!, 1);
            if (fields["lesson"]!["type"]!.GetValue<string>() == "object")
            {
                var lesson = fields["lesson"]!["properties"]!;
                Min(lesson["explanation"]!, 1); Min(lesson["tips"]!["items"]!, 1);
                Min(lesson["examples"]!["items"]!["properties"]!["source"]!, 1);
                Min(lesson["examples"]!["items"]!["properties"]!["target"]!, 1);
            }
        }
        Assert.Contains("observation.explanation", OpenRouterAiCoachProvider.SystemPrompt);
        Assert.Contains("never an empty or whitespace-only string", OpenRouterAiCoachProvider.SystemPrompt);
    }

    private static void Min(JsonNode schema, int min)
    {
        Assert.Equal(min, schema["minLength"]!.GetValue<int>());
        Assert.True(schema["maxLength"]!.GetValue<int>() > min);
        Assert.False(schema.AsObject().ContainsKey("pattern"));
    }
    private static JsonNode Node(bool practice) => JsonNode.Parse(CoachOutputValidatorTests.Serialize(CoachOutputValidatorTests.Result(practice)))!;
    private static void Set(JsonNode root, string pointer, string value)
    {
        var segments = pointer.Split('/').Skip(1).ToArray(); var node = root;
        foreach (var segment in segments[..^1]) node = int.TryParse(segment, out var n) ? node[n]! : node[segment]!;
        if (int.TryParse(segments[^1], out var index)) node[index] = value; else node[segments[^1]] = value;
    }
    private static string Path(string pointer) => "$" + string.Concat(pointer.Split('/').Skip(1).Select(s => int.TryParse(s, out _) ? $"[{s}]" : $".{s}"));
    private static void Failure(JsonNode output, string code, string path)
    {
        var error = Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(output.ToJsonString(), CoachOutputValidatorTests.Context()));
        Assert.Equal("ai_provider_invalid_response", error.ErrorCode);
        var detail = CoachFailure.Details(error)!;
        Assert.Equal("OutputValidation", detail.FailureStage); Assert.Equal(code, detail.ValidationCode); Assert.Equal(path, detail.ValidationPath);
        Assert.DoesNotContain("PRIVATE_", detail.ToString());
    }
}

using System.Text.Json.Nodes;
using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Services;
using Xunit;

namespace LearnWord.WebApi.Tests;

public class CoachValidationDiagnosticsTests
{
    [Theory]
    [InlineData("not json PRIVATE_RAW", "json_malformed", "$")]
    [InlineData("null", "schema_type_invalid", "$")]
    [InlineData("[]", "schema_type_invalid", "$")]
    [InlineData("", "output_empty", "$")]
    [InlineData("{\"decision\":\"None\",\"observations\":[],\"interventions\":[]}", "schema_required_missing", "$.summary")]
    [InlineData("{\"decision\":\"PRIVATE_ENUM\",\"summary\":\"\",\"observations\":[],\"interventions\":[]}", "enum_invalid", "$.decision")]
    [InlineData("{\"decision\":\"None\",\"summary\":\"\",\"observations\":null,\"interventions\":[]}", "schema_type_invalid", "$.observations")]
    [InlineData("{\"decision\":\"None\",\"summary\":\"\",\"observations\":[],\"interventions\":[],\"PRIVATE_PROPERTY\":\"PRIVATE_VALUE\"}", "schema_unknown_property", "$")]
    [InlineData("{\"decision\":\"None\",\"decision\":\"None\",\"summary\":\"\",\"observations\":[],\"interventions\":[]}", "schema_duplicate_property", "$")]
    public void ParsingFailuresExposeOnlyKnownRuleAndSchemaPath(string json, string code, string path)
        => Failure(json, code, path);

    [Theory]
    [InlineData("question", "exercise_prompt_mismatch", "$.interventions[0].exercises[0].prompt")]
    [InlineData("wrong-translation", "exercise_answer_mismatch", "$.interventions[0].exercises[0].correctOptionId")]
    [InlineData("unknown-attempt", "evidence_unknown_attempt", "$.observations[0].evidenceAttemptIds[0]")]
    [InlineData("one-again", "observation_evidence_insufficient", "$.observations[0].type")]
    [InlineData("title-bound", "text_too_long", "$.interventions[0].title")]
    [InlineData("option-bound", "array_too_small", "$.interventions[0].exercises[0].options")]
    [InlineData("unknown-nested-property", "schema_unknown_property", "$.interventions[0].exercises[0].options[0]")]
    [InlineData("invalid-guid", "schema_value_invalid", "$.observations[0].evidenceAttemptIds[0]")]
    [InlineData("missing-nested", "schema_required_missing", "$.interventions[0].exercises[0].prompt")]
    public void SemanticAndNestedFailuresIdentifyTheExactSafeRule(string mutation, string code, string path)
    {
        var output = JsonNode.Parse(CoachOutputValidatorTests.Serialize(CoachOutputValidatorTests.Result(practice: true)))!;
        var exercise = output["interventions"]![0]!["exercises"]![0]!;
        switch (mutation)
        {
            case "question": exercise["prompt"] = "What does PRIVATE_WORD mean?"; break;
            case "wrong-translation": exercise["correctOptionId"] = "b"; break;
            case "unknown-attempt": output["observations"]![0]!["evidenceAttemptIds"]![0] = Guid.NewGuid().ToString(); break;
            case "one-again": output["observations"]![0]!["evidenceAttemptIds"]!.AsArray().RemoveAt(1); break;
            case "title-bound": output["interventions"]![0]!["title"] = new string('x', CoachLimits.Title + 1); break;
            case "option-bound": exercise["options"]!.AsArray().RemoveAt(1); break;
            case "unknown-nested-property": exercise["options"]![0]!["PRIVATE_PROPERTY"] = "PRIVATE_VALUE"; break;
            case "invalid-guid": output["observations"]![0]!["evidenceAttemptIds"]![0] = "PRIVATE_BAD_GUID"; break;
            case "missing-nested": exercise.AsObject().Remove("prompt"); break;
        }
        Failure(output.ToJsonString(), code, path);
    }

    [Fact]
    public void OversizedArraysAreRejectedBeforeUnboundedIndexesOrUnknownKeysCanEnterPaths()
    {
        var output = JsonNode.Parse(CoachOutputValidatorTests.Serialize(CoachOutputValidatorTests.Result(practice: true)))!;
        var options = output["interventions"]![0]!["exercises"]![0]!["options"]!.AsArray();
        while (options.Count < CoachLimits.MaxOptions + 10)
            options.Add(new JsonObject { ["PRIVATE_PROPERTY"] = "PRIVATE_VALUE" });
        Failure(output.ToJsonString(), "array_too_large", "$.interventions[0].exercises[0].options");
    }

    [Fact]
    public void LastAllowedArrayItemReceivesItsOwnSafeIndex()
    {
        var output = JsonNode.Parse(CoachOutputValidatorTests.Serialize(CoachOutputValidatorTests.Result(practice: true)))!;
        var exercises = output["interventions"]![0]!["exercises"]!.AsArray();
        while (exercises.Count < CoachLimits.Exercises) exercises.Add(exercises[0]!.DeepClone());
        exercises[CoachLimits.Exercises - 1]!["prompt"] = "PRIVATE_QUESTION";
        Failure(output.ToJsonString(), "exercise_prompt_mismatch", $"$.interventions[0].exercises[{CoachLimits.Exercises - 1}].prompt");
    }

    [Fact]
    public void MultiwordGroundingFailureDistinguishesValidPromptFromAnotherWordsAnswer()
    {
        var context = CoachOutputValidatorTests.Context();
        context.Cards[0].CurrentContent.Add(new CoachWordContext { Value = "never", Translation = "никогда" });
        var output = CoachOutputValidatorTests.Result(practice: true);
        output.Interventions[0].Exercises[0].CorrectOptionId = "b";
        Failure(CoachOutputValidatorTests.Serialize(output), "exercise_answer_mismatch",
            "$.interventions[0].exercises[0].correctOptionId", context);
    }

    [Fact]
    public void ArbitraryExceptionDataCannotMasqueradeAsTrustedDiagnosticMetadata()
    {
        var exception = new UpstreamServiceException("private", "ai_provider_invalid_response");
        exception.Data["ValidationPath"] = "PRIVATE_PROPERTY";
        exception.Data["CoachFailureDetails"] = new CoachFailureDetails("PRIVATE_STAGE", "PRIVATE_CODE", "PRIVATE_PATH");
        Assert.Null(CoachFailure.Details(exception));
    }

    private static void Failure(string json, string code, string path, CoachSessionContext? context = null)
    {
        var error = Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(json, context ?? CoachOutputValidatorTests.Context()));
        Assert.Equal("ai_provider_invalid_response", error.ErrorCode);
        var details = Assert.IsType<CoachFailureDetails>(CoachFailure.Details(error));
        Assert.Equal("OutputValidation", details.FailureStage);
        Assert.Equal(code, details.ValidationCode);
        Assert.Equal(path, details.ValidationPath);
        Assert.Null(details.ProviderHttpStatus);
        Assert.DoesNotContain("PRIVATE_", error.Message);
        Assert.DoesNotContain("PRIVATE_", details.ToString());
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Services;
using Xunit;

namespace LearnWord.WebApi.Tests;

public class CoachOutputValidatorTests
{
    private static readonly Guid First = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Second = Guid.Parse("00000000-0000-0000-0000-000000000002");
    internal static CoachSessionContext Context() => new() {
        Cards = [new CoachCardContext {
            CardId = 7, LearningRevision = 2, CurrentRevisionEvidenceCount = 2,
            CurrentContent = [new CoachWordContext { Value = "eventually", Translation = "в конце концов" }],
            CurrentLearnerState = new LearnerCardStateDto { Status = "Weak", MasteryScore = .1m, LearningRevision = 2 },
            RecentAttempts = [new CoachAttemptContext { Id = First, Outcome = "Again", LearningRevision = 2, ResponseTimeMs = 16000 },
                new CoachAttemptContext { Id = Second, Outcome = "Again", LearningRevision = 2, ResponseTimeMs = 18000 }]
        }]
    };
    internal static CoachAnalysisResult Result(bool practice = false) => new() {
        Decision = "Intervention", Summary = "Two difficult reviews.",
        Observations = [new CoachObservationResult { CardId = 7, LearningRevision = 2, Type = "RepeatedFailure",
            EvidenceAttemptIds = [First, Second], EvidenceSummary = "Two Again outcomes.", Explanation = "Try a short recap." }],
        Interventions = [new CoachInterventionResult { CardId = 7, LearningRevision = 2, Type = practice ? "ExtraPractice" : "MicroLesson", Title = "Eventually",
            Lesson = practice ? null : new CoachLessonDto { Explanation = "A result after some time.", Examples = [new CoachExampleDto { Source = "It eventually worked.", Target = "В конце концов это сработало." }], Tips = ["Recall the meaning after a short pause."] },
            Exercises = practice ? [new CoachExerciseResult { Prompt = "eventually", Options = [new CoachOptionDto { Id = "a", Text = "в конце концов" }, new CoachOptionDto { Id = "b", Text = "никогда" }], CorrectOptionId = "a", Explanation = "This is the card translation." }] : []
        }]
    };
    internal static string Serialize(CoachAnalysisResult result) => JsonSerializer.Serialize(result, CoachOutputValidator.Json);

    [Fact]
    public void None_IsAValidFirstClassResult()
    {
        var result = CoachOutputValidator.ParseAndValidate("""{"decision":"None","summary":"","observations":[],"interventions":[]}""", Context());
        Assert.Equal("None", result.Decision);
        Assert.Empty(result.Interventions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidGroundedStructuredLessonAndPracticeAreAccepted(bool practice)
    {
        var result = CoachOutputValidator.ParseAndValidate(Serialize(Result(practice)), Context());
        Assert.Equal(7, Assert.Single(result.Observations).CardId);
        Assert.Equal(practice ? "ExtraPractice" : "MicroLesson", Assert.Single(result.Interventions).Type);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"decision\":\"Maybe\"}")]
    [InlineData("{\"decision\":\"None\",\"observations\":null}")]
    [InlineData("{\"decision\":\"None\",\"changeDueDate\":true}")]
    public void MalformedUnknownEnumsOrUnexpectedFieldsAreRejected(string json)
        => Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(json, Context()));

    [Theory]
    [InlineData("card")]
    [InlineData("revision")]
    [InlineData("unknown-attempt")]
    [InlineData("duplicate-attempt")]
    [InlineData("empty-evidence")]
    [InlineData("duplicate-observation")]
    [InlineData("unknown-observation")]
    [InlineData("unfounded-repeat")]
    [InlineData("unknown-intervention")]
    [InlineData("duplicate-intervention")]
    [InlineData("unobserved-intervention-card")]
    [InlineData("empty-title")]
    [InlineData("oversized-title")]
    [InlineData("empty-lesson")]
    [InlineData("too-many-examples")]
    [InlineData("too-many-tips")]
    [InlineData("too-many-observations")]
    [InlineData("too-many-interventions")]
    [InlineData("none-with-advice")]
    public void UntrustedOutput_RejectsUnsupportedReferencesGroundingAndBounds(string problem)
    {
        var result = Result();
        var observation = result.Observations[0];
        var intervention = result.Interventions[0];
        switch (problem)
        {
            case "card": observation.CardId = 999; break;
            case "revision": observation.LearningRevision = 1; break;
            case "unknown-attempt": observation.EvidenceAttemptIds[0] = Guid.NewGuid(); break;
            case "duplicate-attempt": observation.EvidenceAttemptIds = [First, First]; break;
            case "empty-evidence": observation.EvidenceAttemptIds.Clear(); break;
            case "duplicate-observation": result.Observations.Add(observation); break;
            case "unknown-observation": observation.Type = "ConfusesEventuallyWithFinally"; break;
            case "unfounded-repeat": observation.EvidenceAttemptIds = [First]; break;
            case "unknown-intervention": intervention.Type = "EditCard"; break;
            case "duplicate-intervention": result.Interventions.Add(intervention); break;
            case "unobserved-intervention-card": intervention.CardId = 123; break;
            case "empty-title": intervention.Title = " "; break;
            case "oversized-title": intervention.Title = new string('x', CoachLimits.Title + 1); break;
            case "empty-lesson": intervention.Lesson!.Explanation = ""; break;
            case "too-many-examples": intervention.Lesson!.Examples = Enumerable.Repeat(intervention.Lesson.Examples[0], CoachLimits.Examples + 1).ToList(); break;
            case "too-many-tips": intervention.Lesson!.Tips = Enumerable.Repeat("tip", CoachLimits.Tips + 1).ToList(); break;
            case "too-many-observations": result.Observations = Enumerable.Repeat(observation, CoachLimits.Observations + 1).ToList(); break;
            case "too-many-interventions": result.Interventions = Enumerable.Repeat(intervention, CoachLimits.Interventions + 1).ToList(); break;
            case "none-with-advice": result.Decision = "None"; break;
        }
        var error = Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(Serialize(result), Context()));
        Assert.Equal("ai_provider_invalid_response", error.ErrorCode);
    }

    [Theory]
    [InlineData("unsupported-type")]
    [InlineData("unknown-answer")]
    [InlineData("invented-answer")]
    [InlineData("too-few-options")]
    [InlineData("too-many-options")]
    [InlineData("duplicate-option-id")]
    [InlineData("duplicate-option-text")]
    [InlineData("unsafe-option-id")]
    [InlineData("provider-generated-id")]
    [InlineData("too-many-exercises")]
    [InlineData("practice-with-lesson")]
    [InlineData("empty-prompt")]
    [InlineData("prompt-leaks-answer")]
    [InlineData("prompt-mismatched-word")]
    public void SupplementaryExercises_RequireBoundedServerMappedGroundedChoice(string problem)
    {
        var result = Result(practice: true);
        var intervention = result.Interventions[0];
        var exercise = intervention.Exercises[0];
        switch (problem)
        {
            case "unsupported-type": exercise.Type = "FreeChat"; break;
            case "unknown-answer": exercise.CorrectOptionId = "missing"; break;
            case "invented-answer": exercise.CorrectOptionId = "b"; break;
            case "too-few-options": exercise.Options.RemoveAt(1); break;
            case "too-many-options": exercise.Options = Enumerable.Range(0, CoachLimits.MaxOptions + 1).Select(n => new CoachOptionDto { Id = n.ToString(), Text = n.ToString() }).ToList(); break;
            case "duplicate-option-id": exercise.Options[1].Id = "a"; break;
            case "duplicate-option-text": exercise.Options[1].Text = exercise.Options[0].Text; break;
            case "unsafe-option-id": exercise.Options[1].Id = "<script>"; break;
            case "provider-generated-id": exercise.Id = Guid.NewGuid(); break;
            case "too-many-exercises": intervention.Exercises = Enumerable.Repeat(exercise, CoachLimits.Exercises + 1).ToList(); break;
            case "practice-with-lesson": intervention.Lesson = Result().Interventions[0].Lesson; break;
            case "empty-prompt": exercise.Prompt = ""; break;
            case "prompt-leaks-answer": exercise.Prompt = "Eventually means в конце концов. Pick it."; break;
            case "prompt-mismatched-word": exercise.Prompt = "finally"; break;
        }
        Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(Serialize(result), Context()));
    }

    [Fact]
    public void RecognitionAnswerMustMatchTheExactPromptWordOnMultiwordCards()
    {
        var context = Context();
        context.Cards[0].CurrentContent.Add(new CoachWordContext { Value = "never", Translation = "никогда" });
        var result = Result(practice: true);
        result.Interventions[0].Exercises[0].CorrectOptionId = "b";
        Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(Serialize(result), context));
    }

    [Theory]
    [InlineData("decision")]
    [InlineData("summary")]
    [InlineData("observations")]
    [InlineData("interventions")]
    public void MissingRequiredTopLevelSchemaFieldsAreRejected(string field)
    {
        var json = JsonNode.Parse("{\"decision\":\"None\",\"summary\":\"\",\"observations\":[],\"interventions\":[]}")!.AsObject();
        json.Remove(field);
        Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(json.ToJsonString(), Context()));
    }

    [Fact]
    public void OversizedOrExcessivelyNestedProviderResponseIsRejected()
    {
        Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(new string('x', CoachLimits.ResponseCharacters + 1), Context()));
        var result = Result();
        result.Summary = new string('x', CoachLimits.Prose + 1);
        Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(Serialize(result), Context()));
        Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(new string('[', 20) + new string(']', 20), Context()));
    }

    [Theory]
    [InlineData("SlowRecall")]
    [InlineData("LowMastery")]
    [InlineData("WeakAfterMultipleAttempts")]
    public void AlternativeObservations_AreAcceptedWhenTheirActualEvidenceSupportsThem(string type)
    {
        var result = Result();
        result.Observations[0].Type = type;
        Assert.Equal(type, CoachOutputValidator.ParseAndValidate(Serialize(result), Context()).Observations[0].Type);
    }

    [Theory]
    [InlineData("RepeatedHard")]
    [InlineData("RecentRegression")]
    public void ClaimsWithoutSupportingOutcomeEvidenceAreRejected(string type)
    {
        var result = Result();
        result.Observations[0].Type = type;
        Assert.Throws<UpstreamServiceException>(() => CoachOutputValidator.ParseAndValidate(Serialize(result), Context()));
    }
}

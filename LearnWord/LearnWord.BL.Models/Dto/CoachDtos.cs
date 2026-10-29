using System.Text.Json.Serialization;
namespace LearnWord.BL.Models.Dto;
public class AnalyzeCoachRequest { public string Language { get; set; } = "en"; }
public class CoachAnswerRequest
{
    public string OptionId { get; set; } = "";
    public int? ResponseTimeMs { get; set; }
}
public class CoachAnalysisDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Status { get; set; } = "Pending";
    public string Decision { get; set; } = "None";
    public string Summary { get; set; } = "";
    public string? FailureCode { get; set; }
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public string PromptVersion { get; set; } = "coach-v1";
    public int SchemaVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public List<CoachObservationDto> Observations { get; set; } = new();
    public List<CoachInterventionDto> Interventions { get; set; } = new();
}
public class CoachObservationDto
{
    [JsonRequired]
    public int CardId { get; set; }
    [JsonRequired]
    public int LearningRevision { get; set; }
    [JsonRequired]
    public string Type { get; set; } = "";
    [JsonRequired]
    public string EvidenceSummary { get; set; } = "";
    [JsonRequired]
    public string Explanation { get; set; } = "";
}
public class CoachLessonDto
{
    [JsonRequired]
    public string Explanation { get; set; } = "";
    [JsonRequired]
    public List<CoachExampleDto> Examples { get; set; } = new();
    [JsonRequired]
    public List<string> Tips { get; set; } = new();
}
public class CoachExampleDto { public string Source { get; set; } = ""; public string Target { get; set; } = ""; }
public class CoachOptionDto { public string Id { get; set; } = ""; public string Text { get; set; } = ""; }
public class CoachExerciseDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "RecognitionChoice";
    public string Prompt { get; set; } = "";
    public List<CoachOptionDto> Options { get; set; } = new();
    public bool Answered { get; set; }
    public bool? Correct { get; set; }
}
public class CoachEvaluationDto
{
    public string Status { get; set; } = "Unknown";
    public int PostInterventionAttempts { get; set; }
    public string? FirstPostInterventionOutcome { get; set; }
    public DateTimeOffset? FirstPostInterventionReviewedAt { get; set; }
}
public class CoachInterventionDto
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public int CardId { get; set; }
    public int LearningRevision { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "Suggested";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public CoachLessonDto? Lesson { get; set; }
    public List<CoachExerciseDto> Exercises { get; set; } = new();
    public CoachEvaluationDto Evaluation { get; set; } = new();
}
public class CoachAnswerDto
{
    public Guid ExerciseId { get; set; }
    public bool Correct { get; set; }
    public string Explanation { get; set; } = "";
    public CoachInterventionDto Intervention { get; set; } = new();
}

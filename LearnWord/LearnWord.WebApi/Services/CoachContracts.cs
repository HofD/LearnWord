using LearnWord.BL.Models.Dto;
using System.Text.Json.Serialization;
namespace LearnWord.WebApi.Services;

public static class CoachLimits
{
    public const string PromptVersion = "coach-v6";
    public const int SchemaVersion = 1, Cards = 5, RecentAttempts = 6, Words = 4,
        Observations = 5, Interventions = 3, Exercises = 4, Examples = 3, Tips = 3,
        PromptCharacters = 24000, ResponseCharacters = 16000, Title = 120, Prose = 800,
        ShortText = 240, MinOptions = 2, MaxOptions = 4, MaxRetries = 3, RetrySeconds = 5, LeaseSeconds = 120;
}
public class CoachSessionContext
{
    public int SchemaVersion { get; set; } = CoachLimits.SchemaVersion;
    public string Language { get; set; } = "en";
    public DateTimeOffset CompletedAt { get; set; }
    public CoachSessionSummary Summary { get; set; } = new();
    public int ContentOmittedCards { get; set; }
    public int RevisionMismatchCards { get; set; }
    public List<CoachCardContext> Cards { get; set; } = new();
}
public class CoachSessionSummary
{
    public int Planned { get; set; }
    public int Completed { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
}
public class CoachCardContext
{
    public int CardId { get; set; }
    public int LearningRevision { get; set; }
    public List<CoachWordContext> CurrentContent { get; set; } = new();
    public LearnerCardStateDto CurrentLearnerState { get; set; } = new();
    public int CurrentRevisionEvidenceCount { get; set; }
    public int HistoricalRevisionEvidenceCount { get; set; }
    public List<CoachAttemptContext> RecentAttempts { get; set; } = new();
    public CoachAttemptContext SessionAttempt { get; set; } = new();
}
public class CoachWordContext { public string Value { get; set; } = ""; public string Translation { get; set; } = ""; }
public class CoachAttemptContext
{
    public Guid Id { get; set; }
    public string Outcome { get; set; } = "";
    public DateTimeOffset ReviewedAt { get; set; }
    public int? ResponseTimeMs { get; set; }
    public int LearningRevision { get; set; }
}
public class CoachAnalysisResult
{
    [JsonRequired]
    public string Decision { get; set; } = "";
    [JsonRequired]
    public string Summary { get; set; } = "";
    [JsonRequired]
    public List<CoachObservationResult> Observations { get; set; } = new();
    [JsonRequired]
    public List<CoachInterventionResult> Interventions { get; set; } = new();
}
public class CoachObservationResult : CoachObservationDto
{
    [JsonRequired]
    public List<Guid> EvidenceAttemptIds { get; set; } = new();
}
public class CoachInterventionResult
{
    [JsonRequired]
    public int CardId { get; set; }
    [JsonRequired]
    public int LearningRevision { get; set; }
    [JsonRequired]
    public string Type { get; set; } = "";
    [JsonRequired]
    public string Title { get; set; } = "";
    [JsonRequired]
    public CoachLessonDto? Lesson { get; set; }
    [JsonRequired]
    public List<CoachExerciseResult> Exercises { get; set; } = new();
}
public class CoachExerciseResult
{
    public Guid Id { get; set; }
    [JsonRequired]
    public string Type { get; set; } = "RecognitionChoice";
    [JsonRequired]
    public string Prompt { get; set; } = "";
    [JsonRequired]
    public List<CoachOptionDto> Options { get; set; } = new();
    [JsonRequired]
    public string CorrectOptionId { get; set; } = "";
    [JsonRequired]
    public string Explanation { get; set; } = "";
}

public sealed record CoachProviderResponse(string Content, CoachProviderResponseDiagnosticsDto? ProviderResponse = null);

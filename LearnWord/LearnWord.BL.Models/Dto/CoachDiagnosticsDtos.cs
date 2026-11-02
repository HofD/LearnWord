namespace LearnWord.BL.Models.Dto;

// Private, text-free evidence metadata. No model prose, content, prompt or exercise answers.
public class CoachDiagnosticsDto
{
    public int SchemaVersion { get; set; } = 1;
    public Guid SessionId { get; set; }
    public Guid? AnalysisId { get; set; }
    public string Mode { get; set; } = "ReconstructedNow";
    public DateTimeOffset CapturedAt { get; set; }
    public CoachDiagnosticAnalysisDto Analysis { get; set; } = new();
    public CoachDiagnosticContextDto Context { get; set; } = new();
    public bool? ProviderCalled { get; set; }
    public int? InputCharacters { get; set; }
    public int? OutputCharacters { get; set; }
    public long? DurationMs { get; set; }
    public CoachProviderResponseDiagnosticsDto? ProviderResponse { get; set; }
    public CoachOutputFormatDiagnosticsDto? OutputFormat { get; set; }
    public bool Truncated { get; set; }
}
public class CoachDiagnosticAnalysisDto
{
    public string Status { get; set; } = "NotRequested";
    public string Decision { get; set; } = "None";
    public string DecisionSource { get; set; } = "LegacyUnknown";
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public string PromptVersion { get; set; } = "";
    public int SchemaVersion { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureStage { get; set; }
    public string? ValidationCode { get; set; }
    public string? ValidationPath { get; set; }
    public int? ProviderHttpStatus { get; set; }
    public long? JsonLineNumber { get; set; }
    public long? JsonBytePositionInLine { get; set; }
    public int Generation { get; set; }
    public int RetryCount { get; set; }
}
public class CoachDiagnosticContextDto
{
    public int Planned { get; set; }
    public int Completed { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public int CandidateCount { get; set; }
    public int RevisionMismatchCards { get; set; }
    public int ContentOmittedCards { get; set; }
    public List<CoachDiagnosticCardDto> Cards { get; set; } = new();
}
public class CoachDiagnosticCardDto
{
    public int CardId { get; set; }
    public int SessionRevision { get; set; }
    public int? CurrentRevision { get; set; }
    public string SelectionReason { get; set; } = "ContentUnavailable";
    public int CurrentRevisionEvidenceCount { get; set; }
    public int HistoricalRevisionEvidenceCount { get; set; }
    public string? SessionOutcome { get; set; }
    public int? SessionResponseTimeMs { get; set; }
    public string? Status { get; set; }
    public decimal? MasteryScore { get; set; }
    public string? Source { get; set; }
    public int? ModelVersion { get; set; }
    public List<CoachDiagnosticAttemptDto> RecentAttempts { get; set; } = new();
}
public class CoachDiagnosticAttemptDto
{
    public string Outcome { get; set; } = "";
    public DateTimeOffset ReviewedAt { get; set; }
    public int? ResponseTimeMs { get; set; }
    public int LearningRevision { get; set; }
}

// Only bounded numbers and controlled values, never provider prose or raw content.
public class CoachProviderResponseDiagnosticsDto
{
    public int? HttpStatus { get; init; }
    public string? FinishReason { get; init; }
    public int? PromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
    public int? TotalTokens { get; init; }
    public int? ReasoningTokens { get; init; }
}
public class CoachOutputFormatDiagnosticsDto
{
    public bool StartsWithJsonObject { get; init; }
    public bool StartsWithJsonArray { get; init; }
    public bool EndsWithJsonObject { get; init; }
    public bool EndsWithJsonArray { get; init; }
    public bool StartsWithCodeFence { get; init; }
    public bool EndsWithCodeFence { get; init; }
}

#nullable enable
using System;
namespace LearnWord.DAL.Models;

// AI output lives separately from immutable ordinary learning evidence and rebuildable learner state.
public class CoachAnalysis
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public int CollectionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public string Decision { get; set; } = "None";
    public string Language { get; set; } = "en";
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public string PromptVersion { get; set; } = "coach-v1";
    public int SchemaVersion { get; set; } = 1;
    public int Generation { get; set; }
    public int RetryCount { get; set; }
    public string? FailureCode { get; set; }
    public string Summary { get; set; } = "";
    public string ObservationsJson { get; set; } = "[]";
}
public class CoachIntervention
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public int CardId { get; set; }
    public int LearningRevision { get; set; }
    public string Type { get; set; } = "MicroLesson";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "Suggested";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string PayloadJson { get; set; } = "{}";
}
public class InterventionAttempt
{
    public Guid Id { get; set; }
    public Guid InterventionId { get; set; }
    public Guid ExerciseId { get; set; }
    public int CardId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public bool Correct { get; set; }
    public int? ResponseTimeMs { get; set; }
    // An opaque option id, never free-form learner or personal input.
    public string OptionId { get; set; } = "";
}

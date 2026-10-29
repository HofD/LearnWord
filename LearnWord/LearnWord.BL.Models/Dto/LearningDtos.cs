using LearnWord.DAL.Models;
namespace LearnWord.BL.Models.Dto;

public class TodayPlanDto
{
    public int TotalCount { get; set; }
    public int EstimatedMinutes { get; set; }
    public TodayGroupsDto Groups { get; set; } = new();
    public List<CardDto> Cards { get; set; } = new();
}
public class TodayGroupsDto
{
    public int Due { get; set; }
    public int Weak { get; set; }
    public int New { get; set; }
}
public class LearnerSummaryDto
{
    public int New { get; set; }
    public int Learning { get; set; }
    public int Weak { get; set; }
    public int Strong { get; set; }
    public int TotalCardsStudied { get; set; }
    public int AttemptsCount { get; set; }
    public int RecentAttemptsCount { get; set; }
}
public class LearningSessionDto
{
    public Guid Id { get; set; }
    public int CollectionId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int PlannedCardsCount { get; set; }
    public int CompletedCardsCount { get; set; }
    public int SuccessfulReviews { get; set; }
    public int FailedReviews { get; set; }
    public List<CardDto> Cards { get; set; } = new();
}
public class LearnerCardStateDto
{
    public int CardId { get; set; }
    public decimal MasteryScore { get; set; }
    public decimal DifficultyScore { get; set; }
    public int AttemptsCount { get; set; }
    public int SuccessfulAttempts { get; set; }
    public int FailedAttempts { get; set; }
    public int ConsecutiveSuccesses { get; set; }
    public int ConsecutiveFailures { get; set; }
    public string? LastOutcome { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public string Status { get; set; } = "New";
    public string Source { get; set; } = "NoHistory";
    public int ModelVersion { get; set; }
    public int LearningRevision { get; set; }
}
public class LearningAttemptDto
{
    public Guid Id { get; set; }
    public int CardId { get; set; }
    public Guid? SessionId { get; set; }
    public string Outcome { get; set; } = "";
    public DateTimeOffset ReviewedAt { get; set; }
    public int PreviousIntervalDays { get; set; }
    public int NewIntervalDays { get; set; }
    public decimal PreviousEaseFactor { get; set; }
    public decimal NewEaseFactor { get; set; }
    public DateTimeOffset PreviousDueDate { get; set; }
    public DateTimeOffset NewDueDate { get; set; }
    public int? ResponseTimeMs { get; set; }
    public int LearningRevision { get; set; }
}

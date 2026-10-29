using System;
using LearnWord.BL.Models.Dto;
namespace LearnWord.DAL.Models;
public enum LearnerStatus { New, Learning, Weak, Strong }
public class LearnerCardState
{
    public int CardId { get; set; }
    public int LearningRevision { get; set; }
    public int ModelVersion { get; set; } = 1;
    public string Source { get; set; } = "Observed";
    public decimal MasteryScore { get; set; }
    public decimal DifficultyScore { get; set; }
    public int AttemptsCount { get; set; }
    public int SuccessfulAttempts { get; set; }
    public int FailedAttempts { get; set; }
    public int ConsecutiveSuccesses { get; set; }
    public int ConsecutiveFailures { get; set; }
    public decimal OutcomePoints { get; set; }
    public ReviewOutcome? LastOutcome { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public LearnerStatus Status { get; set; }
}

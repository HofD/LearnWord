using System;
using LearnWord.BL.Models.Dto;
namespace LearnWord.DAL.Models;

// Content-free immutable evidence. Current-state resets never modify old attempts.
public class LearningAttempt
{
    public Guid Id { get; set; }
    public int CardId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? RequestId { get; set; }
    public int LearningRevision { get; set; }
    public ReviewOutcome Outcome { get; set; }
    public DateTimeOffset ReviewedAt { get; set; }
    public int PreviousIntervalDays { get; set; }
    public int NewIntervalDays { get; set; }
    public decimal PreviousEaseFactor { get; set; }
    public decimal NewEaseFactor { get; set; }
    public DateTimeOffset PreviousDueDate { get; set; }
    public DateTimeOffset NewDueDate { get; set; }
    public int? ResponseTimeMs { get; set; }
}

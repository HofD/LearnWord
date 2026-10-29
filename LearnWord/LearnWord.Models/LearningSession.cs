using System;
using System.Collections.Generic;
namespace LearnWord.DAL.Models;
public class LearningSession
{
    public Guid Id { get; set; }
    public int CollectionId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int PlannedCardsCount { get; set; }
    public int CompletedCardsCount { get; set; }
    public int SuccessfulReviews { get; set; }
    public int FailedReviews { get; set; }
    public List<LearningSessionCard> Cards { get; set; } = new();
}
public class LearningSessionCard
{
    public Guid SessionId { get; set; }
    public int CardId { get; set; }
    public int Position { get; set; }
    public int LearningRevision { get; set; }
}

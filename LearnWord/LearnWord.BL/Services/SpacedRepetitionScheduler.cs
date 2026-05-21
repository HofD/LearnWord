using LearnWord.BL.Abstractions;
using LearnWord.BL.Models.Dto;
using LearnWord.DAL.Models;

namespace LearnWord.BL.Services
{
    public class SpacedRepetitionScheduler : ISpacedRepetitionScheduler
    {
        private const decimal DefaultEaseFactor = 2.5m;
        private const decimal MinEaseFactor = 1.3m;

        public void ApplyReview(Card card, ReviewOutcome outcome, DateTimeOffset reviewedAt)
        {
            var currentEaseFactor = card.EaseFactor <= 0 ? DefaultEaseFactor : card.EaseFactor;
            var currentInterval = card.IntervalDays;

            var newEaseFactor = outcome switch
            {
                ReviewOutcome.Again => currentEaseFactor - 0.20m,
                ReviewOutcome.Hard => currentEaseFactor - 0.15m,
                ReviewOutcome.Easy => currentEaseFactor + 0.15m,
                _ => currentEaseFactor
            };

            newEaseFactor = Math.Max(MinEaseFactor, newEaseFactor);

            var newInterval = outcome switch
            {
                ReviewOutcome.Again => 1,
                ReviewOutcome.Hard => Math.Max(1, (int)Math.Ceiling(currentInterval * 1.2m)),
                ReviewOutcome.Good => card.ReviewCount == 0
                    ? 1
                    : Math.Max(1, (int)Math.Ceiling(currentInterval * currentEaseFactor)),
                ReviewOutcome.Easy => card.ReviewCount == 0
                    ? 4
                    : Math.Max(1, (int)Math.Ceiling(currentInterval * currentEaseFactor * 1.3m)),
                _ => 1
            };

            card.IntervalDays = newInterval;
            card.EaseFactor = newEaseFactor;
            card.ReviewCount += 1;
            card.LastReviewedAt = reviewedAt;
            card.DueDate = reviewedAt.AddDays(newInterval);
            card.ShowedAt = reviewedAt;
            card.ModifiedAt = reviewedAt;

            if (outcome == ReviewOutcome.Again)
            {
                card.Learnt = false;
                card.LearntAt = null;
                return;
            }

            card.Learnt = true;
            card.LearntAt = reviewedAt;
        }

        public void Reset(Card card, DateTimeOffset resetAt)
        {
            card.Learnt = false;
            card.LearntAt = null;
            card.ShowedAt = null;
            card.DueDate = resetAt;
            card.IntervalDays = 0;
            card.EaseFactor = DefaultEaseFactor;
            card.ReviewCount = 0;
            card.LastReviewedAt = null;
            card.ModifiedAt = resetAt;
        }
    }
}

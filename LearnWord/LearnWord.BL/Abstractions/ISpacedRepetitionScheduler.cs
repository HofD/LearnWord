using LearnWord.BL.Models.Dto;
using LearnWord.DAL.Models;

namespace LearnWord.BL.Abstractions
{
    public interface ISpacedRepetitionScheduler
    {
        void ApplyReview(Card card, ReviewOutcome outcome, DateTimeOffset reviewedAt);
        void Reset(Card card, DateTimeOffset resetAt);
    }
}

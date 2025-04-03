using LearnWord.Review.Models;
using LearnWord.BL.Models.Dto;

namespace LearnWord.Review.Abstractions;

public interface IReviewService
{
    Task<IEnumerable<CardDto>> GetCardsForReviewAsync(string userId, int collectionId);
    Task<ReviewCardDto> MarkCardAsLearnedAsync(string userId, int cardId);
    Task<ReviewCardDto> MarkCardAsForgottenAsync(string userId, int cardId);
} 
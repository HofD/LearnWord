using LearnWord.Review.Models;
using LearnWord.BL.Models.Dto;

namespace LearnWord.Review.Abstractions;

public interface ILearnWordHttpService
{
    Task<IEnumerable<CardDto>> GetReviewCardsAsync(int collectionId);
    Task<ReviewCardDto> GetCardAsync(int cardId);
    Task<ReviewCardDto> ShowCardAsync(int cardId);
    Task<ReviewCardDto> LearnCardAsync(int cardId);
    Task<ReviewCardDto> ForgetCardAsync(int cardId);
} 
using LearnWord.BL.Models.Dto;

namespace LearnWord.Review.Abstractions;

public interface IIdentityHttpService
{
    Task<bool> IsUserCollectionOwnerAsync(string userId, int collectionId);
    Task<bool> IsUserCardOwnerAsync(string userId, int cardId);
    Task<CardDto> ForgetCardAsync(int cardId);
    Task<CardDto> LearnCardAsync(int cardId);
} 
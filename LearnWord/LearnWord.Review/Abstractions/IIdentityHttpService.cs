namespace LearnWord.Review.Abstractions;

public interface IIdentityHttpService
{
    Task<bool> IsUserCollectionOwnerAsync(string userId, int collectionId);
    Task<bool> IsUserCardOwnerAsync(string userId, int cardId);
} 
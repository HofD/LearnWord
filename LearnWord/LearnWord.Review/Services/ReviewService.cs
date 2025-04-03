using LearnWord.BL.Models.Dto;
using LearnWord.Review.Abstractions;
using LearnWord.Review.Models;

namespace LearnWord.Review.Services;

public class ReviewService : IReviewService
{
    private readonly ILearnWordHttpService _learnWordHttpService;
    private readonly IIdentityHttpService _identityHttpService;

    public ReviewService(ILearnWordHttpService learnWordHttpService, IIdentityHttpService identityHttpService)
    {
        _learnWordHttpService = learnWordHttpService;
        _identityHttpService = identityHttpService;
    }

    public async Task<IEnumerable<CardDto>> GetCardsForReviewAsync(string userId, int collectionId)
    {
        // Verify user owns the collection
        var isOwner = await _identityHttpService.IsUserCollectionOwnerAsync(userId, collectionId);
        if (!isOwner)
        {
            throw new UnauthorizedAccessException("User does not own this collection");
        }

        // Get review cards from the collection
        return await _learnWordHttpService.GetReviewCardsAsync(collectionId);
    }

    public async Task<ReviewCardDto> MarkCardAsLearnedAsync(string userId, int cardId)
    {
        // Get card to find its collection
        var card = await _learnWordHttpService.GetCardAsync(cardId);
        
        // Verify user owns the collection
        var isCollectionOwner = await _identityHttpService.IsUserCollectionOwnerAsync(userId, card.CollectionId);
        if (!isCollectionOwner)
        {
            throw new UnauthorizedAccessException("User does not own this collection");
        }

        return await _learnWordHttpService.LearnCardAsync(cardId);
    }

    public async Task<ReviewCardDto> MarkCardAsForgottenAsync(string userId, int cardId)
    {
        // Get card to find its collection
        var card = await _learnWordHttpService.GetCardAsync(cardId);
        
        // Verify user owns the collection
        var isCollectionOwner = await _identityHttpService.IsUserCollectionOwnerAsync(userId, card.CollectionId);
        if (!isCollectionOwner)
        {
            throw new UnauthorizedAccessException("User does not own this collection");
        }

        return await _learnWordHttpService.ForgetCardAsync(cardId);
    }

    public async Task<ReviewCardDto> MarkCardAsReviewedAsync(string userId, int cardId)
    {
        // Get card to find its collection
        var card = await _learnWordHttpService.GetCardAsync(cardId);
        
        // Verify user owns the collection
        var isCollectionOwner = await _identityHttpService.IsUserCollectionOwnerAsync(userId, card.CollectionId);
        if (!isCollectionOwner)
        {
            throw new UnauthorizedAccessException("User does not own this collection");
        }

        return await _learnWordHttpService.LearnCardAsync(cardId);
    }
} 
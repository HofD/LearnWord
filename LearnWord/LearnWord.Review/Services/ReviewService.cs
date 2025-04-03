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
        // Use Identity service to mark card as learned
        var updatedCard = await _identityHttpService.LearnCardAsync(cardId);
        
        return new ReviewCardDto
        {
            Id = updatedCard.Id,
            CollectionId = updatedCard.CollectionId,
            Words = updatedCard.Words,
            Learnt = updatedCard.Learnt
        };
    }

    public async Task<ReviewCardDto> MarkCardAsForgottenAsync(string userId, int cardId)
    {
        // Use Identity service to mark card as forgotten
        var updatedCard = await _identityHttpService.ForgetCardAsync(cardId);
        
        return new ReviewCardDto
        {
            Id = updatedCard.Id,
            CollectionId = updatedCard.CollectionId,
            Words = updatedCard.Words,
            Learnt = updatedCard.Learnt
        };
    }
} 
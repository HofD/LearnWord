using LearnWord.BL.Models.Dto;
using LearnWord.Review.Abstractions;
using LearnWord.Review.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnWord.Review.Controllers;

[ApiController]
[Route("[controller]")]

public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("collections/{collectionId}/cards")]
    public async Task<ActionResult<IEnumerable<CardDto>>> GetCardsForReview(int collectionId)
    {
        var userId = HttpContext.Items["UserId"]?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in context");
        }
        var cards = await _reviewService.GetCardsForReviewAsync(userId, collectionId);
        return Ok(cards);
    }

    [HttpPost("cards/{cardId}/learn")]
    public async Task<IActionResult> MarkCardAsLearned(int cardId)
    {
        var userId = HttpContext.Items["UserId"]?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in context");
        }
        await _reviewService.MarkCardAsLearnedAsync(userId, cardId);
        return Ok();
    }

    [HttpPost("cards/{cardId}/forget")]
    public async Task<IActionResult> MarkCardAsForgotten(int cardId)
    {
        var userId = HttpContext.Items["UserId"]?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in context");
        }
        await _reviewService.MarkCardAsForgottenAsync(userId, cardId);
        return Ok();
    }

    [HttpPost("cards/{cardId}/review")]
    public async Task<ActionResult<ReviewCardDto>> MarkCardAsReviewed(int cardId)
    {
        var userId = HttpContext.Items["UserId"]?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in context");
        }
        var updatedCard = await _reviewService.MarkCardAsReviewedAsync(userId, cardId);
        return Ok(updatedCard);
    }
} 
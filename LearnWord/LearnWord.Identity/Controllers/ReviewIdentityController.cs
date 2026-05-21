using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;
using Microsoft.AspNetCore.Mvc;

namespace LearnWord.Identity.Controllers
{
    [ApiController]
    [Route("review/cards")]
    public class ReviewIdentityController : ControllerBase
    {
        private readonly ICardIdentityService cardIdentityService;

        public ReviewIdentityController(ICardIdentityService cardIdentityService)
        {
            this.cardIdentityService = cardIdentityService;
        }

        [HttpPost("{cardId}/review")]
        public async Task<ActionResult<CardDto>> Review(int cardId, ReviewCardRequest request)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            if (userId == null)
            {
                return Unauthorized();
            }

            return Ok(await cardIdentityService.Review(cardId, request, userId));
        }
    }
}

using LearnWord.BL.Abstractions;
using LearnWord.BL.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace LearnWord.WebApi.Controllers
{
    [ApiController]
    [Route("review/cards")]
    public class ReviewController : ControllerBase
    {
        private readonly ICardService cardService;

        public ReviewController(ICardService cardService)
        {
            this.cardService = cardService;
        }

        [HttpPost("{cardId}/review")]
        public async Task<ActionResult<CardDto>> Review(int cardId, ReviewCardRequest request)
        {
            return Ok(await cardService.Review(cardId, request));
        }
    }
}

using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;
using Microsoft.AspNetCore.Mvc;

namespace LearnWord.Identity.Controllers
{
    [ApiController]
    [Route("cards")]
    public class CardsIdentityController : ControllerBase
    {
        private readonly ICardIdentityService cardIdentityService;

        public CardsIdentityController(ICardIdentityService cardIdentityService)
        {
            this.cardIdentityService = cardIdentityService;
        }

        [HttpPost]
        public async Task<ActionResult<CardDto>> Add(CardCreateDto card)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            if (userId == null)
            {
                return Unauthorized();
            }

            return CreatedAtAction(nameof(Add), await cardIdentityService.Add(card, userId));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();

            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await cardIdentityService.Remove(id, userId);

            return result ? Ok() : StatusCode(StatusCodes.Status502BadGateway);
        }
    }
}

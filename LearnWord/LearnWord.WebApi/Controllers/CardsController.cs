using LearnWord.BL.Abstractions;
using LearnWord.BL.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace LearnWord.WebApi.Controllers
{
    [ApiController]
    [Route("cards")]
    public class CardsController : ControllerBase
    {
        private readonly ILogger<CardsController> logger;
        private readonly ICardService cardService;

        public CardsController(ILogger<CardsController> logger, ICardService cardService) 
        {
            this.logger = logger;
            this.cardService = cardService;
        }

        [HttpPost]
        public async Task<ActionResult<CardDto>> Add(CardCreateDto card)
        {
            return Ok(await cardService.Add(card));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            await cardService.Remove(id);
            return Ok();
        }
    }
}

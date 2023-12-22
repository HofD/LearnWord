using LearningWords.BL.Abstractions;
using LearningWords.BL.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace LearningWords.WebApi.Controllers
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
        public async Task<CardDto> Add(CardCreateDto card)
        {
            return await cardService.Add(card);
        }

        [HttpDelete("{id}")]
        public async Task Remove(int id)
        {
            await cardService.Remove(id);
        }

        [HttpPost("{id}/learn")]
        public async Task<CardDto> Learn(int id)
        {
            return await cardService.Learn(id);
        }

        [HttpPost("{id}/forget")]
        public async Task<CardDto> Forget(int id)
        {
            return await cardService.Forget(id);
        }
    }
}

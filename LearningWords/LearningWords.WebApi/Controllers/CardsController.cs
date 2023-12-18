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
    }
}

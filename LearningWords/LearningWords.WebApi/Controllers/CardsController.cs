using LearningWords.BL.Models.Dto;
using LearningWords.BL.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearningWords.WebApi.Controllers
{
    [ApiController]
    [Route("cards")]
    public class CardsController : ControllerBase
    {
        private readonly ILogger<CardsController> logger;
        private readonly CardService cardService;

        public CardsController(ILogger<CardsController> logger, CardService cardService) 
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

using LearningWords.BL.Abstractions;
using LearningWords.BL.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace LearningWords.WebApi.Controllers
{
    [ApiController]
    public class WordsController : ControllerBase
    {
        private readonly ILogger<WordsController> logger;
        private readonly IWordService wordService;

        public WordsController(ILogger<WordsController> logger, IWordService wordService)
        {
            this.logger = logger;
            this.wordService = wordService;
        }

        [HttpPost("cards/{cardId}/words")]
        public async Task<WordDto> Add(WordCreateDto word, int cardId)
        {
            return await wordService.Add(word, cardId);
        }

        [HttpDelete("words/{id}")]
        public async Task Remove(int id)
        {
            await wordService.Remove(id);
        }
    }
}

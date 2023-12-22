using LearningWords.BL.Abstractions;
using LearningWords.BL.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace LearningWords.WebApi.Controllers
{
    [ApiController]
    public class WordsController : ControllerBase
    {
        private readonly ILogger<WordsController> logger;
        private readonly IWordEditService wordEditService;

        public WordsController(ILogger<WordsController> logger, IWordEditService wordEditService)
        {
            this.logger = logger;
            this.wordEditService = wordEditService;
        }

        [HttpPost("cards/{cardId}/words")]
        public async Task<WordDto> Add(WordCreateDto word, int cardId)
        {
            return await wordEditService.Add(word, cardId);
        }

        [HttpDelete("words/{id}")]
        public async Task Remove(int id)
        {
            await wordEditService.Remove(id);
        }

        [HttpPut("cards/{cardId}/words/{id}")]
        public async Task<WordDto> Update(int cardId, int id, WordUpdateDto word)
        {
            return await wordEditService.Update(cardId, id, word);
        }
    }
}

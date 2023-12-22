using LearningWords.BL.Abstractions;
using LearningWords.BL.Models.Dto;

namespace LearningWords.BL.Services
{
    public class WordEditService : IWordEditService
    {
        private readonly ICardService cardService;
        private readonly IWordService wordService;

        public WordEditService(ICardService cardService, IWordService wordService)
        {
            this.cardService = cardService;
            this.wordService = wordService;
        }

        public async Task<WordDto> Add(WordCreateDto word, int cardId)
        {
            var addedWord = await wordService.Add(word, cardId);

            await cardService.ResetCard(cardId);

            return addedWord;
        }

        public async Task Remove(int id)
        {
            await wordService.Remove(id);
        }

        public async Task<WordDto> Update(int cardId, int id, WordUpdateDto word)
        {
            var updatedWord = await wordService.Update(cardId, id, word);

            await cardService.ResetCard(cardId);

            return updatedWord;
        }
    }
}

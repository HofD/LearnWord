using LearnWord.BL.Abstractions;
using LearnWord.BL.Models.Dto;

namespace LearnWord.BL.Services
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

            await cardService.Reset(cardId);

            return addedWord;
        }

        public async Task Remove(int cardId, int id)
        {
            await wordService.Remove(cardId, id);

            if (await wordService.HasAnyActiveWords(cardId))
            {
                await cardService.Reset(cardId);
            }
            else
            {
                await cardService.Remove(cardId);
            }
        }

        public async Task<WordDto> Update(int cardId, int id, WordUpdateDto word)
        {
            var updatedWord = await wordService.Update(cardId, id, word);

            await cardService.Reset(cardId);

            return updatedWord;
        }
    }
}

using LearnWord.BL.Abstractions;
using LearnWord.BL.Models.Dto;
using LearnWord.DAL.Repositories;

namespace LearnWord.BL.Services
{
    public class WordEditService : IWordEditService
    {
        private readonly ICardService cardService;
        private readonly IWordService wordService;
        private readonly LearningRepository? learning;

        public WordEditService(ICardService cardService, IWordService wordService, LearningRepository? learning = null)
        {
            this.cardService = cardService;
            this.wordService = wordService;
            this.learning = learning;
        }

        public async Task<WordDto> Add(WordCreateDto word, int cardId)
        {
            await using var tx = learning == null ? null : await learning.BeginMutation(cardId);
            var addedWord = await wordService.Add(word, cardId);

            await cardService.Reset(cardId);

            if (tx != null) await tx.CommitAsync();
            return addedWord;
        }

        public async Task Remove(int cardId, int id)
        {
            await using var tx = learning == null ? null : await learning.BeginMutation(cardId);
            await wordService.Remove(cardId, id);

            if (await wordService.HasAnyActiveWords(cardId))
            {
                await cardService.Reset(cardId);
            }
            else
            {
                await cardService.Remove(cardId);
            }
            if (tx != null) await tx.CommitAsync();
        }

        public async Task<WordDto> Update(int cardId, int id, WordUpdateDto word)
        {
            await using var tx = learning == null ? null : await learning.BeginMutation(cardId);
            var updatedWord = await wordService.Update(cardId, id, word);

            await cardService.Reset(cardId);

            if (tx != null) await tx.CommitAsync();
            return updatedWord;
        }
    }
}

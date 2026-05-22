using LearnWord.BL.Abstractions;
using LearnWord.BL.Mapping;
using LearnWord.BL.Models.Dto;
using LearnWord.DAL.Models;
using LearnWord.DAL.Repositories;

namespace LearnWord.BL.Services
{
    public class WordService : IWordService
    {
        private readonly WordRepository wordRepository;
        private readonly ObjectMapper mapper;

        public WordService(WordRepository wordRepository, ObjectMapper mapper)
        {
            this.wordRepository = wordRepository;
            this.mapper = mapper;
        }
        public async Task<WordDto> Add(WordCreateDto word, int cardId)
        {
            Word wordToSave = mapper.Map<Word>(word);
            wordToSave.CardId = cardId;

            return mapper.Map<WordDto>(await wordRepository.Add(wordToSave));
        }

        public async Task Remove(int cardId, int id)
        {
            await wordRepository.Remove(cardId, id);
        }

        public async Task<bool> HasAnyActiveWords(int cardId)
        {
            return await wordRepository.HasAnyActiveWords(cardId);
        }

        public async Task<WordDto> Update(int cardId, int id, WordUpdateDto word)
        {
            Word wordToSave = mapper.Map<Word>(word);
            wordToSave.CardId = cardId;
            wordToSave.Id = id;

            return mapper.Map<WordDto>(await wordRepository.Update(wordToSave));
        }
    }
}

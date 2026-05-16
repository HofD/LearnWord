using LearnWord.BL.Models.Dto;

namespace LearnWord.BL.Abstractions
{
    public interface IWordService
    {
        Task<WordDto> Add(WordCreateDto word, int cardId);
        Task Remove(int cardId, int id);
        Task<bool> HasAnyActiveWords(int cardId);
        Task<WordDto> Update(int cardId, int id, WordUpdateDto word);
    }
}

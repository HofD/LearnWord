using LearnWord.BL.Models.Dto;

namespace LearnWord.BL.Abstractions
{
    public interface IWordEditService
    {
        Task<WordDto> Add(WordCreateDto word, int cardId);
        Task Remove(int id);
        Task<WordDto> Update(int cardId, int id, WordUpdateDto word);
    }
}
using LearningWords.BL.Models.Dto;

namespace LearningWords.BL.Abstractions
{
    public interface IWordService
    {
        Task<WordDto> Add(WordCreateDto word, int cardId);
        Task Remove(int id);
        Task<WordDto> Update(WordUpdateDto word, int cardId);
    }
}

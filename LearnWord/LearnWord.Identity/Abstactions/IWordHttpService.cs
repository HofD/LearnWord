using LearnWord.BL.Models.Dto;

namespace LearnWord.Identity.Abstactions
{
    public interface IWordHttpService
    {
        Task<WordDto> Add(int cardId, WordCreateDto wordCreateDto);
        Task<bool> Remove(int id, int cardId);
        Task<WordDto> Update(int cardId, int id, WordUpdateDto wordUpdateDto);
    }
}

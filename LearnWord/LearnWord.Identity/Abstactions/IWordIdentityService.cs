using LearnWord.BL.Models.Dto;

namespace LearnWord.Identity.Abstactions
{
    public interface IWordIdentityService
    {
        Task<WordDto?> Add(int cardId, WordCreateDto wordCreateDto, string userId);
        Task<bool> Remove(int id, int cardId, string userId);
        Task<WordDto> Update(int cardId, int id, WordUpdateDto wordUpdateDto, string userId);
    }
}

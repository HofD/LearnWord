using LearnWord.BL.Models.Dto;

namespace LearnWord.Identity.Abstactions
{
    public interface ICardIdentityService
    {
        Task<CardDto> Add(CardCreateDto cardCreateDto, string userId);
        Task<bool?> Remove(int id, string userId);
        Task<CardDto> Learn(int id, string userId);
        Task<CardDto> Forget(int id, string userId);
    }
}

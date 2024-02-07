using LearnWord.BL.Models.Dto;

namespace LearnWord.Identity.Abstactions
{
    public interface ICardHttpService
    {
        Task<CardDto> Add(CardCreateDto cardCreateDto);
        Task<bool?> Remove(int id);
        Task<CardDto> Learn(int id);
        Task<CardDto> Forget(int id);
    }
}

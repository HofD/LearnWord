using LearningWords.BL.Models.Dto;

namespace LearningWords.BL.Abstractions
{
    public interface ICardService
    {
        Task<CardDto> Add(CardCreateDto createDto);
        Task Remove(int id);
        Task<CardDto> Reset(int id);
        Task<CardDto> Learn(int id);
        Task<CardDto> Forget(int id);
    }
}

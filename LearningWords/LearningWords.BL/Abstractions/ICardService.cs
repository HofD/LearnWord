using LearningWords.BL.Models.Dto;

namespace LearningWords.BL.Abstractions
{
    public interface ICardService
    {
        Task<CardDto> Add(CardCreateDto createDto);
        Task Remove(int id);
    }
}

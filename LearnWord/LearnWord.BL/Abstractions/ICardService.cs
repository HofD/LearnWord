using LearnWord.BL.Models.Dto;

namespace LearnWord.BL.Abstractions
{
    public interface ICardService
    {
        Task<CardDto> Add(CardCreateDto createDto);
        Task Remove(int id);
        Task<CardDto> Reset(int id);
        Task<CardDto> Review(int id, ReviewCardRequest request);
    }
}

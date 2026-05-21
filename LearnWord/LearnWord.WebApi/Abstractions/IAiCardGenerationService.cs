using LearnWord.BL.Models.Dto;

namespace LearnWord.WebApi.Abstractions
{
    public interface IAiCardGenerationService
    {
        Task<AiCardGenerationResponse> GenerateCards(AiCardGenerationRequest request, CancellationToken cancellationToken);
    }
}

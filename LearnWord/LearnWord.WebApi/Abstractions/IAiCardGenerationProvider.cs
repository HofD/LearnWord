using LearnWord.BL.Models.Dto;

namespace LearnWord.WebApi.Abstractions
{
    public interface IAiCardGenerationProvider
    {
        Task<AiCardGenerationResponse> GenerateCards(AiCardGenerationRequest request, CancellationToken cancellationToken);
    }
}

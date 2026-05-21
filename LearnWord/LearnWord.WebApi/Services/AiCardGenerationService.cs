using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Options;
using Microsoft.Extensions.Options;

namespace LearnWord.WebApi.Services
{
    public class AiCardGenerationService : IAiCardGenerationService
    {
        private readonly IAiCardGenerationProvider provider;
        private readonly AiCardGenerationOptions options;

        public AiCardGenerationService(
            IAiCardGenerationProvider provider,
            IOptions<AiCardGenerationOptions> options)
        {
            this.provider = provider;
            this.options = options.Value;
        }

        public async Task<AiCardGenerationResponse> GenerateCards(AiCardGenerationRequest request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var response = await provider.GenerateCards(request, cancellationToken);
            ValidateResponse(response, request.MaxCards);

            return response;
        }

        private void ValidateRequest(AiCardGenerationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SourceText))
            {
                throw new BadRequestException("Source text is required.", "ai_source_text_required");
            }

            if (request.SourceText.Length > options.MaxSourceTextLength)
            {
                throw new BadRequestException(
                    $"Source text must not exceed {options.MaxSourceTextLength} characters.",
                    "ai_source_text_too_long");
            }

            if (request.MaxCards < 1 || request.MaxCards > options.MaxCards)
            {
                throw new BadRequestException(
                    $"Max cards must be between 1 and {options.MaxCards}.",
                    "ai_max_cards_out_of_range");
            }
        }

        private static void ValidateResponse(AiCardGenerationResponse response, int requestedMaxCards)
        {
            if (response.Cards.Count > requestedMaxCards)
            {
                response.Cards = response.Cards.Take(requestedMaxCards).ToList();
            }

            foreach (var card in response.Cards)
            {
                if (string.IsNullOrWhiteSpace(card.Value) || string.IsNullOrWhiteSpace(card.Translation))
                {
                    throw new UpstreamServiceException(
                        "AI provider returned an invalid card suggestion.",
                        "ai_invalid_provider_response");
                }

                card.Value = card.Value.Trim();
                card.Transcription = card.Transcription.Trim();
                card.Translation = card.Translation.Trim();
                card.Example = card.Example.Trim();
                card.Explanation = card.Explanation.Trim();
                card.Difficulty = card.Difficulty.Trim();
            }
        }
    }
}

using LearnWord.BL.Models.Dto;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Options;
using Microsoft.Extensions.Options;

namespace LearnWord.WebApi.Services
{
    public class ConfiguredAiCardGenerationProvider : IAiCardGenerationProvider
    {
        private readonly FakeAiCardGenerationProvider fakeProvider;
        private readonly OpenRouterAiCardGenerationProvider openRouterProvider;
        private readonly AiCardGenerationOptions options;
        private readonly ILogger<ConfiguredAiCardGenerationProvider> logger;

        public ConfiguredAiCardGenerationProvider(
            FakeAiCardGenerationProvider fakeProvider,
            OpenRouterAiCardGenerationProvider openRouterProvider,
            IOptions<AiCardGenerationOptions> options,
            ILogger<ConfiguredAiCardGenerationProvider> logger)
        {
            this.fakeProvider = fakeProvider;
            this.openRouterProvider = openRouterProvider;
            this.options = options.Value;
            this.logger = logger;
        }

        public Task<AiCardGenerationResponse> GenerateCards(AiCardGenerationRequest request, CancellationToken cancellationToken)
        {
            if (string.Equals(options.Provider, "OpenRouter", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(options.OpenRouter.ApiKey))
            {
                return openRouterProvider.GenerateCards(request, cancellationToken);
            }

            if (string.Equals(options.Provider, "OpenRouter", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("OpenRouter provider selected without an API key; using fake AI card provider.");
            }

            return fakeProvider.GenerateCards(request, cancellationToken);
        }
    }
}

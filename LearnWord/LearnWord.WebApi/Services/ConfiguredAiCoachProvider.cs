using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Options;
using Microsoft.Extensions.Options;
namespace LearnWord.WebApi.Services;
public class ConfiguredAiCoachProvider : IAiCoachProvider
{
    private readonly FakeAiCoachProvider fake;
    private readonly OpenRouterAiCoachProvider openRouter;
    private readonly AiCoachOptions options;
    public ConfiguredAiCoachProvider(FakeAiCoachProvider fake, OpenRouterAiCoachProvider openRouter, IOptions<AiCoachOptions> options)
    { this.fake = fake; this.openRouter = openRouter; this.options = options.Value; }
    public string Provider => options.Provider ?? "";
    public string Model => string.Equals(options.Provider, "Fake", StringComparison.OrdinalIgnoreCase) ? fake.Model : openRouter.Model;
    public Task<string> Analyze(CoachSessionContext context, CancellationToken cancellationToken)
        => options.Provider?.ToLowerInvariant() switch { "fake" => fake.Analyze(context, cancellationToken), "openrouter" => openRouter.Analyze(context, cancellationToken),
            _ => throw CoachFailure.WithDetails(new UpstreamServiceException("AI Coach provider is disabled or not configured.", "ai_provider_configuration_error"), new("Configuration")) };
    public Task<CoachProviderResponse> AnalyzeWithDiagnostics(CoachSessionContext context, CancellationToken cancellationToken)
        => options.Provider?.ToLowerInvariant() switch { "fake" => ((IAiCoachProvider)fake).AnalyzeWithDiagnostics(context, cancellationToken), "openrouter" => openRouter.AnalyzeWithDiagnostics(context, cancellationToken),
            _ => throw CoachFailure.WithDetails(new UpstreamServiceException("AI Coach provider is disabled or not configured.", "ai_provider_configuration_error"), new("Configuration")) };
}

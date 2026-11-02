using LearnWord.BL.Models.Errors;
namespace LearnWord.WebApi.Services;

// Internal metadata: callers supply only fixed rule names and paths assembled from known schema keys.
// The private object key prevents arbitrary exception.Data strings becoming exported diagnostics.
public sealed record CoachFailureDetails(string FailureStage, string? ValidationCode = null, string? ValidationPath = null, int? ProviderHttpStatus = null);
public static class CoachFailure
{
    private static readonly object Key = new();
    public static UpstreamServiceException WithDetails(UpstreamServiceException exception, CoachFailureDetails details)
    { exception.Data[Key] = details; return exception; }
    public static CoachFailureDetails? Details(Exception exception) => exception.Data[Key] as CoachFailureDetails;
    public static UpstreamServiceException Invalid(string code, string path) => WithDetails(
        new UpstreamServiceException("AI Coach returned invalid structured output.", "ai_provider_invalid_response"), new("OutputValidation", code, path));
}

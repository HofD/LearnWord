using System.Text;
using System.Text.Json.Nodes;
using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Services;

namespace LearnWord.CoachEval;

// Only the standalone evaluation uses this adapter; production provider/configuration stay unchanged.
public static class EvaluationHttp
{
    public static HttpClient CreateClient(string? reasoningEffort, HttpMessageHandler? inner = null, int maxOutputTokens = Evaluation.MaxOutputTokens)
    {
        EvaluationOptions.ValidateReasoningEffort(reasoningEffort);
        EvaluationOptions.ValidateMaxOutputTokens(maxOutputTokens);
        inner ??= new HttpClientHandler();
        return new HttpClient(reasoningEffort == null && maxOutputTokens == Evaluation.MaxOutputTokens ? inner : new RequestOptionsHandler(reasoningEffort, maxOutputTokens, inner))
        { Timeout = Timeout.InfiniteTimeSpan };
    }

    private sealed class RequestOptionsHandler(string? effort, int maxOutputTokens, HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var original = request.Content ?? throw new InvalidOperationException("Evaluation request content is required.");
            var body = JsonNode.Parse(await original.ReadAsStringAsync(cancellationToken))!.AsObject();
            if (effort != null) body["reasoning"] = new JsonObject { ["effort"] = effort };
            if (maxOutputTokens != Evaluation.MaxOutputTokens) body["max_tokens"] = maxOutputTokens;
            var serialized = body.ToJsonString();
            // The real provider checks the original request; adding a field must not bypass its bound.
            if (serialized.Length > CoachLimits.PromptCharacters)
                throw CoachFailure.WithDetails(new UpstreamServiceException("AI Coach context is too large.", "coach_context_too_large"), new("RequestSize"));
            request.Content = new StringContent(serialized, Encoding.UTF8, "application/json");
            original.Dispose();
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

using System.Text;
using System.Text.Json.Nodes;
using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Services;

namespace LearnWord.CoachEval;

// Only the standalone evaluation uses this adapter; production provider/configuration stay unchanged.
public static class EvaluationHttp
{
    public static HttpClient CreateClient(string? reasoningEffort, HttpMessageHandler? inner = null)
    {
        EvaluationOptions.ValidateReasoningEffort(reasoningEffort);
        inner ??= new HttpClientHandler();
        return new HttpClient(reasoningEffort == null ? inner : new ReasoningHandler(reasoningEffort, inner))
        { Timeout = Timeout.InfiniteTimeSpan };
    }

    private sealed class ReasoningHandler(string effort, HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var original = request.Content ?? throw new InvalidOperationException("Evaluation request content is required.");
            var body = JsonNode.Parse(await original.ReadAsStringAsync(cancellationToken))!.AsObject();
            body["reasoning"] = new JsonObject { ["effort"] = effort };
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

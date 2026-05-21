using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Options;
using Microsoft.Extensions.Options;

namespace LearnWord.WebApi.Services
{
    public class OpenRouterAiCardGenerationProvider : IAiCardGenerationProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient httpClient;
        private readonly OpenRouterOptions options;
        private readonly ILogger<OpenRouterAiCardGenerationProvider> logger;

        public OpenRouterAiCardGenerationProvider(
            HttpClient httpClient,
            IOptions<AiCardGenerationOptions> options,
            ILogger<OpenRouterAiCardGenerationProvider> logger)
        {
            this.httpClient = httpClient;
            this.options = options.Value.OpenRouter;
            this.logger = logger;
        }

        public async Task<AiCardGenerationResponse> GenerateCards(AiCardGenerationRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new UpstreamServiceException("OpenRouter API key is not configured.", "ai_provider_not_configured");
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, options.BaseUrl);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            httpRequest.Content = JsonContent.Create(BuildRequestBody(request), options: JsonOptions);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds
                    ?? response.Headers.RetryAfter?.Date?.Subtract(DateTimeOffset.UtcNow).TotalSeconds;
                logger.LogWarning(
                    "OpenRouter generation failed with {StatusCode}. RetryAfterSeconds={RetryAfterSeconds}. ResponseBody={ResponseBody}",
                    (int)response.StatusCode,
                    retryAfter,
                    TruncateForLog(responseBody));

                throw BuildProviderException(response.StatusCode);
            }

            var completion = JsonSerializer.Deserialize<OpenRouterChatResponse>(responseBody, JsonOptions);
            var content = completion?.Choices.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new UpstreamServiceException("AI provider returned an empty response.", "ai_provider_empty_response");
            }

            try
            {
                var result = JsonSerializer.Deserialize<AiCardGenerationResponse>(content, JsonOptions);

                if (result == null)
                {
                    throw new JsonException("Empty JSON payload.");
                }

                return result;
            }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "Failed to parse AI provider structured output.");
                throw new UpstreamServiceException(
                    "AI provider returned invalid structured output.",
                    "ai_provider_invalid_json");
            }
        }

        private static UpstreamServiceException BuildProviderException(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.TooManyRequests => new UpstreamServiceException(
                    StatusCodes.Status429TooManyRequests,
                    "AI provider rate limit",
                    "ai_provider_rate_limited",
                    "AI generation is temporarily rate limited by the provider. Please try again in a minute."),
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new UpstreamServiceException(
                    StatusCodes.Status502BadGateway,
                    "AI provider configuration error",
                    "ai_provider_configuration_error",
                    "AI generation provider is not configured correctly."),
                _ => new UpstreamServiceException(
                    StatusCodes.Status503ServiceUnavailable,
                    "AI provider unavailable",
                    "ai_provider_unavailable",
                    "AI generation provider is temporarily unavailable. Please try again later.")
            };
        }

        private static string TruncateForLog(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            return value.Length <= 1000 ? value : value[..1000];
        }

        private object BuildRequestBody(AiCardGenerationRequest request)
        {
            var sourceLanguage = string.IsNullOrWhiteSpace(request.SourceLanguage) ? "auto-detect" : request.SourceLanguage.Trim();
            var targetLanguage = string.IsNullOrWhiteSpace(request.TargetLanguage) ? "the user's target language" : request.TargetLanguage.Trim();
            var level = string.IsNullOrWhiteSpace(request.Level) ? "appropriate beginner/intermediate level" : request.Level.Trim();

            return new
            {
                model = options.Model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You generate concise draft vocabulary cards for language learners. Return only JSON matching the provided schema. Use empty strings for unknown transcription, example, or explanation."
                    },
                    new
                    {
                        role = "user",
                        content = $"Extract up to {request.MaxCards} useful vocabulary cards from this text. Source language: {sourceLanguage}. Target language: {targetLanguage}. Learner level: {level}.\n\nText:\n{request.SourceText.Trim()}"
                    }
                },
                response_format = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "ai_card_generation_response",
                        strict = true,
                        schema = BuildResponseSchema()
                    }
                }
            };
        }

        private static object BuildResponseSchema()
        {
            return new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "cards" },
                properties = new
                {
                    cards = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[] { "value", "transcription", "translation", "example", "explanation", "difficulty" },
                            properties = new
                            {
                                value = new { type = "string" },
                                transcription = new { type = "string" },
                                translation = new { type = "string" },
                                example = new { type = "string" },
                                explanation = new { type = "string" },
                                difficulty = new { type = "string" }
                            }
                        }
                    }
                }
            };
        }

        private sealed class OpenRouterChatResponse
        {
            [JsonPropertyName("choices")]
            public List<OpenRouterChoice> Choices { get; set; } = [];
        }

        private sealed class OpenRouterChoice
        {
            [JsonPropertyName("message")]
            public OpenRouterMessage? Message { get; set; }
        }

        private sealed class OpenRouterMessage
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}

namespace LearnWord.WebApi.Options
{
    public class OpenRouterOptions
    {
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";
        public string Model { get; set; } = "google/gemma-4-26b-a4b-it:free";
        public string? ApiKey { get; set; }
        public int TimeoutSeconds { get; set; } = 60;
    }
}

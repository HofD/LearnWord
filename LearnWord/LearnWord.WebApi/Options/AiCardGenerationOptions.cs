namespace LearnWord.WebApi.Options
{
    public class AiCardGenerationOptions
    {
        public string Provider { get; set; } = "Fake";
        public int MaxSourceTextLength { get; set; } = 4000;
        public int MaxCards { get; set; } = 10;
        public OpenRouterOptions OpenRouter { get; set; } = new();
    }
}

namespace LearnWord.WebApi.Options;

public class AiCoachOptions
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "Fake";
    public OpenRouterOptions OpenRouter { get; set; } = new() { Model = "z-ai/glm-5.3-flash" };
    public string? ReasoningEffort { get; set; } = "low";
    public int MaxOutputTokens { get; set; } = 3000;
}

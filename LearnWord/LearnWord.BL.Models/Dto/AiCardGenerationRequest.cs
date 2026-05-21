namespace LearnWord.BL.Models.Dto
{
    public class AiCardGenerationRequest
    {
        public string SourceText { get; set; } = string.Empty;
        public string? SourceLanguage { get; set; }
        public string? TargetLanguage { get; set; }
        public string? Level { get; set; }
        public int MaxCards { get; set; } = 5;
    }
}

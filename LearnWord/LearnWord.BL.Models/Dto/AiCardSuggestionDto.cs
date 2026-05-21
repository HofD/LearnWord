namespace LearnWord.BL.Models.Dto
{
    public class AiCardSuggestionDto
    {
        public string Value { get; set; } = string.Empty;
        public string Transcription { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
    }
}

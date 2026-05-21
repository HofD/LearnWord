using LearnWord.BL.Models.Dto;
using LearnWord.WebApi.Abstractions;

namespace LearnWord.WebApi.Services
{
    public class FakeAiCardGenerationProvider : IAiCardGenerationProvider
    {
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a",
            "an",
            "and",
            "are",
            "for",
            "from",
            "the",
            "this",
            "that",
            "with"
        };

        public Task<AiCardGenerationResponse> GenerateCards(AiCardGenerationRequest request, CancellationToken cancellationToken)
        {
            var words = request.SourceText
                .Split([' ', '\r', '\n', '\t', ',', '.', ';', ':', '!', '?', '"', '(', ')', '[', ']'], StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.Trim('\'', '-'))
                .Where(word => word.Length > 2 && !StopWords.Contains(word))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(request.MaxCards)
                .ToList();

            if (words.Count == 0)
            {
                words.Add("word");
            }

            var targetLanguage = string.IsNullOrWhiteSpace(request.TargetLanguage) ? "target language" : request.TargetLanguage.Trim();
            var level = string.IsNullOrWhiteSpace(request.Level) ? "A1" : request.Level.Trim();

            var response = new AiCardGenerationResponse
            {
                Cards = words.Select(word => new AiCardSuggestionDto
                {
                    Value = word,
                    Transcription = string.Empty,
                    Translation = $"[{targetLanguage}] {word}",
                    Example = request.SourceText.Trim(),
                    Explanation = "Local fake AI suggestion for development and tests.",
                    Difficulty = level
                }).ToList()
            };

            return Task.FromResult(response);
        }
    }
}

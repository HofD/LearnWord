using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;

namespace LearnWord.Identity.Services
{
    public class WordHttpService : UpstreamHttpService<WordHttpService>, IWordHttpService
    {
        private const string UpstreamService = "Words";
        private readonly string serviceBaseUrl;

        public WordHttpService(IConfiguration configuration, ILogger<WordHttpService>? logger = null)
            : base(logger)
        {
            this.serviceBaseUrl = configuration["LwServicesRoutes:WordsRoute"]
                ?? throw new InvalidOperationException("LwServicesRoutes:WordsRoute configuration is missing.");
        }

        public async Task<WordDto> Add(int cardId, WordCreateDto wordCreateDto)
        {
            var request = $"{serviceBaseUrl}/{cardId}/words";
            return await SendForJson<WordDto>(
                UpstreamService,
                "CreateWord",
                request,
                () => HttpClient.PostAsJsonAsync(request, wordCreateDto),
                "words_service_empty_response",
                $"Failed to create word for card {cardId}.");
        }

        public async Task<bool> Remove(int id, int cardId)
        {
            var request = $"{serviceBaseUrl}/{cardId}/words/{id}";
            return await SendForSuccess(
                UpstreamService,
                "RemoveWord",
                request,
                () => HttpClient.DeleteAsync(request),
                $"Failed to remove word {id}.");
        }

        public async Task<WordDto> Update(int cardId, int id, WordUpdateDto wordUpdateDto)
        {
            var request = $"{serviceBaseUrl}/{cardId}/words/{id}";
            return await SendForJson<WordDto>(
                UpstreamService,
                "UpdateWord",
                request,
                () => HttpClient.PutAsJsonAsync(request, wordUpdateDto),
                "words_service_empty_response",
                $"Failed to update word {id}.");
        }
    }
}

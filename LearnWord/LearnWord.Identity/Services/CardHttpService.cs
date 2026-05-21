using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;

namespace LearnWord.Identity.Services
{
    public class CardHttpService : UpstreamHttpService<CardHttpService>, ICardHttpService
    {
        private const string UpstreamService = "Cards";
        private readonly string serviceBaseUrl;

        public CardHttpService(IConfiguration configuration, ILogger<CardHttpService>? logger = null)
            : base(logger)
        {
            this.serviceBaseUrl = configuration["LwServicesRoutes:CardsRoute"]
                ?? throw new InvalidOperationException("LwServicesRoutes:CardsRoute configuration is missing.");
        }

        public async Task<CardDto> Add(CardCreateDto cardCreateDto)
        {
            return await SendForJson<CardDto>(
                UpstreamService,
                "CreateCard",
                serviceBaseUrl,
                () => HttpClient.PostAsJsonAsync(serviceBaseUrl, cardCreateDto),
                "cards_service_empty_response",
                "Failed to create card.");
        }

        public async Task<CardDto> Forget(int id)
        {
            var request = $"{serviceBaseUrl}/{id}/forget";
            return await SendForJson<CardDto>(
                UpstreamService,
                "ForgetCard",
                request,
                () => HttpClient.PostAsJsonAsync(request, new { id }),
                "cards_service_empty_response",
                $"Failed to forget card {id}.");
        }

        public async Task<CardDto> Learn(int id)
        {
            var request = $"{serviceBaseUrl}/{id}/learn";
            return await SendForJson<CardDto>(
                UpstreamService,
                "LearnCard",
                request,
                () => HttpClient.PostAsJsonAsync(request, new { id }),
                "cards_service_empty_response",
                $"Failed to learn card {id}.");
        }

        public async Task<CardDto> Review(int id, ReviewCardRequest reviewRequest)
        {
            var request = $"{serviceBaseUrl.Replace("/cards", "/review/cards")}/{id}/review";
            return await SendForJson<CardDto>(
                UpstreamService,
                "ReviewCard",
                request,
                () => HttpClient.PostAsJsonAsync(request, reviewRequest),
                "cards_service_empty_response",
                $"Failed to review card {id}.");
        }

        public async Task<bool> Remove(int id)
        {
            var request = $"{serviceBaseUrl}/{id}";
            return await SendForSuccess(
                UpstreamService,
                "RemoveCard",
                request,
                () => HttpClient.DeleteAsync(request),
                $"Failed to remove card {id}.");
        }
    }
}

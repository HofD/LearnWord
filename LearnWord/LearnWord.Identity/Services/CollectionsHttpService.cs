using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;

namespace LearnWord.Identity.Services
{
    public class CollectionsHttpService : UpstreamHttpService<CollectionsHttpService>, ICollectionsHttpService
    {
        private const string UpstreamService = "Collections";
        private readonly string serviceBaseUrl;

        public CollectionsHttpService(IConfiguration configuration, ILogger<CollectionsHttpService>? logger = null)
            : base(logger)
        {
            this.serviceBaseUrl = configuration["LwServicesRoutes:CollectionsRoute"]
                ?? throw new InvalidOperationException("LwServicesRoutes:CollectionsRoute configuration is missing.");
        }

        public async Task<CollectionDto> Add(CollectionCreateDto createDto)
        {
            return await SendForJson<CollectionDto>(
                UpstreamService,
                "CreateCollection",
                serviceBaseUrl,
                () => HttpClient.PostAsJsonAsync(serviceBaseUrl, createDto),
                "collections_service_empty_response",
                "Failed to create collection.");
        }

        public async Task<CollectionDto> Get(int id)
        {
            var request = $"{serviceBaseUrl}/{id}";
            return await SendForJson<CollectionDto>(
                UpstreamService,
                "GetCollection",
                request,
                () => HttpClient.GetAsync(request),
                "collections_service_empty_response",
                $"Failed to get collection {id}.");
        }

        public async Task<IEnumerable<CardDto>> GetCardsForReview(int collectionId)
        {
            var request = $"{serviceBaseUrl}/{collectionId}/review-cards";
            return await SendForJson<IEnumerable<CardDto>>(
                UpstreamService,
                "GetReviewCards",
                request,
                () => HttpClient.GetAsync(request),
                "collections_service_empty_response",
                $"Failed to get review cards for collection {collectionId}.");
        }

        public async Task<AiCardGenerationResponse> GenerateAiCards(int collectionId, AiCardGenerationRequest request)
        {
            var targetUrl = $"{serviceBaseUrl}/{collectionId}/ai/generate-cards";
            return await SendForJson<AiCardGenerationResponse>(
                UpstreamService,
                "GenerateAiCards",
                targetUrl,
                () => HttpClient.PostAsJsonAsync(targetUrl, request),
                "collections_service_empty_response",
                $"Failed to generate AI cards for collection {collectionId}.");
        }

        public async Task<CollectionListDto> GetList(int[] ids)
        {
            var request = $"{serviceBaseUrl}?";

            foreach (var id in ids)
            {
                request += $"&ids={id}";
            }

            return await SendForJson<CollectionListDto>(
                UpstreamService,
                "GetCollectionsList",
                request,
                () => HttpClient.GetAsync(request),
                "collections_service_empty_response",
                "Failed to get collections list.");
        }

        public async Task<bool> Remove(int id)
        {
            var request = $"{serviceBaseUrl}/{id}";

            return await SendForSuccess(
                UpstreamService,
                "RemoveCollection",
                request,
                () => HttpClient.DeleteAsync(request),
                $"Failed to remove collection {id}.");
        }

        public async Task<CollectionDto> Rename(int id, CollectionRenameDto renameDto)
        {
            var request = $"{serviceBaseUrl}/{id}";

            return await SendForJson<CollectionDto>(
                UpstreamService,
                "RenameCollection",
                request,
                () => HttpClient.PutAsJsonAsync(request, renameDto),
                "collections_service_empty_response",
                $"Failed to rename collection {id}.");
        }
    }
}

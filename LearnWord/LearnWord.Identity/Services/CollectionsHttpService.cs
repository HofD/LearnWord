using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.Identity.Abstactions;

namespace LearnWord.Identity.Services
{
    public class CollectionsHttpService : ICollectionsHttpService
    {
        private readonly HttpClient httpClient = new HttpClient();
        private readonly string serviceBaseUrl;

        public CollectionsHttpService(IConfiguration configuration)
        {
            this.serviceBaseUrl = configuration["LwServicesRoutes:CollectionsRoute"];
        }

        public async Task<CollectionDto> Add(CollectionCreateDto createDto)
        {
            using var response = await httpClient.PostAsJsonAsync(serviceBaseUrl, createDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CollectionDto>();

                if (result == null)
                {
                    throw new UpstreamServiceException("Collections service returned an empty response.", "collections_service_empty_response");
                }

                return result;
            }

            throw new UpstreamServiceException($"Failed to create collection. Collections service returned {(int)response.StatusCode}.");
        }

        public async Task<CollectionDto> Get(int id)
        {
            using var response = await httpClient.GetAsync($"{serviceBaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CollectionDto>();

                if (result == null)
                {
                    throw new UpstreamServiceException("Collections service returned an empty response.", "collections_service_empty_response");
                }

                return result;
            }

            throw new UpstreamServiceException($"Failed to get collection {id}. Collections service returned {(int)response.StatusCode}.");
        }

        public async Task<IEnumerable<CardDto>> GetCardsForReview(int collectionId)
        {
            using var response = await httpClient.GetAsync($"{serviceBaseUrl}/{collectionId}/review-cards");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<IEnumerable<CardDto>>();

                if (result == null)
                {
                    throw new UpstreamServiceException("Collections service returned an empty response.", "collections_service_empty_response");
                }

                return result;
            }

            throw new UpstreamServiceException($"Failed to get review cards for collection {collectionId}. Collections service returned {(int)response.StatusCode}.");
        }

        public async Task<CollectionListDto> GetList(int[] ids)
        {
            var request = $"{serviceBaseUrl}?";

            foreach (var id in ids)
            {
                request += $"&ids={id}";
            }

            using var response = await httpClient.GetAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CollectionListDto>();

                if (result == null)
                {
                    throw new UpstreamServiceException("Collections service returned an empty response.", "collections_service_empty_response");
                }

                return result;
            }

            throw new UpstreamServiceException($"Failed to get collections list. Collections service returned {(int)response.StatusCode}.");
        }

        public async Task<bool> Remove(int id)
        {
            var request = $"{serviceBaseUrl}/{id}";

            using var response = await httpClient.DeleteAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            throw new UpstreamServiceException($"Failed to remove collection {id}. Collections service returned {(int)response.StatusCode}.");
        }

        public async Task<CollectionDto> Rename(int id, CollectionRenameDto renameDto)
        {
            var request = $"{serviceBaseUrl}/{id}";

            using var response = await httpClient.PutAsJsonAsync(request, renameDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CollectionDto>();

                if (result == null)
                {
                    throw new UpstreamServiceException("Collections service returned an empty response.", "collections_service_empty_response");
                }

                return result;
            }

            throw new UpstreamServiceException($"Failed to rename collection {id}. Collections service returned {(int)response.StatusCode}.");
        }
    }
}

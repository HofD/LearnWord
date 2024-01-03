using LearningWords.BL.Models.Dto;
using LearningWords.Collections.Identity.Abstactions;

namespace LearningWords.Collections.Identity.Services
{
    public class CollectionsHttpService : ICollectionsHttpService
    {
        private readonly HttpClient httpClient = new HttpClient();
        private readonly string serviceBaseUrl;

        public CollectionsHttpService(IConfiguration configuration)
        {
            this.serviceBaseUrl = configuration["LwServicesRoutes:CollectionsRoute"];
        }

        public async Task<CollectionDto?> Add(CollectionCreateDto createDto)
        {
            using var response = await httpClient.PostAsJsonAsync(serviceBaseUrl, createDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CollectionDto>();

                return result;
            }

            return null;
        }

        public async Task<CollectionDto?> Get(int id)
        {
            using var response = await httpClient.GetAsync($"{serviceBaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CollectionDto>();

                return result;
            }

            return null;
        }

        public async Task<CollectionListDto?> GetList(int[] ids)
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

                return result;
            }

            return null;
        }

        public async Task<bool> Remove(int id)
        {
            var request = $"{serviceBaseUrl}/{id}";

            using var response = await httpClient.DeleteAsync(request);

            return response.IsSuccessStatusCode;
        }

        public async Task<CollectionDto?> Rename(int id, CollectionRenameDto renameDto)
        {
            var request = $"{serviceBaseUrl}/{id}";

            using var response = await httpClient.PutAsJsonAsync(request, renameDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CollectionDto>();

                return result;
            }

            return null;
        }
    }
}

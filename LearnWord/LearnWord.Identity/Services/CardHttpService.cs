using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;

namespace LearnWord.Identity.Services
{
    public class CardHttpService : ICardHttpService
    {
        private readonly HttpClient httpClient = new HttpClient();
        private readonly string serviceBaseUrl;

        public CardHttpService(IConfiguration configuration)
        {
            this.serviceBaseUrl = configuration["LwServicesRoutes:CardsRoute"];
        }

        public async Task<CardDto?> Add(CardCreateDto cardCreateDto)
        {
            using var response = await httpClient.PostAsJsonAsync(serviceBaseUrl, cardCreateDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CardDto>();

                return result;
            }

            throw new Exception($"Failed to create new card. Server status code: {response.StatusCode}");
        }

        public async Task<CardDto> Forget(int id)
        {
            using var response = await httpClient.PostAsJsonAsync($"{serviceBaseUrl}/{id}/forget", new { id });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CardDto>();

                return result;
            }

            throw new Exception($"Failed to forget new card. Server status code: {response.StatusCode}");
        }

        public async Task<CardDto> Learn(int id)
        {
            using var response = await httpClient.PostAsJsonAsync($"{serviceBaseUrl}/{id}/learn", new { id });
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CardDto>();

                return result;
            }

            throw new Exception($"Failed to learn new card. Server status code: {response.StatusCode}");
        }

        public async Task<bool> Remove(int id)
        {
            using var response = await httpClient.DeleteAsync($"{serviceBaseUrl}/{id}");

            return response.IsSuccessStatusCode;
        }
    }
}

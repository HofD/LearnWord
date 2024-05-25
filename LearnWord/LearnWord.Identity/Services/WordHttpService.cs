using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;

namespace LearnWord.Identity.Services
{
    public class WordHttpService : IWordHttpService
    {
        private readonly HttpClient httpClient = new HttpClient();
        private readonly string serviceBaseUrl;

        public WordHttpService(IConfiguration configuration)
        {
            this.serviceBaseUrl = configuration["LwServicesRoutes:WordsRoute"];
        }

        public async Task<WordDto?> Add(int cardId, WordCreateDto wordCreateDto)
        {
            using var response = await httpClient.PostAsJsonAsync($"{serviceBaseUrl}/{cardId}/words", wordCreateDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<WordDto>();

                return result;
            }

            throw new Exception($"Failed to create new word for card {cardId}. Server status code: {response.StatusCode}");
        }

        public async Task<bool> Remove(int id, int cardId)
        {
            using var response = await httpClient.DeleteAsync($"{serviceBaseUrl}/{cardId}/words/{id}");

            return response.IsSuccessStatusCode;
        }

        public async Task<WordDto> Update(int cardId, int id, WordUpdateDto wordUpdateDto)
        {
            using var response = await httpClient.PutAsJsonAsync($"{serviceBaseUrl}/{cardId}/words/{id}", wordUpdateDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<WordDto>();

                return result;
            }

            throw new Exception($"Failed to update word {id}. Server status code: {response.StatusCode}");
        }
    }
}

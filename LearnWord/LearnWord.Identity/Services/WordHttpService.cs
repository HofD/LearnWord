using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
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

        public async Task<WordDto> Add(int cardId, WordCreateDto wordCreateDto)
        {
            using var response = await httpClient.PostAsJsonAsync($"{serviceBaseUrl}/{cardId}/words", wordCreateDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<WordDto>();

                if (result == null)
                {
                    throw new UpstreamServiceException("Words service returned an empty response.", "words_service_empty_response");
                }

                return result;
            }

            throw new UpstreamServiceException($"Failed to create word for card {cardId}. Words service returned {(int)response.StatusCode}.");
        }

        public async Task<bool> Remove(int id, int cardId)
        {
            using var response = await httpClient.DeleteAsync($"{serviceBaseUrl}/{cardId}/words/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            throw new UpstreamServiceException($"Failed to remove word {id}. Words service returned {(int)response.StatusCode}.");
        }

        public async Task<WordDto> Update(int cardId, int id, WordUpdateDto wordUpdateDto)
        {
            using var response = await httpClient.PutAsJsonAsync($"{serviceBaseUrl}/{cardId}/words/{id}", wordUpdateDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<WordDto>();

                if (result == null)
                {
                    throw new UpstreamServiceException("Words service returned an empty response.", "words_service_empty_response");
                }

                return result;
            }

            throw new UpstreamServiceException($"Failed to update word {id}. Words service returned {(int)response.StatusCode}.");
        }
    }
}

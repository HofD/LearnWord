using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
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

        public async Task<CardDto> Add(CardCreateDto cardCreateDto)
        {
            using var response = await httpClient.PostAsJsonAsync(serviceBaseUrl, cardCreateDto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CardDto>();

                if (result == null)
                {
                    throw new UpstreamServiceException("Cards service returned an empty response.", "cards_service_empty_response");
                }

                return result;
            }

            throw new UpstreamServiceException($"Failed to create card. Cards service returned {(int)response.StatusCode}.");
        }

        public async Task<CardDto> Forget(int id)
        {
            using var response = await httpClient.PostAsJsonAsync($"{serviceBaseUrl}/{id}/forget", new { id });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CardDto>();

                if (result == null)
                {
                    throw new UpstreamServiceException("Cards service returned an empty response.", "cards_service_empty_response");
                }

                return result;
            }

            throw new UpstreamServiceException($"Failed to forget card {id}. Cards service returned {(int)response.StatusCode}.");
        }

        public async Task<CardDto> Learn(int id)
        {
            using var response = await httpClient.PostAsJsonAsync($"{serviceBaseUrl}/{id}/learn", new { id });
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CardDto>();

                if (result == null)
                {
                    throw new UpstreamServiceException("Cards service returned an empty response.", "cards_service_empty_response");
                }

                return result;
            }

            throw new UpstreamServiceException($"Failed to learn card {id}. Cards service returned {(int)response.StatusCode}.");
        }

        public async Task<bool> Remove(int id)
        {
            using var response = await httpClient.DeleteAsync($"{serviceBaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            throw new UpstreamServiceException($"Failed to remove card {id}. Cards service returned {(int)response.StatusCode}.");
        }
    }
}

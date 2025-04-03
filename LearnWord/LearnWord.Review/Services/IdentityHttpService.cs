using LearnWord.Review.Abstractions;

namespace LearnWord.Review.Services;

public class IdentityHttpService : IIdentityHttpService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityHttpService(
        HttpClient httpClient, 
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _httpClient.BaseAddress = new Uri(_configuration["Services:Identity:BaseUrl"]);
    }

    public async Task<bool> IsUserCollectionOwnerAsync(string userId, int collectionId)
    {
        var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
        if (token != null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        
        var response = await _httpClient.GetAsync($"/collections/{collectionId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> IsUserCardOwnerAsync(string userId, int cardId)
    {
        var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
        if (token != null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        
        var response = await _httpClient.GetAsync($"/cards/{cardId}");
        return response.IsSuccessStatusCode;
    }
} 
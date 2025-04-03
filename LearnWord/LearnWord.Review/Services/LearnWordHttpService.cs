using LearnWord.BL.Models.Dto;
using LearnWord.Review.Abstractions;
using LearnWord.Review.Models;

namespace LearnWord.Review.Services;

public class LearnWordHttpService : ILearnWordHttpService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public LearnWordHttpService(
        HttpClient httpClient, 
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.BaseAddress = new Uri(_configuration["Services:LearnWord:BaseUrl"]);
    }

    public async Task<IEnumerable<CardDto>> GetReviewCardsAsync(int collectionId)
    {
        var response = await _httpClient.GetAsync($"/collections/{collectionId}/review-cards");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<CardDto>>();
    }

    public async Task<ReviewCardDto> GetCardAsync(int cardId)
    {
        var response = await _httpClient.GetAsync($"/cards/{cardId}");
        response.EnsureSuccessStatusCode();
        var card = await response.Content.ReadFromJsonAsync<CardDto>();
        
        return new ReviewCardDto
        {
            Id = card.Id,
            CollectionId = card.CollectionId,
            Words = card.Words,
            Learnt = card.Learnt
        };
    }

    public async Task<ReviewCardDto> ShowCardAsync(int cardId)
    {
        var response = await _httpClient.PostAsync($"/cards/{cardId}/show", null);
        response.EnsureSuccessStatusCode();
        var card = await response.Content.ReadFromJsonAsync<CardDto>();
        
        return new ReviewCardDto
        {
            Id = card.Id,
            CollectionId = card.CollectionId,
            Words = card.Words,
            Learnt = card.Learnt
        };
    }

    public async Task<ReviewCardDto> LearnCardAsync(int cardId)
    {
        var response = await _httpClient.PostAsync($"/cards/{cardId}/learn", null);
        response.EnsureSuccessStatusCode();
        var card = await response.Content.ReadFromJsonAsync<CardDto>();
        
        return new ReviewCardDto
        {
            Id = card.Id,
            CollectionId = card.CollectionId,
            Words = card.Words,
            Learnt = card.Learnt
        };
    }

    public async Task<ReviewCardDto> ForgetCardAsync(int cardId)
    {
        var response = await _httpClient.PostAsync($"/cards/{cardId}/forget", null);
        response.EnsureSuccessStatusCode();
        var card = await response.Content.ReadFromJsonAsync<CardDto>();
        
        return new ReviewCardDto
        {
            Id = card.Id,
            CollectionId = card.CollectionId,
            Words = card.Words,
            Learnt = card.Learnt
        };
    }
} 
using LearnWord.BL.Models.Dto;
using LearnWord.DAL.Models;
using LearnWord.Identity.Abstactions;

namespace LearnWord.Identity.Services;

public class SocialCollectionsHttpService : UpstreamHttpService<SocialCollectionsHttpService>, ISocialCollectionsHttpService
{
    private readonly string collectionsUrl;
    private readonly string socialUrl;
    public SocialCollectionsHttpService(IConfiguration configuration, ILogger<SocialCollectionsHttpService>? logger = null) : base(logger)
    {
        collectionsUrl = configuration["LwServicesRoutes:CollectionsRoute"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("LwServicesRoutes:CollectionsRoute configuration is missing.");
        socialUrl = new Uri(new Uri(collectionsUrl + "/"), "../social/collections").AbsoluteUri;
    }

    public Task<PagedResult<SocialCollectionRecord>> Explore(int page, int pageSize, string? search)
    {
        var url = $"{socialUrl}?page={page}&pageSize={pageSize}&search={Uri.EscapeDataString(search ?? string.Empty)}";
        return Read<PagedResult<SocialCollectionRecord>>(url, "ExploreCollections", () => HttpClient.GetAsync(url));
    }
    public Task<SocialCollectionPage> Get(Guid publicId, int page, int pageSize, string? search = null)
    {
        var url = $"{socialUrl}/{publicId}?page={page}&pageSize={pageSize}&search={Uri.EscapeDataString(search ?? string.Empty)}";
        return Read<SocialCollectionPage>(url, "ReadSharedCollection", () => HttpClient.GetAsync(url));
    }
    public Task<CollectionDto> Fork(Guid publicId)
    {
        var url = $"{socialUrl}/{publicId}/fork";
        return Read<CollectionDto>(url, "ForkCollection", () => HttpClient.PostAsync(url, null));
    }
    public Task<CollectionDto> SetVisibility(int id, CollectionVisibility visibility)
    {
        var url = $"{collectionsUrl}/{id}/visibility";
        return Read<CollectionDto>(url, "SetCollectionVisibility", () => HttpClient.PutAsJsonAsync(url, new CollectionVisibilityDto { Visibility = visibility }));
    }
    public Task<bool> Remove(int id)
    {
        var url = $"{collectionsUrl}/{id}";
        return SendForSuccess("Collections", "RemoveFailedFork", url, () => HttpClient.DeleteAsync(url), "Failed to remove incomplete fork.");
    }
    private Task<T> Read<T>(string url, string operation, Func<Task<HttpResponseMessage>> send)
        => SendForJson<T>("Collections", operation, url, send, "collections_service_empty_response", "Collections operation failed.");
}

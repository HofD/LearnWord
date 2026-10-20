using LearnWord.BL.Models.Dto;
using LearnWord.DAL.Models;

namespace LearnWord.Identity.Abstactions;

public interface ISocialCollectionsHttpService
{
    Task<PagedResult<SocialCollectionRecord>> Explore(int page, int pageSize, string? search);
    Task<SocialCollectionPage> Get(Guid publicId, int page, int pageSize, string? search = null);
    Task<CollectionDto> Fork(Guid publicId);
    Task<CollectionDto> SetVisibility(int id, CollectionVisibility visibility);
    Task<bool> Remove(int id);
}

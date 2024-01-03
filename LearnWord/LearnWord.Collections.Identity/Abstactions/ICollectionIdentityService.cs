using LearnWord.BL.Models.Dto;

namespace LearnWord.Collections.Identity.Abstactions
{
    public interface ICollectionIdentityService
    {
        Task<CollectionDto> Add(CollectionCreateDto createDto, string userId);
        Task<CollectionDto?> Rename(int id, CollectionRenameDto renameDto, string userId);
        Task<bool?> Remove(int id, string userId);
        Task<CollectionDto?> Get(int id, string userId);
        Task<CollectionListDto> GetAll(string userId);
    }
}

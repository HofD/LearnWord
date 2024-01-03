using LearnWord.BL.Models.Dto;

namespace LearnWord.Collections.Identity.Abstactions
{
    public interface ICollectionsHttpService
    {
        Task<CollectionDto?> Add(CollectionCreateDto createDto);
        Task<CollectionDto?> Rename(int id, CollectionRenameDto renameDto);
        Task<bool> Remove(int id);
        Task<CollectionDto?> Get(int id);
        Task<CollectionListDto?> GetList(int[] ids);
    }
}

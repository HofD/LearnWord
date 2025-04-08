using LearnWord.BL.Models.Dto;

namespace LearnWord.Identity.Abstactions
{
    public interface ICollectionsHttpService
    {
        Task<CollectionDto?> Add(CollectionCreateDto createDto);
        Task<CollectionDto?> Rename(int id, CollectionRenameDto renameDto);
        Task<bool> Remove(int id);
        Task<CollectionDto?> Get(int id);
        Task<CollectionListDto?> GetList(int[] ids);
        Task<IEnumerable<CardDto>?> GetCardsForReview(int collectionId);
    }
}

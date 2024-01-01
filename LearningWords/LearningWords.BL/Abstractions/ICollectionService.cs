using LearningWords.BL.Models.Dto;

namespace LearningWords.BL.Abstractions
{
    public interface ICollectionService
    {
        Task<CollectionDto> Add(CollectionCreateDto createDto);
        Task<CollectionDto> Rename(int id, CollectionRenameDto renameDto);
        Task Remove(int id);
        Task<CollectionDto> Get(int id);
        Task<CollectionListDto> GetList(List<int> ids);
    }
}

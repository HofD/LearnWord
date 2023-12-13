using LearningWords.BL.Models.Dto;

namespace LearningWords.BL.Abstractions
{
    public interface ICollectionService
    {
        Task<CollectionDto> Add(CollectionCreateDto createDto);
        Task Rename(int id, CollectionRenameDto renameDto);
        Task Remove(int id);
        Task<CollectionDto> Get(int id);
    }
}

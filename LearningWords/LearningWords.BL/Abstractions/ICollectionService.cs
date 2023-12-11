using LearningWords.BL.Models.Dto;

namespace LearningWords.BL.Abstractions
{
    public interface ICollectionService
    {
        Task<CollectionDto> Create(CollectionCreateDto createDto);
        Task Rename(int id, CollectionRenameDto renameDto);
        Task Delete(int id);
        Task<CollectionDto> Get(int id);
    }
}

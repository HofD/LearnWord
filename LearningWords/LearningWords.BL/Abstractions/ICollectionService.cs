using LearningWords.BL.Models.Dto;

namespace LearningWords.BL.Abstractions
{
    public interface ICollectionService
    {
        /// <summary>
        /// Создать новую коллекцию.
        /// </summary>
        /// <param name="createDto">Коллекция, которую следует создать.</param>
        /// <returns>Возвращает созданную коллекцию.</returns>
        Task<CollectionDto> Create(CollectionCreateDto createDto);
        /// <summary>
        /// Переименовать коллекцию.
        /// </summary>
        /// <param name="renameDto">Коллекция, которую следует переименовать.</param>
        /// <returns>Возвращает переименнованную коллекцию.</returns>
        Task<CollectionDto> Rename(CollectionRenameDto renameDto);
        /// <summary>
        /// Удалить коллекцию.
        /// </summary>
        /// <param name="collectionDto">Коллекция, которую следует удалить.</param>
        /// <returns>Возвращает исключение, если коллекция не удалена.</returns>
        Task Delete(CollectionDto collectionDto);
        /// <summary>
        /// Получить коллекцию и все карточки в ней.
        /// </summary>
        /// <param name="id">Идентификатор коллекции.</param>
        /// <returns>Возвращает коллекцию либо null, если коллекция не существует.</returns>
        Task<CollectionDto> Get(int id);
        /// <summary>
        /// Получить список коллекций пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>Возвращает список коллекций пользователя.</returns>
        Task<IEnumerable<CollectionDto>> GetAll(string userId);
    }
}

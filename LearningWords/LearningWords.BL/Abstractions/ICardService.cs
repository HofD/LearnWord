using LearningWords.BL.Models.Dto;

namespace LearningWords.BL.Abstractions
{
    public interface ICardService
    {
        /// <summary>
        /// Создает новую карточку со словами.
        /// </summary>
        /// <param name="createDto">Карточка, которую следует создать.</param>
        /// <returns>Возвращает созданную карточку.</returns>
        Task<CardDto> Add(CardCreateDto createDto);
        /// <summary>
        /// Обновляет карточку и сбрасывает статистику.
        /// </summary>
        /// <param name="updateDto">Карточка, которую следует обновить.</param>
        /// <returns>Обновленная карточка.</returns>
        Task<CardDto> Update(CardUpdateDto updateDto);
        /// <summary>
        /// Удаляет карточку.
        /// </summary>
        /// <param name="cardDto"></param>
        /// <returns></returns>
        Task<CardDto> Delete(CardDto cardDto);
        /// <summary>
        /// Получить все карточки в коллекции.
        /// </summary>
        /// <param name="collectionId">Идентификатор коллекции.</param>
        /// <returns>Список карточек в коллекции.</returns>
        Task<IEnumerable<CardDto>> GetAll(int collectionId);
    }
}

using LearningWords.BL.Abstractions;
using LearningWords.BL.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearningWords.BL.Services
{
    public class CardService : ICardService
    {
        public Task<CardDto> Create(CardCreateDto createDto)
        {
            throw new NotImplementedException();
        }

        public Task<CardDto> Delete(CardDto cardDto)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CardDto>> GetAll(int collectionId)
        {
            throw new NotImplementedException();
        }

        public Task<CardDto> Update(CardUpdateDto updateDto)
        {
            throw new NotImplementedException();
        }
    }
}

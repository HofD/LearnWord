using AutoMapper;
using LearningWords.BL.Abstractions;
using LearningWords.BL.Models.Dto;
using LearningWords.DAL.Models;
using LearningWords.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearningWords.BL.Services
{
    public class CardService : ICardService
    {
        private readonly CardRepository cardRepository;
        private readonly IMapper mapper;

        public CardService(CardRepository cardRepository, IMapper mapper)
        {
            this.cardRepository = cardRepository;
            this.mapper = mapper;
        }
        public async Task<CardDto> Add(CardCreateDto createDto)
        {
            return mapper.Map<CardDto>(await cardRepository.Add(mapper.Map<Card>(createDto)));
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

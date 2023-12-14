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

        public async Task Remove(int id)
        {
            await cardRepository.Remove(id);
        }
    }
}

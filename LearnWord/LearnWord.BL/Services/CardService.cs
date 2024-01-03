using AutoMapper;
using LearnWord.BL.Abstractions;
using LearnWord.BL.Models.Dto;
using LearnWord.DAL.Models;
using LearnWord.DAL.Repositories;

namespace LearnWord.BL.Services
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
        public async Task<CardDto> Forget(int id)
        {
            return mapper.Map<CardDto>(await cardRepository.Forget(id));
        }
        public async Task<CardDto> Learn(int id)
        {
            return mapper.Map<CardDto>(await cardRepository.Learn(id));
        }
        public async Task Remove(int id)
        {
            await cardRepository.Remove(id);
        }
        public async Task<CardDto> Reset(int id)
        {
            return mapper.Map<CardDto>(await cardRepository.Reset(id));
        }
    }
}

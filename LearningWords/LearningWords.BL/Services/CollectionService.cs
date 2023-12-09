using AutoMapper;
using LearningWords.BL.Abstractions;
using LearningWords.BL.Models.Dto;
using LearningWords.DAL.Repositories;

namespace LearningWords.BL.Services
{
    public class CollectionService : ICollectionService
    {
        private readonly CollectionRepository collectionRepository;
        private readonly IMapper mapper;

        public CollectionService(CollectionRepository collectionRepository, IMapper mapper) 
        {
            this.collectionRepository = collectionRepository;
            this.mapper = mapper;
        }
        public async Task<CollectionDto> Create(CollectionCreateDto createDto)
        {
            return mapper.Map<CollectionDto>(await collectionRepository.Add(createDto));
        }

        public async Task<CollectionDto> Get(int id)
        {
            return mapper.Map<CollectionDto>(await collectionRepository.FindById(id));
        }

        public Task Delete(CollectionDto collectionDto)
        {
            throw new NotImplementedException();
        }

        public Task<CollectionDto> Rename(CollectionRenameDto renameDto)
        {
            throw new NotImplementedException();
        }
    }
}

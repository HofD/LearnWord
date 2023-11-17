using AutoMapper;
using LearningWords.BL.Abstractions;
using LearningWords.BL.Models.Dto;
using LearningWords.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<IEnumerable<CollectionDto>> GetAll(string userId)
        {
            return (await collectionRepository.GetByUserId(userId)).Select(x => mapper.Map<CollectionDto>(x)).ToList();
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

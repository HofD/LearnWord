using AutoMapper;
using LearnWord.BL.Abstractions;
using LearnWord.BL.Models.Dto;
using LearnWord.DAL.Models;
using LearnWord.DAL.Repositories;

namespace LearnWord.BL.Services
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
        public async Task<CollectionDto> Add(CollectionCreateDto createDto)
        {
            return mapper.Map<CollectionDto>(await collectionRepository.Add(mapper.Map<Collection>(createDto)));
        }

        public async Task<CollectionDto> Get(int id)
        {
            return mapper.Map<CollectionDto>(await collectionRepository.FindById(id));
        }

        public async Task<CollectionListDto> GetList(List<int> ids)
        {
            var result = new CollectionListDto();

            var values = await collectionRepository.FindByIds(ids);

            foreach ( var value in values )
            {
                result.Collections.Add(
                    new CollectionListEntityDto() 
                    { 
                        Id = value.Id, 
                        Name = value.Name, 
                        CardsCount = value.Cards.Count 
                    });
            }

            return result;
        }

        public async Task Remove(int id)
        {
            await collectionRepository.Remove(id);
        }

        public async Task<CollectionDto> Rename(int id, CollectionRenameDto renameDto)
        {
            return mapper.Map<CollectionDto>(await collectionRepository.Rename(id, renameDto.Name));
        }
    }
}

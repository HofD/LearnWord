using AutoMapper;
using LearnWord.BL.Models.Dto;
using LearnWord.DAL.Models;

namespace LearnWord.BL.MappingProfiles
{
    public class CollectionMappingProfile : Profile
    {
        public CollectionMappingProfile() 
        {
            CreateMap<Collection, CollectionDto>();
            CreateMap<CollectionCreateDto, Collection>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(o => DateTime.UtcNow));
        }
    }
}

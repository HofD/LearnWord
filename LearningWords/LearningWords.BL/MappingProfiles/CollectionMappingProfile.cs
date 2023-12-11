using AutoMapper;
using LearningWords.BL.Models.Dto;
using LearningWords.DAL.Models;

namespace LearningWords.BL.MappingProfiles
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

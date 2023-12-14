using AutoMapper;
using LearningWords.BL.Models.Dto;
using LearningWords.DAL.Models;

namespace LearningWords.BL.MappingProfiles
{
    public class CardMappingProfile : Profile
    {
        public CardMappingProfile() 
        {
            CreateMap<Card, CardDto>();
            CreateMap<CardCreateDto, Card>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(o => DateTime.UtcNow));
        }
    }
}

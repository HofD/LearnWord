using AutoMapper;
using LearnWord.BL.Models.Dto;
using LearnWord.DAL.Models;

namespace LearnWord.BL.MappingProfiles
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

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
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(o => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.DueDate, opt => opt.MapFrom(o => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.IntervalDays, opt => opt.MapFrom(o => 0))
                .ForMember(dest => dest.EaseFactor, opt => opt.MapFrom(o => 2.5m))
                .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(o => 0))
                .ForMember(dest => dest.LastReviewedAt, opt => opt.Ignore());
        }
    }
}

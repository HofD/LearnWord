using AutoMapper;
using LearningWords.BL.Models.Dto;
using LearningWords.DAL.Models;

namespace LearningWords.BL.MappingProfiles
{
    public class WordMappingProfile : Profile
    {
        public WordMappingProfile() 
        {
            CreateMap<Word, WordDto>();
            CreateMap<WordDto, Word>();
            CreateMap<WordCreateDto, Word>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(o => DateTime.UtcNow));
            CreateMap<WordUpdateDto, Word>()
                .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(o => DateTime.UtcNow));
        }
    }
}

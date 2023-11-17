using AutoMapper;
using LearningWords.BL.Models;
using LearningWords.BL.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearningWords.BL.MappingProfiles
{
    public class CollectionMappingProfile : Profile
    {
        public CollectionMappingProfile() 
        {
            CreateMap<DAL.Models.Collection, CollectionDto>();
        }
    }
}

using LearningWords.BL.Abstractions;
using LearningWords.BL.Models.Dto;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearningWords.BL.Services
{
    public class CollectionService : ICollectionService
    {
        public Task<CollectionDto> Create(CollectionCreateDto createDto)
        {
            throw new NotImplementedException();
        }

        public Task<CollectionDto> Get(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CollectionDto>> GetAll(string userId)
        {
            throw new NotImplementedException();
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

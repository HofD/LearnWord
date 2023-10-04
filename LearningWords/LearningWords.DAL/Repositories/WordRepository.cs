using Common.Data;
using LearningWords.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LearningWords.DAL.Repositories
{
    internal class WordRepository : RepositoryBase
    {
        public WordRepository(WordsDbContext dbContext) : base(dbContext)
        {
        }

        public async Task Add(Word word)
        {
            dbContext.Words.Add(word);
            await SaveChangesAsync();
        }

        public async Task Delete(Word word)
        {
            dbContext.Words.Remove(word);
            await SaveChangesAsync();
        }

        public async Task Update(Word word)
        {
            dbContext.Words.Update(word);
            await SaveChangesAsync();
        }

        public async Task<Word> FindById(int id)
        {
            return await GetQueryable().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Word>> GetAll()
        {
            return await GetQueryable().ToListAsync();
        }

        private IQueryable<Word> GetQueryable()
        {
            var queryable = dbContext.Words.AsQueryable();
            queryable = queryable.Where(x => x.DeletedAt == null);
            return queryable;
        }
    }
}

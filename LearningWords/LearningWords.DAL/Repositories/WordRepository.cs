using LearningWords.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LearningWords.DAL.Repositories
{
    public class WordRepository : RepositoryBase
    {
        public WordRepository(WordsDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Word> Add(Word word)
        {
            dbContext.Words.Add(word);

            await SaveChangesAsync();

            return await FindById(word.Id);
        }

        public async Task Remove(int id)
        {
            var word = await FindById(id);

            if (word == null)
            {
                throw new Exception($"Word {id} not found.");
            }

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

        private IQueryable<Word> GetQueryable()
        {
            var queryable = dbContext.Words.AsQueryable();
            queryable = queryable.Where(x => x.DeletedAt == null);
            return queryable;
        }
    }
}

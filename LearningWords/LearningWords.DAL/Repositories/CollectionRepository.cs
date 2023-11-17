using Common.Data;
using LearningWords.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LearningWords.DAL.Repositories
{
    public class CollectionRepository : RepositoryBase
    {
        public CollectionRepository(WordsDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Collection> Add(Collection collection)
        {
            dbContext.Collections.Add(collection);
            await SaveChangesAsync();
            return collection;
        }

        public async Task Delete(Collection collection)
        {
            dbContext.Collections.Remove(collection);
            await SaveChangesAsync();
        }

        public async Task Update(Collection collection)
        {
            dbContext.Collections.Update(collection);
            await SaveChangesAsync();
        }

        public async Task<Collection> FindById(int id, bool include = true)
        {
            return await GetQueryable(include).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Collection>> GetByUserId(string userId, bool include = false)
        {
            return await GetQueryable(include).Where(x => x.UserId == userId).ToListAsync();
        }

        private IQueryable<Collection> GetQueryable(bool include)
        {
            var queryable = dbContext.Collections.AsQueryable();

            if (include)
            {
                queryable = queryable
                    .Include(x => x.Cards);
            }

            queryable = queryable
                .Where(x => x.DeletedAt == null);

            return queryable;
        }
    }
}

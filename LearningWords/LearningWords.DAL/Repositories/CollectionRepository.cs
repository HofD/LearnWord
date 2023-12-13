using LearningWords.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
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

        public async Task Remove(int id)
        {
            var collection = await FindById(id, false);

            if (collection == null)
            {
                throw new Exception($"Collection {id} not found.");
            }

            dbContext.Collections.Remove(collection);
            await SaveChangesAsync();
        }

        public async Task Rename(int id, string name)
        {
            var collection = await FindById(id, false);

            if (collection == null)
            {
                throw new Exception($"Collection {id} not found.");
            }

            collection.Name = name;
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

        private IQueryable<Collection> GetQueryable(bool include)
        {
            var queryable = dbContext.Collections.AsQueryable();

            if (include)
            {
                queryable = queryable
                    .Include(x => x.Cards)
                    .ThenInclude(x => x.Words);
            }

            queryable = queryable
                .Where(x => x.DeletedAt == null);

            return queryable;
        }
    }
}

using LearnWord.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LearnWord.DAL.Repositories
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

        public async Task<Collection> Rename(int id, string name)
        {
            var collection = await FindById(id, false);

            if (collection == null)
            {
                throw new Exception($"Collection {id} not found.");
            }

            collection.Name = name;
            await SaveChangesAsync();

            return collection;
        }

        public async Task<Collection> FindById(int id, bool include = true)
        {
            return await GetQueryable(include).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Collection>> FindByIds(List<int> ids)
        {
            var queryable = dbContext.Collections.AsQueryable();

            queryable = queryable
                    .Include(x => x.Cards);

            queryable = queryable
                .Where(x => ids.Contains(x.Id));

            return await queryable.AsNoTracking().ToListAsync();
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

using LearnWord.BL.Models.Errors;
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
            var collection = await GetQueryable(true).FirstOrDefaultAsync(x => x.Id == id);

            if (collection == null)
            {
                throw new NotFoundException($"Collection {id} not found.", "collection_not_found");
            }

            var deletedAt = DateTimeOffset.UtcNow;
            collection.DeletedAt = deletedAt;
            collection.ModifiedAt = deletedAt;

            if (collection.Cards != null)
            {
                foreach (var card in collection.Cards)
                {
                    card.DeletedAt = deletedAt;
                    card.ModifiedAt = deletedAt;

                    foreach (var word in card.Words ?? Enumerable.Empty<Word>())
                    {
                        word.DeletedAt = deletedAt;
                        word.ModifiedAt = deletedAt;
                    }
                }
            }

            await SaveChangesAsync();
        }

        public async Task<Collection> Rename(int id, string name)
        {
            var collection = await FindById(id, false);

            if (collection == null)
            {
                throw new NotFoundException($"Collection {id} not found.", "collection_not_found");
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

            return queryable;
        }
    }
}

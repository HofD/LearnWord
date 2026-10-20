using LearnWord.Identity.DAL.Context;
using LearnWord.Identity.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnWord.Identity.DAL.Repositories
{
    public class CollectionIdentityRepository
    {
        private readonly CollectionIdentityDbContext dbContext;

        public CollectionIdentityRepository(CollectionIdentityDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<CollectionIdentity> Add(CollectionIdentity collectionIdentity)
        {
            dbContext.CollectionIdentities.Add(collectionIdentity);
            await dbContext.SaveChangesAsync();
            return collectionIdentity;
        }

        public async Task<CollectionIdentity?> Get(int collectionId, string userId)
        {
            var queryable = dbContext.CollectionIdentities.AsQueryable();

            queryable = queryable.Where(x => x.UserId == userId && x.CollectionId == collectionId);

            return await queryable.SingleOrDefaultAsync();
        }

        public async Task<List<CollectionIdentity>> GetAll(string userId)
        {
            var queryable = dbContext.CollectionIdentities.AsQueryable();

            queryable = queryable.Where(x => x.UserId == userId);

            return await queryable.ToListAsync();
        }

        public async Task<CollectionIdentity?> GetOwner(int collectionId)
        {
            return await dbContext.CollectionIdentities.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CollectionId == collectionId);
        }

        public async Task<Dictionary<int, string>> GetOwners(IEnumerable<int> collectionIds)
        {
            var ids = collectionIds.ToArray();
            var links = await dbContext.CollectionIdentities.AsNoTracking()
                .Where(x => ids.Contains(x.CollectionId)).ToListAsync();
            return links.GroupBy(x => x.CollectionId).ToDictionary(x => x.Key, x => x.First().UserId);
        }

        public async Task LinkFork(int collectionId, IEnumerable<int> cardIds, string userId)
        {
            dbContext.CollectionIdentities.Add(new CollectionIdentity { CollectionId = collectionId, UserId = userId });
            dbContext.CardIdentities.AddRange(cardIds.Select(id => new CardIdentity { CardId = id, UserId = userId }));
            // EF SaveChanges wraps all identity rows in a single transaction.
            await dbContext.SaveChangesAsync();
        }

        public async Task Remove(int collectionId, string userId)
        {
            var queryable = dbContext.CollectionIdentities.AsQueryable();

            queryable = queryable.Where(x => x.UserId == userId && x.CollectionId == collectionId);

            var result = await queryable.SingleAsync();

            dbContext.CollectionIdentities.Remove(result);
            
            await dbContext.SaveChangesAsync();
        }
    }
}

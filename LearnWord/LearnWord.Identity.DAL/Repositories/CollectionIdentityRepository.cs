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

using LearnWord.Identity.DAL.Context;
using LearnWord.Identity.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnWord.Identity.DAL.Repositories
{
    public class CardIdentityRepository
    {
        private readonly CollectionIdentityDbContext dbContext;

        public CardIdentityRepository(CollectionIdentityDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<CardIdentity> Add(CardIdentity cardIdentity)
        {
            dbContext.CardIdentities.Add(cardIdentity);
            await dbContext.SaveChangesAsync();
            return cardIdentity;
        }

        public async Task<CardIdentity?> Get(int cardId, string userId)
        {
            var queryable = dbContext.CardIdentities.AsQueryable();

            queryable = queryable.Where(x => x.UserId == userId && x.CardId == cardId);

            return await queryable.SingleOrDefaultAsync();
        }

        public async Task<List<CardIdentity>> GetAll(string userId)
        {
            var queryable = dbContext.CardIdentities.AsQueryable();

            queryable = queryable.Where(x => x.UserId == userId);

            return await queryable.ToListAsync();
        }

        public async Task Remove(int cardId, string userId)
        {
            var queryable = dbContext.CardIdentities.AsQueryable();

            queryable = queryable.Where(x => x.UserId == userId && x.CardId == cardId);

            var result = await queryable.SingleAsync();

            dbContext.CardIdentities.Remove(result);

            await dbContext.SaveChangesAsync();
        }
    }
}

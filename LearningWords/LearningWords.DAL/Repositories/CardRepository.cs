using LearningWords.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LearningWords.DAL.Repositories
{
    public class CardRepository : RepositoryBase
    {
        public CardRepository(WordsDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Card> Add(Card card)
        {
            dbContext.Cards.Add(card);
            await SaveChangesAsync();
            return await FindById(card.Id);
        }

        public async Task Remove(Card card)
        {
            dbContext.Cards.Remove(card);
            await SaveChangesAsync();
        }

        public async Task Update(Card card)
        {
            dbContext.Cards.Update(card);
            await SaveChangesAsync();
        }

        public async Task<Card> FindById(int id, bool include = true)
        {
            return await GetQueryable(include).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Card>> GetByCollectionId(int collectionId, bool include = false)
        {
            return await GetQueryable(include).Where(x => x.CollectionId == collectionId).ToListAsync();
        }

        public async Task<List<Card>> GetAll()
        {
            return await GetQueryable(false).ToListAsync();
        }

        private IQueryable<Card> GetQueryable(bool include)
        {
            var queryable = dbContext.Cards.AsQueryable();

            if (include)
            {
                queryable = queryable
                    .Include(x => x.Words);
            }

            queryable = queryable
                .Where(x => x.DeletedAt == null);

            return queryable;
        }
    }
}

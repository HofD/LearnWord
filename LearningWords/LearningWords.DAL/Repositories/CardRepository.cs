using LearningWords.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
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

        public async Task Remove(int id)
        {
            var card = await FindById(id, false);

            if (card == null)
            {
                throw new Exception($"Card {id} not found.");
            }

            dbContext.Cards.Remove(card);
            await SaveChangesAsync();
        }

        public async Task<Card> FindById(int id, bool include = true)
        {
            return await GetQueryable(include).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Card> ResetCard(int id)
        {
            var card = await FindById(id, false);

            card.ModifiedAt = DateTime.UtcNow;
            card.Learnt = false;
            card.LearntAt = null;
            card.ShowedAt = null;

            await SaveChangesAsync();

            return card;
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

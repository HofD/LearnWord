using LearnWord.BL.Models.Errors;
using LearnWord.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LearnWord.DAL.Repositories
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

        public async Task Remove(int cardId, int id)
        {
            var word = await FindById(id);

            if (word == null)
            {
                throw new NotFoundException($"Word {id} not found.", "word_not_found");
            }

            if (word.CardId != cardId)
            {
                throw new BadRequestException($"Wrong card id for word {word.Id}.", "word_card_mismatch");
            }

            dbContext.Words.Remove(word);

            await SaveChangesAsync();
        }

        public async Task<Word> Update(Word updatedWord)
        {
            var word = await FindById(updatedWord.Id);

            if (word == null)
            {
                throw new NotFoundException($"Word {updatedWord.Id} not found.", "word_not_found");
            }

            if (word.CardId != updatedWord.CardId)
            {
                throw new BadRequestException($"Wrong card id for word {word.Id}.", "word_card_mismatch");
            }

            word.Value = updatedWord.Value;
            word.Transcription = updatedWord.Transcription;
            word.Translation = updatedWord.Translation;
            word.ModifiedAt = updatedWord.ModifiedAt;

            dbContext.Words.Update(word);

            await SaveChangesAsync();

            return word;
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

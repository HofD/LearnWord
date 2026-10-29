using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace LearnWord.DAL.Repositories;

public class LearningRepository : RepositoryBase
{
    public WordsDbContext Context => dbContext;
    public LearningRepository(WordsDbContext dbContext) : base(dbContext) { }

    public async Task<IDbContextTransaction> BeginMutation(int? cardId = null, Guid? sessionId = null)
    {
        var tx = await dbContext.Database.BeginTransactionAsync();
        try
        {
            // All review writers lock session then card. This serializes progress and prevents lost SRS updates.
            if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                if (sessionId.HasValue)
                    await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM words.\"LearningSessions\" WHERE \"Id\" = {sessionId.Value} FOR UPDATE");
                if (cardId.HasValue)
                    await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM words.\"Cards\" WHERE \"Id\" = {cardId.Value} FOR UPDATE");
            }
            return tx;
        }
        catch { await tx.DisposeAsync(); throw; }
    }
}

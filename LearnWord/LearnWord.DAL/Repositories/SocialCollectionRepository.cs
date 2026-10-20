using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnWord.DAL.Repositories;

public class SocialCollectionRepository : RepositoryBase
{
    public SocialCollectionRepository(WordsDbContext dbContext) : base(dbContext) { }

    public async Task<PagedResult<SocialCollectionRecord>> Explore(int page, int pageSize, string search)
    {
        var query = dbContext.Collections.AsNoTracking()
            .Where(c => c.Visibility == CollectionVisibility.Public);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(c => c.Name.ToLower().Contains(term));
        }
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(c => c.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new SocialCollectionRecord
            {
                CollectionId = c.Id, PublicId = c.PublicId, Visibility = c.Visibility,
                Name = c.Name, CardsCount = c.Cards.Count()
            }).ToListAsync();
        return new() { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<SocialCollectionPage> Get(Guid publicId, int page, int pageSize, string? search = null)
    {
        // Visibility, count and the requested card page come from one coherent snapshot.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var source = await dbContext.Collections.AsNoTracking().Where(c => c.PublicId == publicId)
            .Select(c => new { c.Id, c.PublicId, c.Name, c.Visibility, CardsCount = c.Cards.Count() })
            .SingleOrDefaultAsync();
        if (source == null) throw Missing();
        var matchingCards = dbContext.Cards.AsNoTracking().Where(c => c.CollectionId == source.Id);
        var totalCount = source.CardsCount;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            matchingCards = matchingCards.Where(c => c.Words.Any(w =>
                (w.Value != null && w.Value.ToLower().Contains(term)) ||
                (w.Translation != null && w.Translation.ToLower().Contains(term))));
            totalCount = await matchingCards.CountAsync();
        }
        var cards = await matchingCards
            .OrderBy(c => c.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new PublicCardDto
            {
                Words = c.Words.OrderBy(w => w.Id).Select(w => new PublicWordDto
                { Value = w.Value, Transcription = w.Transcription, Translation = w.Translation }).ToList()
            }).ToListAsync();
        var result = new SocialCollectionPage
        {
            CollectionId = source.Id, Visibility = source.Visibility,
            Details = new PublicCollectionDetails
            {
                PublicId = source.PublicId, Name = source.Name, CardsCount = source.CardsCount,
                Cards = cards, Page = page, PageSize = pageSize, TotalCount = totalCount
            }
        };
        await transaction.CommitAsync();
        return result;
    }

    public async Task<Collection> SetVisibility(int id, CollectionVisibility visibility)
    {
        var collection = await dbContext.Collections.Include(c => c.Cards).ThenInclude(c => c.Words)
            .SingleOrDefaultAsync(c => c.Id == id);
        if (collection == null) throw Missing();
        collection.Visibility = visibility;
        collection.ModifiedAt = DateTimeOffset.UtcNow;
        await SaveChangesAsync();
        return collection;
    }

    public async Task<Collection> Fork(Guid publicId)
    {
        // One snapshot for source/access/content and one atomic write for the full clone.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var source = await dbContext.Collections.AsNoTracking().Include(c => c.Cards).ThenInclude(c => c.Words)
            .SingleOrDefaultAsync(c => c.PublicId == publicId &&
                (c.Visibility == CollectionVisibility.Public || c.Visibility == CollectionVisibility.Unlisted));
        if (source == null) throw Missing();
        var now = DateTimeOffset.UtcNow;
        var clone = new Collection
        {
            Name = source.Name, Visibility = CollectionVisibility.Private, PublicId = Guid.NewGuid(),
            ForkedFromCollectionId = source.Id, CreatedAt = now,
            Cards = source.Cards.OrderBy(c => c.Id).Select(c => new Card
            {
                CreatedAt = now, DueDate = now, EaseFactor = 2.5m, IntervalDays = 0,
                ReviewCount = 0, Learnt = false, LastReviewedAt = null,
                Words = c.Words.OrderBy(w => w.Id).Select(w => new Word
                {
                    Value = w.Value, Transcription = w.Transcription, Translation = w.Translation,
                    CreatedAt = now
                }).ToList()
            }).ToList()
        };
        dbContext.Collections.Add(clone);
        await SaveChangesAsync();
        await transaction.CommitAsync();
        return clone;
    }

    private static NotFoundException Missing() => new("Collection not found.", "collection_not_found");
}

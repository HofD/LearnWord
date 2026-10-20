using LearnWord.BL.Mapping;
using LearnWord.BL.Models.Errors;
using LearnWord.BL.Services;
using LearnWord.DAL;
using LearnWord.DAL.Models;
using LearnWord.DAL.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnWord.BL.Tests;

public class SocialCollectionServiceTests
{
    [Fact]
    public void Visibility_UsesStableNumericContract()
    {
        Assert.Equal(0, (int)CollectionVisibility.Private);
        Assert.Equal(1, (int)CollectionVisibility.Unlisted);
        Assert.Equal(2, (int)CollectionVisibility.Public);
    }

    [Fact]
    public async Task Explore_FiltersVisibilityDeletedRowsAndNameBeforePagination()
    {
        await using var db = await Database.Create();
        await db.Seed("English private", CollectionVisibility.Private);
        await db.Seed("English unlisted", CollectionVisibility.Unlisted);
        await db.Seed("French public", CollectionVisibility.Public);
        var first = await db.Seed("English public one", CollectionVisibility.Public);
        var second = await db.Seed("ENGLISH public two", CollectionVisibility.Public);
        await db.Seed("English deleted", CollectionVisibility.Public, deleted: true);

        var page = await db.Service.Explore(2, 1, " english ");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(first.PublicId, Assert.Single(page.Items).PublicId);
        Assert.NotEqual(second.PublicId, page.Items[0].PublicId);
        Assert.Equal(1, page.Items[0].CardsCount);
        Assert.Empty((await db.Service.Explore(3, 1, "english")).Items);
    }

    [Fact]
    public async Task SharedContent_PaginatesActiveCardsAndWordsOnly()
    {
        await using var db = await Database.Create();
        var source = await db.Seed("Shared", CollectionVisibility.Public);
        var extra = new Card { CollectionId = source.Id, Words = [Word("second")] };
        db.Context.Cards.Add(extra);
        db.Context.Cards.Add(new Card { CollectionId = source.Id, DeletedAt = DateTimeOffset.UtcNow, Words = [] });
        source.Cards[0].Words.Add(new Word { Value = "hidden", DeletedAt = DateTimeOffset.UtcNow });
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        var first = await db.Service.Get(source.PublicId, 1, 1);
        var second = await db.Service.Get(source.PublicId, 2, 1);

        Assert.Equal(2, first.Details.TotalCount);
        Assert.Equal(2, first.Details.CardsCount);
        Assert.Equal("cat", Assert.Single(Assert.Single(first.Details.Cards).Words).Value);
        Assert.Equal("second", Assert.Single(Assert.Single(second.Details.Cards).Words).Value);
        Assert.Equal(2, second.Details.Page);
    }

    [Fact]
    public async Task VisibilityChanges_PreservePublicIdAndImmediatelyAffectExploreAndFork()
    {
        await using var db = await Database.Create();
        var source = await db.Seed("Shared", CollectionVisibility.Public);

        var unlisted = await db.Service.SetVisibility(source.Id, CollectionVisibility.Unlisted);
        Assert.Equal(source.PublicId, unlisted.PublicId);
        Assert.Empty((await db.Service.Explore(1, 20, null)).Items);
        Assert.Equal(source.PublicId, (await db.Service.Get(source.PublicId, 1, 20)).Details.PublicId);
        var clone = await db.Service.Fork(source.PublicId);
        Assert.Equal(source.Id, clone.ForkedFromCollectionId);

        await db.Service.SetVisibility(source.Id, CollectionVisibility.Private);
        await Assert.ThrowsAsync<NotFoundException>(() => db.Service.Fork(source.PublicId));
    }

    [Fact]
    public async Task Fork_DeepCopiesActiveContentWithFreshSrs_AndSurvivesSourceDeletion()
    {
        await using var db = await Database.Create();
        var source = await db.Seed("Source", CollectionVisibility.Public);
        var originalCard = source.Cards[0];
        originalCard.Learnt = true;
        originalCard.LearntAt = DateTimeOffset.UtcNow.AddDays(-2);
        originalCard.ShowedAt = DateTimeOffset.UtcNow.AddDays(-1);
        originalCard.ReviewCount = 8;
        originalCard.IntervalDays = 30;
        originalCard.EaseFactor = 3.1m;
        originalCard.LastReviewedAt = DateTimeOffset.UtcNow.AddDays(-1);
        originalCard.DueDate = DateTimeOffset.UtcNow.AddDays(30);
        originalCard.Words.Add(new Word { Value = "hidden", DeletedAt = DateTimeOffset.UtcNow });
        source.Cards.Add(new Card { DeletedAt = DateTimeOffset.UtcNow, Words = [] });
        await db.Context.SaveChangesAsync();
        var originalWordId = originalCard.Words[0].Id;
        db.Context.ChangeTracker.Clear();
        var before = DateTimeOffset.UtcNow;

        var result = await db.Service.Fork(source.PublicId);
        var after = DateTimeOffset.UtcNow;
        var clone = await db.Context.Collections.Include(c => c.Cards).ThenInclude(c => c.Words)
            .SingleAsync(c => c.Id == result.Id);
        var card = Assert.Single(clone.Cards);
        var word = Assert.Single(card.Words);

        Assert.NotEqual(source.Id, clone.Id);
        Assert.NotEqual(source.PublicId, clone.PublicId);
        Assert.Equal(CollectionVisibility.Private, clone.Visibility);
        Assert.Equal(source.Id, clone.ForkedFromCollectionId);
        Assert.NotEqual(originalCard.Id, card.Id);
        Assert.NotEqual(originalWordId, word.Id);
        Assert.Equal(("cat", "[cat]", "кот"), (word.Value, word.Transcription, word.Translation));
        Assert.False(card.Learnt);
        Assert.Null(card.LearntAt);
        Assert.Null(card.ShowedAt);
        Assert.Null(card.LastReviewedAt);
        Assert.Equal(0, card.ReviewCount);
        Assert.Equal(0, card.IntervalDays);
        Assert.Equal(2.5m, card.EaseFactor);
        Assert.InRange(card.DueDate, before, after);

        word.Value = "independent";
        await db.Context.SaveChangesAsync();
        Assert.Equal("cat", (await db.Context.Words.AsNoTracking().SingleAsync(w => w.Id == originalWordId)).Value);
        await new CollectionRepository(db.Context).Remove(source.Id);
        db.Context.ChangeTracker.Clear();
        var surviving = await db.Context.Collections.Include(c => c.Cards).ThenInclude(c => c.Words)
            .SingleAsync(c => c.Id == clone.Id);
        Assert.Equal(source.Id, surviving.ForkedFromCollectionId);
        Assert.Equal("independent", Assert.Single(Assert.Single(surviving.Cards).Words).Value);
        await Assert.ThrowsAsync<NotFoundException>(() => db.Service.Get(source.PublicId, 1, 20));
    }

    [Theory]
    [InlineData("needle")]
    [InlineData("  NeEdLe  ")]
    public async Task SharedSearch_FiltersWholeCollectionBeforePaging_ReturnsAllWordsAndCountsCardsOnce(string search)
    {
        await using var db = await Database.Create();
        var source = await db.Seed("Shared", CollectionVisibility.Public);
        for (var i = 1; i <= 60; i++)
        {
            var card = new Card { CollectionId = source.Id, Words = [Word($"item-{i}")] };
            if (i == 55) card.Words = [Word("Needle value"), Word("NEEDLE extra"), Word("supporting")];
            if (i == 57) card.Words = [new Word { Value = "ordinary", Translation = "Needle translation" }, Word("companion")];
            if (i == 58) card.Words = [new Word { Value = "needle hidden", DeletedAt = DateTimeOffset.UtcNow }, Word("unrelated")];
            if (i == 59) { card.Words = [Word("needle deleted card")]; card.DeletedAt = DateTimeOffset.UtcNow; }
            if (i == 60) card.Words[0].Transcription = "needle transcription only";
            db.Context.Cards.Add(card);
        }
        var foreign = await db.Seed("Foreign matching collection", CollectionVisibility.Private);
        foreign.Cards[0].Words[0].Value = "needle foreign";
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        var first = (await db.Service.Get(source.PublicId, 1, 1, search)).Details;
        var second = (await db.Service.Get(source.PublicId, 2, 1, search)).Details;
        var empty = (await db.Service.Get(source.PublicId, 3, 1, search)).Details;

        Assert.Equal(60, first.CardsCount);
        Assert.Equal(2, first.TotalCount);
        Assert.Equal(new[] { "Needle value", "NEEDLE extra", "supporting" }, Assert.Single(first.Cards).Words.Select(w => w.Value));
        Assert.Equal(new[] { "ordinary", "companion" }, Assert.Single(second.Cards).Words.Select(w => w.Value));
        Assert.Equal(2, second.TotalCount);
        Assert.Equal(2, second.Page);
        Assert.Equal(1, second.PageSize);
        Assert.Empty(empty.Cards);
        Assert.Equal(2, empty.TotalCount);

        var translation = (await db.Service.Get(source.PublicId, 1, 20, "  NEEDLE TRANSLATION ")).Details;
        Assert.Equal(1, translation.TotalCount);
        Assert.Equal("ordinary", Assert.Single(translation.Cards).Words[0].Value);
        var missing = (await db.Service.Get(source.PublicId, 1, 20, "does not exist")).Details;
        Assert.Empty(missing.Cards);
        Assert.Equal(0, missing.TotalCount);
        Assert.Equal(60, missing.CardsCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \t ")]
    public async Task SharedSearch_EmptyOrWhitespacePreservesUnfilteredResults(string? search)
    {
        await using var db = await Database.Create();
        var source = await db.Seed("Shared", CollectionVisibility.Unlisted);
        db.Context.Cards.Add(new Card { CollectionId = source.Id, Words = [Word("dog")] });
        await db.Context.SaveChangesAsync();

        var result = (await db.Service.Get(source.PublicId, 1, 20, search)).Details;

        Assert.Equal(2, result.CardsCount);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { "cat", "dog" }, result.Cards.Select(c => Assert.Single(c.Words).Value));
    }

    [Fact]
    public async Task SharedSearch_DoesNotRestrictSubsequentForkContent_AndDeletedSourceRemainsMissing()
    {
        await using var db = await Database.Create();
        var source = await db.Seed("Shared", CollectionVisibility.Unlisted);
        db.Context.Cards.Add(new Card { CollectionId = source.Id, Words = [Word("dog")] });
        await db.Context.SaveChangesAsync();

        var filtered = await db.Service.Get(source.PublicId, 1, 20, "cat");
        var clone = await db.Service.Fork(source.PublicId);

        Assert.Single(filtered.Details.Cards);
        Assert.Equal(2, clone.Cards.Count);
        await new CollectionRepository(db.Context).Remove(source.Id);
        await Assert.ThrowsAsync<NotFoundException>(() => db.Service.Get(source.PublicId, 1, 20, "dog"));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    [InlineData(int.MaxValue, 50)]
    public async Task InvalidPagination_ThrowsBadRequest(int page, int size)
    {
        await using var db = await Database.Create();
        await Assert.ThrowsAsync<BadRequestException>(() => db.Service.Explore(page, size, null));
        await Assert.ThrowsAsync<BadRequestException>(() => db.Service.Get(Guid.NewGuid(), page, size));
    }

    [Fact]
    public async Task UnknownVisibility_IsBadRequestAndDoesNotModifyCollection()
    {
        await using var db = await Database.Create();
        var source = await db.Seed("Private", CollectionVisibility.Private);
        await Assert.ThrowsAsync<BadRequestException>(() => db.Service.SetVisibility(source.Id, (CollectionVisibility)99));
        Assert.Equal(CollectionVisibility.Private, (await db.Context.Collections.SingleAsync()).Visibility);
    }

    private static Word Word(string value) => new() { Value = value, Transcription = $"[{value}]", Translation = "кот" };

    private sealed class Database : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public Context Context { get; }
        public SocialCollectionService Service { get; }
        private Database(SqliteConnection connection, Context context)
        {
            this.connection = connection;
            Context = context;
            Service = new SocialCollectionService(new SocialCollectionRepository(context), new ObjectMapper());
        }
        public static async Task<Database> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new Context(connection);
            await context.Database.EnsureCreatedAsync();
            return new Database(connection, context);
        }
        public async Task<Collection> Seed(string name, CollectionVisibility visibility, bool deleted = false)
        {
            var collection = new Collection
            {
                Name = name, Visibility = visibility, CreatedAt = DateTimeOffset.UtcNow,
                DeletedAt = deleted ? DateTimeOffset.UtcNow : null,
                Cards = [new Card { Words = [Word("cat")] }]
            };
            Context.Collections.Add(collection);
            await Context.SaveChangesAsync();
            return collection;
        }
        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
    private sealed class Context(SqliteConnection connection) : WordsDbContext(null!)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(connection);
    }
}

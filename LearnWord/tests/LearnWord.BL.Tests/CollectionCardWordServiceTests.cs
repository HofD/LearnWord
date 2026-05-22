using LearnWord.BL.Mapping;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.BL.Services;
using LearnWord.DAL;
using LearnWord.DAL.Models;
using LearnWord.DAL.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnWord.BL.Tests;

public class CollectionCardWordServiceTests
{
    [Fact]
    public async Task CollectionService_AddGetRenameListAndRemove_CoversCollectionLifecycle()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var service = fixture.CreateCollectionService();

        var created = await service.Add(new CollectionCreateDto { Name = "English" });
        var fetched = await service.Get(created.Id);
        var renamed = await service.Rename(created.Id, new CollectionRenameDto { Name = "Spanish" });
        var list = await service.GetList([created.Id]);
        await service.Remove(created.Id);

        Assert.Equal("English", created.Name);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Spanish", renamed.Name);
        var listItem = Assert.Single(list.Collections);
        Assert.Equal(created.Id, listItem.Id);
        Assert.Equal("Spanish", listItem.Name);
        Assert.Equal(0, listItem.CardsCount);
        Assert.Null(await fixture.Context.Collections!.SingleOrDefaultAsync(x => x.Id == created.Id));
        Assert.NotNull(await fixture.Context.Collections!.IgnoreQueryFilters().SingleAsync(x => x.Id == created.Id));
    }

    [Fact]
    public async Task CollectionService_GetList_CountsOnlyActiveCards()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var collection = await fixture.SeedCollection("Review");
        await fixture.SeedCard(collection.Id);
        await fixture.SeedCard(collection.Id, deletedAt: DateTimeOffset.UtcNow);
        var service = fixture.CreateCollectionService();

        var list = await service.GetList([collection.Id]);

        var listItem = Assert.Single(list.Collections);
        Assert.Equal(1, listItem.CardsCount);
    }

    [Fact]
    public async Task CollectionService_GetReviewCards_ReturnsCardsDueByDueDate()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var collection = await fixture.SeedCollection("Review");
        var overdue = await fixture.SeedCard(collection.Id, dueDate: DateTimeOffset.UtcNow.AddMinutes(-1));
        var dueNow = await fixture.SeedCard(collection.Id, dueDate: DateTimeOffset.UtcNow);
        await fixture.SeedCard(collection.Id, dueDate: DateTimeOffset.UtcNow.AddMinutes(30));
        var service = fixture.CreateCollectionService();

        var cards = await service.GetReviewCards(collection.Id);

        Assert.Equal(
            [overdue.Id, dueNow.Id],
            cards.Select(x => x.Id).Order());
    }

    [Fact]
    public async Task CardService_AddLearnForgetResetAndRemove_CoversCardLifecycle()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var collection = await fixture.SeedCollection("Cards");
        var service = fixture.CreateCardService();

        var created = await service.Add(new CardCreateDto
        {
            CollectionId = collection.Id,
            Words =
            [
                new WordCreateDto { Value = "cat", Transcription = "kat", Translation = "cat-translation" }
            ]
        });
        var learnt = await service.Learn(created.Id);
        var storedLearnt = await fixture.Context.Cards!.AsNoTracking().SingleAsync(x => x.Id == created.Id);
        var forgotten = await service.Forget(created.Id);
        var reset = await service.Reset(created.Id);
        await service.Remove(created.Id);

        Assert.Equal(collection.Id, created.CollectionId);
        Assert.Single(created.Words);
        Assert.True(learnt.Learnt);
        Assert.NotNull(storedLearnt.LearntAt);
        Assert.NotNull(storedLearnt.ShowedAt);
        Assert.False(forgotten.Learnt);
        Assert.False(reset.Learnt);
        var deletedCard = await fixture.Context.Cards!
            .IgnoreQueryFilters()
            .Include(x => x.Words)
            .SingleAsync(x => x.Id == created.Id);
        Assert.NotNull(deletedCard.DeletedAt);
        Assert.All(deletedCard.Words, word => Assert.NotNull(word.DeletedAt));
    }

    [Fact]
    public async Task CardService_MissingCardActionsThrowNotFound()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var service = fixture.CreateCardService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.Learn(404));
        await Assert.ThrowsAsync<NotFoundException>(() => service.Forget(404));
        await Assert.ThrowsAsync<NotFoundException>(() => service.Reset(404));
        await Assert.ThrowsAsync<NotFoundException>(() => service.Remove(404));
    }

    [Fact]
    public async Task WordEditService_AddAndUpdateResetCardReviewState()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var card = await fixture.SeedLearntCardWithWord();
        var service = fixture.CreateWordEditService();

        var added = await service.Add(
            new WordCreateDto { Value = "dog", Transcription = "dog", Translation = "dog-translation" },
            card.Id);
        var afterAdd = await fixture.Context.Cards!.AsNoTracking().SingleAsync(x => x.Id == card.Id);

        var updated = await service.Update(
            card.Id,
            added.Id,
            new WordUpdateDto { Value = "dog-updated", Transcription = "dog-updated", Translation = "updated-translation" });
        var afterUpdate = await fixture.Context.Cards!.AsNoTracking().SingleAsync(x => x.Id == card.Id);

        Assert.NotEqual(0, added.Id);
        Assert.Equal("dog-updated", updated.Value);
        Assert.False(afterAdd.Learnt);
        Assert.Null(afterAdd.LearntAt);
        Assert.Null(afterAdd.ShowedAt);
        Assert.False(afterUpdate.Learnt);
        Assert.Null(afterUpdate.LearntAt);
        Assert.Null(afterUpdate.ShowedAt);
    }

    [Fact]
    public async Task WordEditService_RemoveResetsCardReviewState_WhenCardStillHasActiveWords()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var card = await fixture.SeedLearntCardWithWord();
        var service = fixture.CreateWordEditService();
        var added = await service.Add(
            new WordCreateDto { Value = "dog", Transcription = "dog", Translation = "dog-translation" },
            card.Id);
        await fixture.MarkCardAsLearnt(card.Id);

        await service.Remove(card.Id, added.Id);

        var afterRemove = await fixture.Context.Cards!.AsNoTracking().SingleAsync(x => x.Id == card.Id);
        Assert.False(afterRemove.Learnt);
        Assert.Null(afterRemove.LearntAt);
        Assert.Null(afterRemove.ShowedAt);
        Assert.Null(afterRemove.DeletedAt);
        Assert.NotNull(await fixture.Context.Words!.IgnoreQueryFilters().SingleAsync(x => x.Id == added.Id && x.DeletedAt != null));
        Assert.True(await fixture.Context.Words!.AnyAsync(x => x.CardId == card.Id));
    }

    [Fact]
    public async Task WordEditService_RemoveDeletesCard_WhenDeletedWordWasLastActiveWord()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var card = await fixture.SeedLearntCardWithWord();
        var word = await fixture.Context.Words!.AsNoTracking().SingleAsync(x => x.CardId == card.Id);
        var service = fixture.CreateWordEditService();

        await service.Remove(card.Id, word.Id);

        var deletedCard = await fixture.Context.Cards!
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(x => x.Id == card.Id);
        var deletedWord = await fixture.Context.Words!
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(x => x.Id == word.Id);

        Assert.NotNull(deletedCard.DeletedAt);
        Assert.Equal(deletedCard.DeletedAt, deletedCard.ModifiedAt);
        Assert.NotNull(deletedWord.DeletedAt);
        Assert.Equal(deletedWord.DeletedAt, deletedWord.ModifiedAt);
        Assert.Null(await fixture.Context.Cards!.SingleOrDefaultAsync(x => x.Id == card.Id));
    }

    [Fact]
    public async Task WordService_UpdateOrRemoveWithWrongCardIdThrowsBadRequest()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var firstCard = await fixture.SeedCardWithWord();
        var secondCard = await fixture.SeedCardWithWord();
        var word = await fixture.Context.Words!.AsNoTracking().FirstAsync(x => x.CardId == firstCard.Id);
        var service = fixture.CreateWordService();

        await Assert.ThrowsAsync<BadRequestException>(() => service.Update(
            secondCard.Id,
            word.Id,
            new WordUpdateDto { Value = "wrong", Transcription = "wrong", Translation = "wrong" }));
        await Assert.ThrowsAsync<BadRequestException>(() => service.Remove(secondCard.Id, word.Id));
    }

    [Fact]
    public async Task WordService_UpdateOrRemoveMissingWordThrowsNotFound()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var card = await fixture.SeedCardWithWord();
        var service = fixture.CreateWordService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.Update(
            card.Id,
            404,
            new WordUpdateDto { Value = "missing", Transcription = "missing", Translation = "missing" }));
        await Assert.ThrowsAsync<NotFoundException>(() => service.Remove(card.Id, 404));
    }

    private sealed class TestWordsDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly ObjectMapper mapper;

        private TestWordsDatabase(SqliteConnection connection, TestWordsDbContext context)
        {
            this.connection = connection;
            Context = context;
            mapper = new ObjectMapper();
        }

        public TestWordsDbContext Context { get; }

        public static async Task<TestWordsDatabase> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var context = new TestWordsDbContext(connection);
            await context.Database.EnsureCreatedAsync();

            return new TestWordsDatabase(connection, context);
        }

        public CollectionService CreateCollectionService()
        {
            return new CollectionService(new CollectionRepository(Context), mapper);
        }

        public CardService CreateCardService()
        {
            return new CardService(new CardRepository(Context), mapper, new SpacedRepetitionScheduler());
        }

        public WordService CreateWordService()
        {
            return new WordService(new WordRepository(Context), mapper);
        }

        public WordEditService CreateWordEditService()
        {
            return new WordEditService(CreateCardService(), CreateWordService());
        }

        public async Task<Collection> SeedCollection(string name)
        {
            var collection = new Collection
            {
                Name = name,
                CreatedAt = DateTimeOffset.UtcNow,
                Cards = []
            };

            Context.Collections!.Add(collection);
            await SaveAndClearChanges();
            return collection;
        }

        public async Task<Card> SeedCard(
            int collectionId,
            bool learnt = false,
            DateTimeOffset? showedAt = null,
            DateTimeOffset? dueDate = null,
            DateTimeOffset? deletedAt = null)
        {
            var card = new Card
            {
                CollectionId = collectionId,
                CreatedAt = DateTimeOffset.UtcNow,
                Learnt = learnt,
                LearntAt = learnt ? DateTimeOffset.UtcNow.AddDays(-1) : null,
                ShowedAt = showedAt,
                DueDate = dueDate ?? DateTimeOffset.UtcNow,
                IntervalDays = 0,
                EaseFactor = 2.5m,
                ReviewCount = 0,
                DeletedAt = deletedAt,
                ModifiedAt = deletedAt,
                Words = []
            };

            Context.Cards!.Add(card);
            await SaveAndClearChanges();
            return card;
        }

        public async Task<Card> SeedCardWithWord()
        {
            var collection = await SeedCollection($"collection-{Guid.NewGuid()}");
            var card = await SeedCard(collection.Id);
            Context.Words!.Add(CreateWord(card.Id, "cat"));
            await SaveAndClearChanges();
            return card;
        }

        public async Task<Card> SeedLearntCardWithWord()
        {
            var collection = await SeedCollection($"collection-{Guid.NewGuid()}");
            var card = await SeedCard(collection.Id, learnt: true, showedAt: DateTimeOffset.UtcNow.AddDays(-1));
            Context.Words!.Add(CreateWord(card.Id, "cat"));
            await SaveAndClearChanges();
            return card;
        }

        public async Task MarkCardAsLearnt(int cardId)
        {
            var card = await Context.Cards!.SingleAsync(x => x.Id == cardId);
            card.Learnt = true;
            card.LearntAt = DateTimeOffset.UtcNow;
            card.ShowedAt = DateTimeOffset.UtcNow;
            await SaveAndClearChanges();
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }

        private async Task SaveAndClearChanges()
        {
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        private static Word CreateWord(int cardId, string value)
        {
            return new Word
            {
                CardId = cardId,
                Value = value,
                Transcription = $"[{value}]",
                Translation = $"{value}-translation",
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }

    private sealed class TestWordsDbContext : WordsDbContext
    {
        private readonly SqliteConnection connection;

        public TestWordsDbContext(SqliteConnection connection)
            : base(configuration: null!)
        {
            this.connection = connection;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite(connection);
        }
    }
}

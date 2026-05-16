using LearnWord.DAL.Models;
using LearnWord.DAL.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnWord.DAL.Tests;

public class SoftDeleteRepositoryTests
{
    [Fact]
    public async Task WordRepository_Remove_SetsDeletedAtAndModifiedAt_AndFindByIdHidesWord()
    {
        await using var fixture = await TestDatabase.Create();
        var word = await fixture.SeedWord();
        var repository = new WordRepository(fixture.Context);

        await repository.Remove(word.CardId, word.Id);

        var deletedWord = await fixture.Context.Words!
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == word.Id);

        Assert.NotNull(deletedWord.DeletedAt);
        Assert.Equal(deletedWord.DeletedAt, deletedWord.ModifiedAt);
        Assert.Null(await repository.FindById(word.Id));
    }

    [Fact]
    public async Task WordRepository_HasAnyActiveWords_IgnoresSoftDeletedWords()
    {
        await using var fixture = await TestDatabase.Create();
        var word = await fixture.SeedWord();
        var repository = new WordRepository(fixture.Context);

        await repository.Remove(word.CardId, word.Id);

        Assert.False(await repository.HasAnyActiveWords(word.CardId));
    }

    [Fact]
    public async Task CardRepository_Remove_SoftDeletesCardAndItsWords()
    {
        await using var fixture = await TestDatabase.Create();
        var card = await fixture.SeedCardWithWords();
        var repository = new CardRepository(fixture.Context);

        await repository.Remove(card.Id);

        var deletedCard = await fixture.Context.Cards!
            .IgnoreQueryFilters()
            .Include(x => x.Words)
            .SingleAsync(x => x.Id == card.Id);

        Assert.NotNull(deletedCard.DeletedAt);
        Assert.Equal(deletedCard.DeletedAt, deletedCard.ModifiedAt);
        Assert.All(deletedCard.Words, word =>
        {
            Assert.NotNull(word.DeletedAt);
            Assert.Equal(deletedCard.DeletedAt, word.DeletedAt);
            Assert.Equal(word.DeletedAt, word.ModifiedAt);
        });
        Assert.Null(await repository.FindById(card.Id));
    }

    [Fact]
    public async Task CollectionRepository_Remove_SoftDeletesCollectionCardsAndWords()
    {
        await using var fixture = await TestDatabase.Create();
        var collection = await fixture.SeedCollectionWithCardsAndWords();
        var repository = new CollectionRepository(fixture.Context);

        await repository.Remove(collection.Id);

        var deletedCollection = await fixture.Context.Collections!
            .IgnoreQueryFilters()
            .Include(x => x.Cards)
            .ThenInclude(x => x.Words)
            .SingleAsync(x => x.Id == collection.Id);

        Assert.NotNull(deletedCollection.DeletedAt);
        Assert.Equal(deletedCollection.DeletedAt, deletedCollection.ModifiedAt);
        Assert.All(deletedCollection.Cards, card =>
        {
            Assert.NotNull(card.DeletedAt);
            Assert.Equal(deletedCollection.DeletedAt, card.DeletedAt);
            Assert.Equal(card.DeletedAt, card.ModifiedAt);
            Assert.All(card.Words, word =>
            {
                Assert.NotNull(word.DeletedAt);
                Assert.Equal(deletedCollection.DeletedAt, word.DeletedAt);
                Assert.Equal(word.DeletedAt, word.ModifiedAt);
            });
        });
        Assert.Null(await repository.FindById(collection.Id));
    }

    [Fact]
    public async Task CollectionRepository_FindByIds_DoesNotReturnSoftDeletedCollections()
    {
        await using var fixture = await TestDatabase.Create();
        var activeCollection = await fixture.SeedCollection("active");
        var deletedCollection = await fixture.SeedCollection("deleted", deletedAt: DateTimeOffset.UtcNow);
        var repository = new CollectionRepository(fixture.Context);

        var collections = await repository.FindByIds([activeCollection.Id, deletedCollection.Id]);

        Assert.Collection(
            collections,
            collection => Assert.Equal(activeCollection.Id, collection.Id));
    }

    [Fact]
    public async Task CollectionRepository_GetQueryable_DoesNotReturnSoftDeletedCardsInCollection()
    {
        await using var fixture = await TestDatabase.Create();
        var collection = await fixture.SeedCollectionWithActiveAndDeletedCards();
        var repository = new CollectionRepository(fixture.Context);

        var foundCollection = await repository.FindById(collection.Id);

        var card = Assert.Single(foundCollection.Cards);
        Assert.Null(card.DeletedAt);
    }

    [Fact]
    public async Task CardRepository_GetQueryable_DoesNotReturnSoftDeletedCards()
    {
        await using var fixture = await TestDatabase.Create();
        var deletedCard = await fixture.SeedCard(deletedAt: DateTimeOffset.UtcNow);
        var repository = new CardRepository(fixture.Context);

        var foundCard = await repository.FindById(deletedCard.Id);

        Assert.Null(foundCard);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestDatabase(SqliteConnection connection, TestWordsDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public TestWordsDbContext Context { get; }

        public static async Task<TestDatabase> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var context = new TestWordsDbContext(connection);
            await context.Database.EnsureCreatedAsync();

            return new TestDatabase(connection, context);
        }

        public async Task<Collection> SeedCollection(string name, DateTimeOffset? deletedAt = null)
        {
            var collection = new Collection
            {
                Name = name,
                CreatedAt = DateTimeOffset.UtcNow,
                DeletedAt = deletedAt,
                ModifiedAt = deletedAt
            };

            Context.Collections!.Add(collection);
            await SaveAndClearChanges();
            return collection;
        }

        public async Task<Card> SeedCard(DateTimeOffset? deletedAt = null)
        {
            var collection = await SeedCollection($"collection-{Guid.NewGuid()}");
            var card = new Card
            {
                CollectionId = collection.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                DeletedAt = deletedAt,
                ModifiedAt = deletedAt,
                Words = []
            };

            Context.Cards!.Add(card);
            await SaveAndClearChanges();
            return card;
        }

        public async Task<Word> SeedWord()
        {
            var card = await SeedCard();
            var word = CreateWord(card.Id, "one");

            Context.Words!.Add(word);
            await SaveAndClearChanges();
            return word;
        }

        public async Task<Card> SeedCardWithWords()
        {
            var card = await SeedCard();
            Context.Words!.AddRange(
                CreateWord(card.Id, "one"),
                CreateWord(card.Id, "two"));

            await SaveAndClearChanges();
            return card;
        }

        public async Task<Collection> SeedCollectionWithCardsAndWords()
        {
            var collection = await SeedCollection($"collection-{Guid.NewGuid()}");
            var firstCard = CreateCard(collection.Id);
            var secondCard = CreateCard(collection.Id);

            Context.Cards!.AddRange(firstCard, secondCard);
            await SaveAndClearChanges();

            Context.Words!.AddRange(
                CreateWord(firstCard.Id, "one"),
                CreateWord(firstCard.Id, "two"),
                CreateWord(secondCard.Id, "three"));

            await SaveAndClearChanges();
            return collection;
        }

        public async Task<Collection> SeedCollectionWithActiveAndDeletedCards()
        {
            var collection = await SeedCollection($"collection-{Guid.NewGuid()}");

            Context.Cards!.AddRange(
                CreateCard(collection.Id),
                CreateCard(collection.Id, deletedAt: DateTimeOffset.UtcNow));

            await SaveAndClearChanges();
            return collection;
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

        private static Card CreateCard(int collectionId, DateTimeOffset? deletedAt = null)
        {
            return new Card
            {
                CollectionId = collectionId,
                CreatedAt = DateTimeOffset.UtcNow,
                DeletedAt = deletedAt,
                ModifiedAt = deletedAt,
                Words = []
            };
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

    public sealed class TestWordsDbContext : WordsDbContext
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

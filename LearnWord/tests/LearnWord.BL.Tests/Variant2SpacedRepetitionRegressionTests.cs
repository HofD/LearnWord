using AutoMapper;
using LearnWord.BL.MappingProfiles;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Services;
using LearnWord.DAL;
using LearnWord.DAL.Models;
using LearnWord.DAL.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnWord.BL.Tests;

public class Variant2SpacedRepetitionRegressionTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void CardModel_ExposesVariant2SchedulingFields()
    {
        var cardType = typeof(Card);

        Assert.NotNull(cardType.GetProperty("DueDate"));
        Assert.NotNull(cardType.GetProperty("IntervalDays"));
        Assert.NotNull(cardType.GetProperty("EaseFactor"));
        Assert.NotNull(cardType.GetProperty("ReviewCount"));
        Assert.NotNull(cardType.GetProperty("LastReviewedAt"));
    }

    [Fact]
    public async Task CollectionService_GetReviewCards_ReturnsOnlyCardsDueByDueDate()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var collection = await fixture.SeedCollection("SRS Review");
        var overdue = await fixture.SeedScheduledCard(collection.Id, Now.AddMinutes(-1));
        var dueNow = await fixture.SeedScheduledCard(collection.Id, Now);
        await fixture.SeedScheduledCard(collection.Id, Now.AddMinutes(30));
        var service = fixture.CreateCollectionService();

        var cards = await service.GetReviewCards(collection.Id);

        Assert.Equal(
            [overdue.Id, dueNow.Id],
            cards.Select(x => x.Id).Order());
    }

    [Fact]
    public async Task CardService_ReviewOutcome_UpdatesSchedulingStateForRepresentativeOutcomes()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var collection = await fixture.SeedCollection("SRS Outcomes");
        var againCard = await fixture.SeedScheduledCard(collection.Id, Now.AddDays(-1));
        var hardCard = await fixture.SeedScheduledCard(collection.Id, Now.AddDays(-1));
        var goodCard = await fixture.SeedScheduledCard(collection.Id, Now.AddDays(-1));
        var easyCard = await fixture.SeedScheduledCard(collection.Id, Now.AddDays(-1));
        var service = fixture.CreateCardService();

        var again = await InvokeReview(service, againCard.Id, "Again");
        var hard = await InvokeReview(service, hardCard.Id, "Hard");
        var good = await InvokeReview(service, goodCard.Id, "Good");
        var easy = await InvokeReview(service, easyCard.Id, "Easy");

        AssertReviewMetadataAdvanced(again, expectedIntervalDays: 1, expectedEaseFactor: 2.3);
        AssertReviewMetadataAdvanced(hard, expectedIntervalDays: 2, expectedEaseFactor: 2.35);
        AssertReviewMetadataAdvanced(good, expectedIntervalDays: 1, expectedEaseFactor: 2.5);
        AssertReviewMetadataAdvanced(easy, expectedIntervalDays: 4, expectedEaseFactor: 2.65);
    }

    [Fact]
    public async Task CardService_ReviewOutcome_AgainDoesNotReduceEaseBelowMinimum()
    {
        await using var fixture = await TestWordsDatabase.Create();
        var collection = await fixture.SeedCollection("SRS Min Ease");
        var card = await fixture.SeedScheduledCard(collection.Id, Now.AddDays(-1), easeFactor: 1.3);
        var service = fixture.CreateCardService();

        var reviewed = await InvokeReview(service, card.Id, "Again");

        Assert.Equal(1.3, GetRequiredValue<double>(reviewed, "EaseFactor"), precision: 3);
        Assert.Equal(1, GetRequiredValue<int>(reviewed, "ReviewCount"));
        Assert.True(GetRequiredValue<DateTimeOffset>(reviewed, "DueDate") > Now);
    }

    private static async Task<CardDto> InvokeReview(CardService service, int cardId, string outcome)
    {
        var method = typeof(CardService)
            .GetMethods()
            .SingleOrDefault(method =>
                string.Equals(method.Name, "Review", StringComparison.OrdinalIgnoreCase)
                && method.GetParameters().Length == 2
                && method.GetParameters()[0].ParameterType == typeof(int));

        Assert.NotNull(method);

        var reviewRequest = CreateReviewRequest(method.GetParameters()[1].ParameterType, outcome);
        var task = method.Invoke(service, [cardId, reviewRequest]) as Task<CardDto>;

        Assert.NotNull(task);
        return await task;
    }

    private static object CreateReviewRequest(Type requestType, string outcome)
    {
        if (requestType == typeof(string))
        {
            return outcome;
        }

        if (requestType.IsEnum)
        {
            return Enum.Parse(requestType, outcome);
        }

        var request = Activator.CreateInstance(requestType);
        Assert.NotNull(request);

        var outcomeProperty = requestType.GetProperty("Outcome");
        Assert.NotNull(outcomeProperty);

        var outcomeValue = outcomeProperty.PropertyType.IsEnum
            ? Enum.Parse(outcomeProperty.PropertyType, outcome)
            : outcome;
        outcomeProperty.SetValue(request, outcomeValue);
        return request;
    }

    private static void AssertReviewMetadataAdvanced(
        CardDto reviewed,
        int expectedIntervalDays,
        double expectedEaseFactor)
    {
        Assert.Equal(1, GetRequiredValue<int>(reviewed, "ReviewCount"));
        Assert.Equal(expectedIntervalDays, GetRequiredValue<int>(reviewed, "IntervalDays"));
        Assert.Equal(expectedEaseFactor, GetRequiredValue<double>(reviewed, "EaseFactor"), precision: 3);

        var lastReviewedAt = GetRequiredValue<DateTimeOffset>(reviewed, "LastReviewedAt");
        var dueDate = GetRequiredValue<DateTimeOffset>(reviewed, "DueDate");
        Assert.True(lastReviewedAt >= Now.AddMinutes(-1));
        Assert.True(dueDate > lastReviewedAt);
        Assert.Equal(lastReviewedAt.Date.AddDays(GetRequiredValue<int>(reviewed, "IntervalDays")), dueDate.Date);
    }

    private static TValue GetRequiredValue<TValue>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName);
        Assert.NotNull(property);

        var value = property.GetValue(source);
        Assert.NotNull(value);
        if (value is TValue typedValue)
        {
            return typedValue;
        }

        return (TValue)Convert.ChangeType(value, typeof(TValue));
    }

    private sealed class TestWordsDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly IMapper mapper;

        private TestWordsDatabase(SqliteConnection connection, TestWordsDbContext context)
        {
            this.connection = connection;
            Context = context;
            mapper = new MapperConfiguration(config =>
            {
                config.AddProfile<CollectionMappingProfile>();
                config.AddProfile<CardMappingProfile>();
                config.AddProfile<WordMappingProfile>();
            }).CreateMapper();
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

        public async Task<Collection> SeedCollection(string name)
        {
            var collection = new Collection
            {
                Name = name,
                CreatedAt = Now,
                Cards = []
            };

            Context.Collections!.Add(collection);
            await SaveAndClearChanges();
            return collection;
        }

        public async Task<Card> SeedScheduledCard(int collectionId, DateTimeOffset dueDate, double easeFactor = 2.5)
        {
            var card = new Card
            {
                CollectionId = collectionId,
                CreatedAt = Now,
                Learnt = true,
                LearntAt = Now.AddDays(-2),
                ShowedAt = Now.AddDays(-2),
                Words =
                [
                    new Word
                    {
                        Value = $"word-{Guid.NewGuid():N}",
                        Transcription = "[word]",
                        Translation = "translation",
                        CreatedAt = Now
                    }
                ]
            };

            SetRequiredValue(card, "DueDate", dueDate);
            SetRequiredValue(card, "IntervalDays", 1);
            SetRequiredValue(card, "EaseFactor", easeFactor);
            SetRequiredValue(card, "ReviewCount", 0);
            SetRequiredValue<DateTimeOffset?>(card, "LastReviewedAt", null);

            Context.Cards!.Add(card);
            await SaveAndClearChanges();
            return card;
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

        private static void SetRequiredValue<TValue>(object source, string propertyName, TValue value)
        {
            var property = source.GetType().GetProperty(propertyName);
            Assert.NotNull(property);

            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var convertedValue = value is null ? null : Convert.ChangeType(value, targetType);
            property.SetValue(source, convertedValue);
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

using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.Identity.Abstactions;
using LearnWord.Identity.DAL.Context;
using LearnWord.Identity.DAL.Models;
using LearnWord.Identity.DAL.Repositories;
using LearnWord.Identity.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnWord.Identity.Tests;

public class OwnershipIntegrationTests
{
    private const string CurrentUser = "user-current";
    private const string OtherUser = "user-other";

    [Fact]
    public async Task CollectionService_ForeignCollection_ReturnsNullAndDoesNotCallUpstream()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        await fixture.SeedCollectionLink(10, OtherUser);
        var upstream = new RecordingCollectionsHttpService();
        var service = fixture.CreateCollectionService(upstream);

        var get = await service.Get(10, CurrentUser);
        var rename = await service.Rename(10, new CollectionRenameDto { Name = "Renamed" }, CurrentUser);
        var remove = await service.Remove(10, CurrentUser);

        Assert.Null(get);
        Assert.Null(rename);
        Assert.Null(remove);
        Assert.False(upstream.WasCalled);
    }

    [Fact]
    public async Task CollectionService_ForeignCollectionReview_ThrowsForbidden()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        await fixture.SeedCollectionLink(10, OtherUser);
        var upstream = new RecordingCollectionsHttpService();
        var service = fixture.CreateCollectionService(upstream);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetCardsForReview(10, CurrentUser));

        Assert.Equal("collection_forbidden", exception.ErrorCode);
        Assert.False(upstream.GetCardsForReviewCalled);
    }

    [Fact]
    public async Task CollectionService_ForeignCollectionAiGeneration_ThrowsForbiddenAndDoesNotCallUpstream()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        await fixture.SeedCollectionLink(10, OtherUser);
        var upstream = new RecordingCollectionsHttpService();
        var service = fixture.CreateCollectionService(upstream);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GenerateAiCards(10, new AiCardGenerationRequest { SourceText = "hello", MaxCards = 1 }, CurrentUser));

        Assert.Equal("collection_forbidden", exception.ErrorCode);
        Assert.False(upstream.GenerateAiCardsCalled);
    }

    [Fact]
    public async Task CardService_CreatingCardInForeignCollection_ThrowsForbiddenAndDoesNotCreateCardLink()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        await fixture.SeedCollectionLink(10, OtherUser);
        var upstream = new RecordingCardHttpService();
        var service = fixture.CreateCardService(upstream);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.Add(new CardCreateDto { CollectionId = 10, Words = [] }, CurrentUser));

        Assert.Equal("collection_forbidden", exception.ErrorCode);
        Assert.False(upstream.AddCalled);
        Assert.Empty(await fixture.Context.CardIdentities.ToListAsync());
    }

    [Fact]
    public async Task CardService_ForeignCardActions_ThrowForbiddenAndDoNotCallUpstream()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        await fixture.SeedCardLink(20, OtherUser);
        var upstream = new RecordingCardHttpService();
        var service = fixture.CreateCardService(upstream);

        var review = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.Review(20, new ReviewCardRequest { Outcome = ReviewOutcome.Good.ToString() }, CurrentUser));
        var remove = await Assert.ThrowsAsync<ForbiddenException>(() => service.Remove(20, CurrentUser));

        Assert.Equal("card_forbidden", review.ErrorCode);
        Assert.Equal("card_forbidden", remove.ErrorCode);
        Assert.False(upstream.ReviewCalled);
        Assert.False(upstream.RemoveCalled);
    }

    [Fact]
    public async Task WordService_ForeignCardWordActions_ThrowForbiddenAndDoNotCallUpstream()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        await fixture.SeedCardLink(20, OtherUser);
        var upstream = new RecordingWordHttpService();
        var service = fixture.CreateWordService(upstream);

        var add = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.Add(20, new WordCreateDto { Value = "cat", Transcription = "kat", Translation = "cat-translation" }, CurrentUser));
        var update = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.Update(20, 30, new WordUpdateDto { Value = "cat", Transcription = "kat", Translation = "cat-translation" }, CurrentUser));
        var remove = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.Remove(30, 20, CurrentUser));

        Assert.Equal("card_forbidden", add.ErrorCode);
        Assert.Equal("card_forbidden", update.ErrorCode);
        Assert.Equal("card_forbidden", remove.ErrorCode);
        Assert.False(upstream.WasCalled);
    }

    [Fact]
    public async Task CardService_Add_OwnedCollectionCreatesCardOwnershipLink()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        await fixture.SeedCollectionLink(10, CurrentUser);
        var upstream = new RecordingCardHttpService
        {
            AddResult = new CardDto { Id = 20, CollectionId = 10, Words = [] }
        };
        var service = fixture.CreateCardService(upstream);

        var result = await service.Add(new CardCreateDto { CollectionId = 10, Words = [] }, CurrentUser);

        Assert.Equal(20, result.Id);
        Assert.True(upstream.AddCalled);
        var link = await fixture.Context.CardIdentities.SingleAsync();
        Assert.Equal(20, link.CardId);
        Assert.Equal(CurrentUser, link.UserId);
    }

    private sealed class TestIdentityDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestIdentityDatabase(SqliteConnection connection, CollectionIdentityDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public CollectionIdentityDbContext Context { get; }

        public static async Task<TestIdentityDatabase> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<CollectionIdentityDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new CollectionIdentityDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new TestIdentityDatabase(connection, context);
        }

        public CollectionIdentityService CreateCollectionService(ICollectionsHttpService upstream)
        {
            return new CollectionIdentityService(upstream, new CollectionIdentityRepository(Context));
        }

        public CardIdentityService CreateCardService(ICardHttpService upstream)
        {
            return new CardIdentityService(
                upstream,
                new CardIdentityRepository(Context),
                new CollectionIdentityRepository(Context));
        }

        public WordIdentityService CreateWordService(IWordHttpService upstream)
        {
            return new WordIdentityService(upstream, new CardIdentityRepository(Context));
        }

        public async Task SeedCollectionLink(int collectionId, string userId)
        {
            Context.CollectionIdentities.Add(new CollectionIdentity
            {
                CollectionId = collectionId,
                UserId = userId
            });
            await SaveAndClearChanges();
        }

        public async Task SeedCardLink(int cardId, string userId)
        {
            Context.CardIdentities.Add(new CardIdentity
            {
                CardId = cardId,
                UserId = userId
            });
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
    }

    private sealed class RecordingCollectionsHttpService : ICollectionsHttpService
    {
        public bool WasCalled { get; private set; }
        public bool GetCardsForReviewCalled { get; private set; }
        public bool GenerateAiCardsCalled { get; private set; }

        public Task<CollectionDto> Add(CollectionCreateDto createDto)
        {
            WasCalled = true;
            return Task.FromResult(new CollectionDto { Id = 1, Name = createDto.Name, Cards = [] });
        }

        public Task<CollectionDto> Get(int id)
        {
            WasCalled = true;
            return Task.FromResult(new CollectionDto { Id = id, Name = "Collection", Cards = [] });
        }

        public Task<IEnumerable<CardDto>> GetCardsForReview(int collectionId)
        {
            WasCalled = true;
            GetCardsForReviewCalled = true;
            return Task.FromResult<IEnumerable<CardDto>>([]);
        }

        public Task<AiCardGenerationResponse> GenerateAiCards(int collectionId, AiCardGenerationRequest request)
        {
            WasCalled = true;
            GenerateAiCardsCalled = true;
            return Task.FromResult(new AiCardGenerationResponse());
        }

        public Task<CollectionListDto> GetList(int[] ids)
        {
            WasCalled = true;
            return Task.FromResult(new CollectionListDto
            {
                Collections = ids.Select(id => new CollectionListEntityDto { Id = id, Name = $"Collection {id}" }).ToList()
            });
        }

        public Task<bool> Remove(int id)
        {
            WasCalled = true;
            return Task.FromResult(true);
        }

        public Task<CollectionDto> Rename(int id, CollectionRenameDto renameDto)
        {
            WasCalled = true;
            return Task.FromResult(new CollectionDto { Id = id, Name = renameDto.Name, Cards = [] });
        }
    }

    private sealed class RecordingCardHttpService : ICardHttpService
    {
        public bool AddCalled { get; private set; }
        public bool RemoveCalled { get; private set; }
        public bool ReviewCalled { get; private set; }
        public CardDto AddResult { get; set; } = new() { Id = 1, CollectionId = 1, Words = [] };

        public Task<CardDto> Add(CardCreateDto cardCreateDto)
        {
            AddCalled = true;
            return Task.FromResult(AddResult);
        }

        public Task<bool> Remove(int id)
        {
            RemoveCalled = true;
            return Task.FromResult(true);
        }

        public Task<CardDto> Review(int id, ReviewCardRequest request)
        {
            ReviewCalled = true;
            return Task.FromResult(new CardDto { Id = id, CollectionId = 1, Words = [] });
        }
    }

    private sealed class RecordingWordHttpService : IWordHttpService
    {
        public bool WasCalled { get; private set; }

        public Task<WordDto> Add(int cardId, WordCreateDto wordCreateDto)
        {
            WasCalled = true;
            return Task.FromResult(new WordDto
            {
                Id = 1,
                Value = wordCreateDto.Value,
                Transcription = wordCreateDto.Transcription,
                Translation = wordCreateDto.Translation
            });
        }

        public Task<bool> Remove(int id, int cardId)
        {
            WasCalled = true;
            return Task.FromResult(true);
        }

        public Task<WordDto> Update(int cardId, int id, WordUpdateDto wordUpdateDto)
        {
            WasCalled = true;
            return Task.FromResult(new WordDto
            {
                Id = id,
                Value = wordUpdateDto.Value,
                Transcription = wordUpdateDto.Transcription,
                Translation = wordUpdateDto.Translation
            });
        }
    }
}

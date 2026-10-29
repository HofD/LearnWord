using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.Identity.Abstactions;
using LearnWord.Identity.Controllers;
using LearnWord.Identity.DAL.Context;
using LearnWord.Identity.DAL.Models;
using LearnWord.Identity.DAL.Repositories;
using LearnWord.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnWord.Identity.Tests;

public class LearningIdentityServiceTests
{
    [Fact]
    public async Task ForeignCollectionAndCardQueries_AreRejectedBeforeUpstreamReadsOrMutations()
    {
        await using var db = await Database.Create();
        await Assert.ThrowsAsync<NotFoundException>(() => db.Service.Today(10, "visitor"));
        await Assert.ThrowsAsync<NotFoundException>(() => db.Service.Summary(10, "visitor"));
        await Assert.ThrowsAsync<NotFoundException>(() => db.Service.Start(10, "visitor"));
        await Assert.ThrowsAsync<ForbiddenException>(() => db.Service.State(20, "visitor"));
        await Assert.ThrowsAsync<ForbiddenException>(() => db.Service.Attempts(20, 1, 50, "visitor"));
        Assert.Equal(0, db.Upstream.Calls);
    }

    [Fact]
    public async Task OwnedQueries_ForwardOnlyAuthorizedIdentifiersAndPagination()
    {
        await using var db = await Database.Create();
        await db.Service.Today(10, "owner");
        await db.Service.Summary(10, "owner");
        await db.Service.Start(10, "owner");
        await db.Service.State(20, "owner");
        await db.Service.Attempts(20, 3, 7, "owner");
        Assert.Equal(5, db.Upstream.Calls);
        Assert.Equal((20, 3, 7), db.Upstream.LastHistory);
    }

    [Fact]
    public async Task ForeignSession_CanNeitherBeReturnedNorCompleted()
    {
        await using var db = await Database.Create();
        await Assert.ThrowsAsync<NotFoundException>(() => db.Service.Session(db.Upstream.SessionId, "visitor"));
        await Assert.ThrowsAsync<NotFoundException>(() => db.Service.Complete(db.Upstream.SessionId, "visitor"));
        Assert.Equal(0, db.Upstream.CompleteCalls);
        var result = await db.Service.Complete(db.Upstream.SessionId, "owner");
        Assert.Equal(db.Upstream.SessionId, result.Id);
        Assert.Equal(1, db.Upstream.CompleteCalls);
    }

    [Fact]
    public async Task ForeignSessionCannotBeAttachedToAnOwnedCardReview()
    {
        await using var db = await Database.Create();
        db.Context.CardIdentities.Add(new CardIdentity { CardId = 30, UserId = "visitor" });
        await db.Context.SaveChangesAsync();
        var cardUpstream = new Cards();
        var service = new CardIdentityService(cardUpstream, new CardIdentityRepository(db.Context),
            new CollectionIdentityRepository(db.Context), db.Service);
        await Assert.ThrowsAsync<NotFoundException>(() => service.Review(30,
            new ReviewCardRequest { Outcome = "Good", SessionId = db.Upstream.SessionId }, "visitor"));
        Assert.Equal(0, cardUpstream.ReviewCalls);
    }

    [Fact]
    public async Task AllLearnerControllerActions_RejectAnonymousWithoutAccessingUpstream()
    {
        await using var db = await Database.Create();
        var controller = new LearningIdentityController(db.Service)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        Assert.IsType<UnauthorizedResult>((await controller.Today(10)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Summary(10)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Start(10)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Session(db.Upstream.SessionId)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Complete(db.Upstream.SessionId)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.State(20)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Attempts(20, 1, 50)).Result);
        Assert.Equal(0, db.Upstream.Calls);
    }

    private sealed class Upstream : ILearningHttpService
    {
        public Guid SessionId { get; } = Guid.NewGuid();
        public int Calls { get; private set; }
        public int CompleteCalls { get; private set; }
        public (int, int, int) LastHistory { get; private set; }
        public Task<TodayPlanDto> Today(int collectionId) { Calls++; return Task.FromResult(new TodayPlanDto()); }
        public Task<LearnerSummaryDto> Summary(int collectionId) { Calls++; return Task.FromResult(new LearnerSummaryDto()); }
        public Task<LearningSessionDto> Start(int collectionId) { Calls++; return Task.FromResult(new LearningSessionDto { CollectionId = collectionId, Id = SessionId }); }
        public Task<LearningSessionDto> Session(Guid sessionId) { Calls++; return Task.FromResult(new LearningSessionDto { CollectionId = 10, Id = sessionId }); }
        public Task<LearningSessionDto> Complete(Guid sessionId) { Calls++; CompleteCalls++; return Task.FromResult(new LearningSessionDto { CollectionId = 10, Id = sessionId }); }
        public Task<LearnerCardStateDto> State(int cardId) { Calls++; return Task.FromResult(new LearnerCardStateDto { CardId = cardId }); }
        public Task<PagedResult<LearningAttemptDto>> Attempts(int cardId, int page, int pageSize)
        { Calls++; LastHistory = (cardId, page, pageSize); return Task.FromResult(new PagedResult<LearningAttemptDto>()); }
    }

    private sealed class Cards : ICardHttpService
    {
        public int ReviewCalls { get; private set; }
        public Task<CardDto> Add(CardCreateDto request) => throw new NotSupportedException();
        public Task<bool> Remove(int id) => throw new NotSupportedException();
        public Task<CardDto> Review(int id, ReviewCardRequest request) { ReviewCalls++; return Task.FromResult(new CardDto { Id = id }); }
    }

    private sealed class Database : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public CollectionIdentityDbContext Context { get; }
        public Upstream Upstream { get; } = new();
        public LearningIdentityService Service => new(Upstream, new CollectionIdentityRepository(Context), new CardIdentityRepository(Context));
        private Database(SqliteConnection connection, CollectionIdentityDbContext context) { this.connection = connection; Context = context; }
        public static async Task<Database> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new CollectionIdentityDbContext(new DbContextOptionsBuilder<CollectionIdentityDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            context.CollectionIdentities.Add(new CollectionIdentity { CollectionId = 10, UserId = "owner" });
            context.CardIdentities.Add(new CardIdentity { CardId = 20, UserId = "owner" });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            return new Database(connection, context);
        }
        public async ValueTask DisposeAsync() { await Context.DisposeAsync(); await connection.DisposeAsync(); }
    }
}

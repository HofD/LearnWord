using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.DAL.Models;
using LearnWord.Identity.Abstactions;
using LearnWord.Identity.DAL.Context;
using LearnWord.Identity.DAL.Models;
using LearnWord.Identity.DAL.Repositories;
using LearnWord.Identity.Services;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnWord.Identity.Tests;

public class SocialCollectionIdentityServiceTests
{
    private const string Owner = "owner-private-id@example.com";
    private const string Reader = "reader-private-id@example.com";

    [Theory]
    [InlineData(CollectionVisibility.Private, false)]
    [InlineData(CollectionVisibility.Private, true)]
    [InlineData(CollectionVisibility.Unlisted, false)]
    [InlineData(CollectionVisibility.Public, false)]
    public async Task SharedRead_EnforcesVisibilityAndEmitsSafeContent(CollectionVisibility visibility, bool isOwner)
    {
        await using var db = await Database.Create(visibility);
        if (visibility == CollectionVisibility.Private && !isOwner)
        {
            var exception = await Assert.ThrowsAsync<NotFoundException>(() => db.Service.Get(db.Upstream.PublicId, 1, 20, Reader));
            Assert.Equal(404, exception.StatusCode);
            return;
        }

        var result = await db.Service.Get(db.Upstream.PublicId, 1, 20, isOwner ? Owner : Reader);

        Assert.Equal(isOwner, result.IsOwner);
        Assert.Equal(isOwner ? 10 : (int?)null, result.OwnerCollectionId);
        Assert.Equal("cat", Assert.Single(Assert.Single(result.Cards).Words).Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Author));
        Assert.DoesNotContain("@", result.Author);
        Assert.DoesNotContain(Owner, JsonSerializer.Serialize(result));
        Assert.DoesNotContain(Reader, JsonSerializer.Serialize(result));
        AssertPublicShape(result);
    }

    [Theory]
    [InlineData(CollectionVisibility.Private)]
    [InlineData(CollectionVisibility.Unlisted)]
    [InlineData(CollectionVisibility.Public)]
    public async Task ForkingOwnCollection_IsConflictAndDoesNotCreateAnything(CollectionVisibility visibility)
    {
        await using var db = await Database.Create(visibility);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => db.Service.Fork(db.Upstream.PublicId, Owner));

        Assert.Equal(409, exception.StatusCode);
        Assert.False(db.Upstream.ForkCalled);
        Assert.Single(await db.Context.CollectionIdentities.ToListAsync());
        Assert.Empty(await db.Context.CardIdentities.ToListAsync());
    }

    [Fact]
    public async Task PrivateCollection_CannotBeForkedByOtherUser()
    {
        await using var db = await Database.Create(CollectionVisibility.Private);
        await Assert.ThrowsAsync<NotFoundException>(() => db.Service.Fork(db.Upstream.PublicId, Reader));
        Assert.False(db.Upstream.ForkCalled);
        Assert.Single(await db.Context.CollectionIdentities.ToListAsync());
    }

    [Theory]
    [InlineData(CollectionVisibility.Public)]
    [InlineData(CollectionVisibility.Unlisted)]
    public async Task Fork_LinksNewCollectionAndEveryCardToReaderOnly(CollectionVisibility visibility)
    {
        await using var db = await Database.Create(visibility);

        var result = await db.Service.Fork(db.Upstream.PublicId, Reader);

        Assert.True(db.Upstream.ForkCalled);
        Assert.Equal(20, result.Id);
        Assert.Equal(CollectionVisibility.Private, result.Visibility);
        var collection = await db.Context.CollectionIdentities.SingleAsync(c => c.CollectionId == 20);
        Assert.Equal(Reader, collection.UserId);
        var cards = await db.Context.CardIdentities.OrderBy(c => c.CardId).ToListAsync();
        Assert.Equal(new[] { 21, 22 }, cards.Select(c => c.CardId));
        Assert.All(cards, c => Assert.Equal(Reader, c.UserId));
        Assert.Equal(Owner, (await db.Context.CollectionIdentities.SingleAsync(c => c.CollectionId == 10)).UserId);
    }

    [Fact]
    public async Task VisibilityWrite_RequiresOwnerAndPassesValidValue()
    {
        await using var db = await Database.Create(CollectionVisibility.Private);

        var denied = await Assert.ThrowsAsync<NotFoundException>(() => db.Service.SetVisibility(10, CollectionVisibility.Public, Reader));
        Assert.Equal(404, denied.StatusCode);
        Assert.False(db.Upstream.VisibilityCalled);

        var result = await db.Service.SetVisibility(10, CollectionVisibility.Unlisted, Owner);
        Assert.True(db.Upstream.VisibilityCalled);
        Assert.Equal(CollectionVisibility.Unlisted, result.Visibility);
        Assert.Equal((10, CollectionVisibility.Unlisted), db.Upstream.LastVisibility);
    }

    [Fact]
    public async Task UnknownVisibility_IsBadRequestBeforeUpstreamWrite()
    {
        await using var db = await Database.Create(CollectionVisibility.Private);
        var exception = await Assert.ThrowsAsync<BadRequestException>(() => db.Service.SetVisibility(10, (CollectionVisibility)99, Owner));
        Assert.Equal(400, exception.StatusCode);
        Assert.False(db.Upstream.VisibilityCalled);
    }

    [Fact]
    public void VisibilityRequest_RejectsMissingAndUndefinedValues()
    {
        foreach (var value in new CollectionVisibility?[] { null, (CollectionVisibility)99 })
        {
            var request = new CollectionVisibilityDto { Visibility = value };
            Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), new List<ValidationResult>(), true));
        }
        foreach (var value in Enum.GetValues<CollectionVisibility>())
        {
            var request = new CollectionVisibilityDto { Visibility = value };
            Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), new List<ValidationResult>(), true));
        }
    }

    [Fact]
    public async Task Explore_UsesExplicitSafeSummaryAndPseudonymousAuthor()
    {
        await using var db = await Database.Create(CollectionVisibility.Public);
        var result = await db.Service.Explore(2, 3, "cat");
        var summary = Assert.Single(result.Items);
        Assert.Equal((2, 3, "cat"), db.Upstream.LastExplore);
        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(4, result.TotalCount);
        var json = JsonSerializer.SerializeToElement(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(new[] { "author", "cardsCount", "name", "publicId" }, json.EnumerateObject().Select(p => p.Name).Order());
        Assert.DoesNotContain("@", summary.Author);
        Assert.DoesNotContain(Owner, json.GetRawText());
    }

    [Theory]
    [InlineData(CollectionVisibility.Private, false)]
    [InlineData(CollectionVisibility.Private, true)]
    [InlineData(CollectionVisibility.Unlisted, false)]
    [InlineData(CollectionVisibility.Public, false)]
    public async Task SharedSearch_PropagatesQueryAndPreservesAccessRules(CollectionVisibility visibility, bool isOwner)
    {
        await using var db = await Database.Create(visibility);
        const string search = "  cat & translation+?  ";
        var userId = isOwner ? Owner : Reader;
        if (visibility == CollectionVisibility.Private && !isOwner)
        {
            await Assert.ThrowsAsync<NotFoundException>(() => db.Service.Get(db.Upstream.PublicId, 2, 7, userId, search));
        }
        else
        {
            var result = await db.Service.Get(db.Upstream.PublicId, 2, 7, userId, search);
            Assert.Equal(isOwner, result.IsOwner);
            Assert.Equal(isOwner ? 10 : (int?)null, result.OwnerCollectionId);
            AssertPublicShape(result);
        }
        Assert.Equal((db.Upstream.PublicId, 2, 7, search), db.Upstream.LastGet);
    }

    [Fact]
    public async Task SharedSearchController_RequiresAuthenticationAndForwardsSearch()
    {
        await using var db = await Database.Create(CollectionVisibility.Public);
        var context = new DefaultHttpContext();
        var controller = new LearnWord.Identity.Controllers.SocialCollectionsController(db.Service)
        { ControllerContext = new ControllerContext { HttpContext = context } };

        var anonymous = await controller.Get(db.Upstream.PublicId, 2, 7, "cat");
        Assert.IsType<UnauthorizedResult>(anonymous.Result);
        Assert.Equal(default, db.Upstream.LastGet);

        context.Items["UserId"] = Reader;
        var authenticated = await controller.Get(db.Upstream.PublicId, 2, 7, "cat");
        Assert.IsType<OkObjectResult>(authenticated.Result);
        Assert.Equal((db.Upstream.PublicId, 2, 7, "cat"), db.Upstream.LastGet);
    }

    [Fact]
    public async Task ForkAfterFilteredRead_DoesNotPassSearchToSourceRead()
    {
        await using var db = await Database.Create(CollectionVisibility.Public);
        await db.Service.Get(db.Upstream.PublicId, 1, 20, Reader, "cat");

        var clone = await db.Service.Fork(db.Upstream.PublicId, Reader);

        Assert.Equal((db.Upstream.PublicId, 1, 1, (string?)null), db.Upstream.LastGet);
        Assert.Equal(2, clone.Cards.Count);
    }

    private static void AssertPublicShape(PublicCollectionDetails value)
    {
        var json = JsonSerializer.SerializeToElement(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(new[] { "author", "cards", "cardsCount", "isOwner", "name", "ownerCollectionId", "page", "pageSize", "publicId", "totalCount" },
            json.EnumerateObject().Select(p => p.Name).Order());
        var card = json.GetProperty("cards")[0];
        Assert.Equal(new[] { "words" }, card.EnumerateObject().Select(p => p.Name));
        Assert.Equal(new[] { "transcription", "translation", "value" },
            card.GetProperty("words")[0].EnumerateObject().Select(p => p.Name).Order());
    }

    private sealed class Upstream(CollectionVisibility visibility) : ISocialCollectionsHttpService
    {
        public Guid PublicId { get; } = Guid.NewGuid();
        public bool ForkCalled { get; private set; }
        public (Guid, int, int, string?) LastGet { get; private set; }
        public bool VisibilityCalled { get; private set; }
        public (int, CollectionVisibility) LastVisibility { get; private set; }
        public (int, int, string?) LastExplore { get; private set; }
        public Task<PagedResult<SocialCollectionRecord>> Explore(int page, int pageSize, string? search)
        {
            LastExplore = (page, pageSize, search);
            return Task.FromResult(new PagedResult<SocialCollectionRecord>
            {
                Page = page, PageSize = pageSize, TotalCount = 4,
                Items = [new SocialCollectionRecord { CollectionId = 10, PublicId = PublicId, Name = "Shared", CardsCount = 1, Visibility = visibility }]
            });
        }
        public Task<SocialCollectionPage> Get(Guid publicId, int page, int pageSize, string? search = null)
        {
            LastGet = (publicId, page, pageSize, search);
            return Task.FromResult(new SocialCollectionPage
        {
            CollectionId = 10, Visibility = visibility,
            Details = new PublicCollectionDetails
            {
                PublicId = PublicId, Name = "Shared", CardsCount = 1, Page = page, PageSize = pageSize, TotalCount = 1,
                Cards = [new PublicCardDto { Words = [new PublicWordDto { Value = "cat", Transcription = "kat", Translation = "кот" }] }]
            }
        });
        }
        public Task<CollectionDto> Fork(Guid publicId)
        {
            ForkCalled = true;
            return Task.FromResult(new CollectionDto
            {
                Id = 20, PublicId = Guid.NewGuid(), Name = "Fork", Visibility = CollectionVisibility.Private, ForkedFromCollectionId = 10,
                Cards = [new CardDto { Id = 21, CollectionId = 20 }, new CardDto { Id = 22, CollectionId = 20 }]
            });
        }
        public Task<CollectionDto> SetVisibility(int id, CollectionVisibility value)
        {
            VisibilityCalled = true;
            LastVisibility = (id, value);
            return Task.FromResult(new CollectionDto { Id = id, PublicId = PublicId, Name = "Shared", Visibility = value });
        }
        public Task<bool> Remove(int id) => Task.FromResult(true);
    }

    private sealed class Database : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public CollectionIdentityDbContext Context { get; }
        public Upstream Upstream { get; }
        public SocialCollectionIdentityService Service { get; }
        private Database(SqliteConnection connection, CollectionIdentityDbContext context, Upstream upstream)
        {
            this.connection = connection;
            Context = context;
            Upstream = upstream;
            Service = new SocialCollectionIdentityService(upstream, new CollectionIdentityRepository(context));
        }
        public static async Task<Database> Create(CollectionVisibility visibility)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new CollectionIdentityDbContext(new DbContextOptionsBuilder<CollectionIdentityDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            context.CollectionIdentities.Add(new CollectionIdentity { CollectionId = 10, UserId = Owner });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            return new Database(connection, context, new Upstream(visibility));
        }
        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}

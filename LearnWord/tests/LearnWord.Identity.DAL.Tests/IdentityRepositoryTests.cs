using LearnWord.Identity.DAL.Context;
using LearnWord.Identity.DAL.Models;
using LearnWord.Identity.DAL.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnWord.Identity.DAL.Tests;

public class IdentityRepositoryTests
{
    [Fact]
    public async Task CollectionRepository_Get_ReturnsOnlyMatchingUserCollectionLink()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var expected = await fixture.SeedCollectionLink(10, "user-1");
        await fixture.SeedCollectionLink(10, "user-2");
        await fixture.SeedCollectionLink(11, "user-1");
        var repository = new CollectionIdentityRepository(fixture.Context);

        var result = await repository.Get(10, "user-1");

        Assert.NotNull(result);
        Assert.Equal(expected.Id, result.Id);
    }

    [Fact]
    public async Task CollectionRepository_GetAll_ReturnsOnlyCurrentUsersLinks()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        await fixture.SeedCollectionLink(10, "user-1");
        await fixture.SeedCollectionLink(11, "user-1");
        await fixture.SeedCollectionLink(12, "user-2");
        var repository = new CollectionIdentityRepository(fixture.Context);

        var result = await repository.GetAll("user-1");

        Assert.Equal([10, 11], result.Select(x => x.CollectionId).Order());
    }

    [Fact]
    public async Task CollectionRepository_Remove_RemovesOnlyMatchingUserCollectionLink()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        await fixture.SeedCollectionLink(10, "user-1");
        var otherUserLink = await fixture.SeedCollectionLink(10, "user-2");
        var repository = new CollectionIdentityRepository(fixture.Context);

        await repository.Remove(10, "user-1");

        var remaining = await fixture.Context.CollectionIdentities.SingleAsync();
        Assert.Equal(otherUserLink.Id, remaining.Id);
    }

    [Fact]
    public async Task CardRepository_Get_ReturnsOnlyMatchingUserCardLink()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var expected = await fixture.SeedCardLink(20, "user-1");
        await fixture.SeedCardLink(20, "user-2");
        await fixture.SeedCardLink(21, "user-1");
        var repository = new CardIdentityRepository(fixture.Context);

        var result = await repository.Get(20, "user-1");

        Assert.NotNull(result);
        Assert.Equal(expected.Id, result.Id);
    }

    [Fact]
    public async Task CardRepository_Remove_RemovesOnlyMatchingUserCardLink()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        await fixture.SeedCardLink(20, "user-1");
        var otherUserLink = await fixture.SeedCardLink(20, "user-2");
        var repository = new CardIdentityRepository(fixture.Context);

        await repository.Remove(20, "user-1");

        var remaining = await fixture.Context.CardIdentities.SingleAsync();
        Assert.Equal(otherUserLink.Id, remaining.Id);
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

        public async Task<CollectionIdentity> SeedCollectionLink(int collectionId, string userId)
        {
            var link = new CollectionIdentity { CollectionId = collectionId, UserId = userId };

            Context.CollectionIdentities.Add(link);
            await SaveAndClearChanges();
            return link;
        }

        public async Task<CardIdentity> SeedCardLink(int cardId, string userId)
        {
            var link = new CardIdentity { CardId = cardId, UserId = userId };

            Context.CardIdentities.Add(link);
            await SaveAndClearChanges();
            return link;
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
}

using IdentityService.Authorization.Authorization;
using IdentityService.Authorization.Errors;
using IdentityService.Authorization.Services;
using IdentityService.DAL.Context;
using IdentityService.DAL.Models.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IdentityService.Authorization.Tests;

public class RefreshTokenServiceTests
{
    [Fact]
    public async Task RefreshToken_RotatesActiveTokenAndRevokesOriginal()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var user = await fixture.SeedUserWithTokens(new RefreshToken
        {
            Token = "old-token",
            Created = DateTime.UtcNow.AddMinutes(-5),
            CreatedByIp = "10.0.0.1",
            Expires = DateTime.UtcNow.AddDays(1)
        });
        var service = fixture.CreateService();

        var newToken = await service.RefreshToken(user, "old-token", "10.0.0.2");

        var storedUser = await fixture.Context.Users
            .Include(x => x.RefreshTokens)
            .SingleAsync(x => x.Id == user.Id);
        var oldToken = storedUser.RefreshTokens.Single(x => x.Token == "old-token");

        Assert.NotEqual("old-token", newToken.Token);
        Assert.True(newToken.IsActive);
        Assert.Equal("10.0.0.2", newToken.CreatedByIp);
        Assert.NotNull(oldToken.Revoked);
        Assert.Equal("10.0.0.2", oldToken.RevokedByIp);
        Assert.Equal("Replaced by new token", oldToken.ReasonRevoked);
        Assert.Equal(newToken.Token, oldToken.ReplacedByToken);
    }

    [Fact]
    public async Task RevokeToken_ActiveTokenRevokesWithoutReplacement()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var user = await fixture.SeedUserWithTokens(new RefreshToken
        {
            Token = "active-token",
            Created = DateTime.UtcNow.AddMinutes(-5),
            CreatedByIp = "10.0.0.1",
            Expires = DateTime.UtcNow.AddDays(1)
        });
        var service = fixture.CreateService();

        service.RevokeToken(user, "active-token", "10.0.0.2");

        var storedUser = await fixture.Context.Users
            .Include(x => x.RefreshTokens)
            .SingleAsync(x => x.Id == user.Id);
        var revokedToken = storedUser.RefreshTokens.Single(x => x.Token == "active-token");

        Assert.False(revokedToken.IsActive);
        Assert.Equal("10.0.0.2", revokedToken.RevokedByIp);
        Assert.Equal("Revoked without replacement", revokedToken.ReasonRevoked);
        Assert.Null(revokedToken.ReplacedByToken);
    }

    [Fact]
    public async Task RevokeToken_InactiveTokenThrowsInvalidRefreshTokenException()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var user = await fixture.SeedUserWithTokens(new RefreshToken
        {
            Token = "expired-token",
            Created = DateTime.UtcNow.AddDays(-8),
            CreatedByIp = "10.0.0.1",
            Expires = DateTime.UtcNow.AddDays(-1)
        });
        var service = fixture.CreateService();

        Assert.Throws<InvalidRefreshTokenException>(() => service.RevokeToken(user, "expired-token", "10.0.0.2"));
    }

    [Fact]
    public async Task GetUserByRefreshToken_ReturnsUserWithRefreshTokensLoaded()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var user = await fixture.SeedUserWithTokens(new RefreshToken
        {
            Token = "known-token",
            Created = DateTime.UtcNow,
            CreatedByIp = "10.0.0.1",
            Expires = DateTime.UtcNow.AddDays(1)
        });
        var service = fixture.CreateService();

        var result = await service.GetUserByRefreshToken("known-token");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Contains(result.RefreshTokens, x => x.Token == "known-token");
    }

    private sealed class TestIdentityDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly IConfiguration configuration;

        private TestIdentityDatabase(SqliteConnection connection, IdentityContext context, IConfiguration configuration)
        {
            this.connection = connection;
            this.configuration = configuration;
            Context = context;
        }

        public IdentityContext Context { get; }

        public static async Task<TestIdentityDatabase> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<IdentityContext>()
                .UseSqlite(connection)
                .Options;

            var context = new IdentityContext(options);
            await context.Database.EnsureCreatedAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:RefreshTokenTTL"] = "30"
                })
                .Build();

            return new TestIdentityDatabase(connection, context, configuration);
        }

        public RefreshTokenService CreateService()
        {
            return new RefreshTokenService(configuration, new JwtUtils(Context), Context);
        }

        public async Task<LwIdentityUser> SeedUserWithTokens(params RefreshToken[] refreshTokens)
        {
            var user = new LwIdentityUser($"user-{Guid.NewGuid()}@example.com")
            {
                Email = $"user-{Guid.NewGuid()}@example.com",
                RefreshTokens = refreshTokens.ToList()
            };

            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            return user;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}

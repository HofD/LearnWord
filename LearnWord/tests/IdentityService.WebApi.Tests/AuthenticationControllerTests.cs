using System.IdentityModel.Tokens.Jwt;
using IdentityService.Authorization.Authorization;
using IdentityService.Authorization.Models.Authentication;
using IdentityService.Authorization.Services;
using IdentityService.DAL.Context;
using IdentityService.DAL.Models.Entities;
using IdentityService.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IdentityService.WebApi.Tests;

public class AuthenticationControllerTests
{
    [Fact]
    public async Task Login_UnknownUser_ReturnsUnauthorized()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var controller = fixture.CreateAuthenticationController(new FakeUserManager());

        var response = await controller.Login(new LoginRequest { Email = "missing@example.com", Password = "pass" });

        Assert.IsType<UnauthorizedResult>(response.Result);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var userManager = new FakeUserManager();
        var user = await fixture.SeedUser("known@example.com", "user-1");
        userManager.AddUser(user, password: "right-pass");
        var controller = fixture.CreateAuthenticationController(userManager);

        var response = await controller.Login(new LoginRequest { Email = "known@example.com", Password = "wrong-pass" });

        Assert.IsType<UnauthorizedResult>(response.Result);
    }

    [Fact]
    public async Task Login_UnconfirmedEmail_ReturnsForbidden()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var userManager = new FakeUserManager();
        userManager.AddUser(new LwIdentityUser("known@example.com") { Id = "user-1", Email = "known@example.com", EmailConfirmed = false }, password: "right-pass");
        var controller = fixture.CreateAuthenticationController(userManager);

        var response = await controller.Login(new LoginRequest { Email = "known@example.com", Password = "right-pass" });

        Assert.IsType<ForbidResult>(response.Result);
    }

    [Fact]
    public async Task Login_ConfirmedUser_ReturnsJwtAndRefreshToken()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var userManager = new FakeUserManager();
        var user = await fixture.SeedUser("known@example.com", "user-1");
        userManager.AddUser(user, password: "right-pass");
        var controller = fixture.CreateAuthenticationController(userManager);

        var response = await controller.Login(new LoginRequest { Email = "known@example.com", Password = "right-pass" });

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<LoginResponse>(ok.Value);
        Assert.Equal("known@example.com", body.Email);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.Token);
        Assert.Contains(jwt.Claims, x => x.Type == "Id" && x.Value == "user-1");
    }

    [Fact]
    public async Task RefreshToken_UnknownToken_ReturnsBadRequest()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var controller = fixture.CreateAuthenticationController(new FakeUserManager());

        var response = await controller.RefreshToken(new RefreshTokenRequest { RefreshToken = "missing-token" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("Invalid token", badRequest.Value);
    }

    [Fact]
    public async Task RefreshToken_KnownActiveToken_RotatesAndReturnsLoginResponse()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        await fixture.SeedUserWithTokens(new RefreshToken
        {
            Token = "known-token",
            Created = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1",
            Expires = DateTime.UtcNow.AddDays(1)
        });
        var controller = fixture.CreateAuthenticationController(new FakeUserManager());

        var response = await controller.RefreshToken(new RefreshTokenRequest { RefreshToken = "known-token" });

        var ok = Assert.IsType<OkObjectResult>(response);
        var body = Assert.IsType<LoginResponse>(ok.Value);
        Assert.Equal("refresh-user@example.com", body.Email);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
        Assert.NotEqual("known-token", body.RefreshToken);
    }

    [Fact]
    public async Task RevokeToken_MissingToken_ReturnsBadRequest()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var controller = fixture.CreateAuthenticationController(new FakeUserManager());

        var response = await controller.RevokeToken(new RevokeTokenRequest { RefreshToken = "" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("{ message = Token is required }", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task RevokeToken_UnknownToken_ReturnsBadRequest()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        var controller = fixture.CreateAuthenticationController(new FakeUserManager());

        var response = await controller.RevokeToken(new RevokeTokenRequest { RefreshToken = "missing-token" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("Invalid token", badRequest.Value);
    }

    [Fact]
    public async Task RevokeToken_ActiveToken_ReturnsOkAndRevokesToken()
    {
        await using var fixture = await TestIdentityDatabase.Create();
        await fixture.SeedUserWithTokens(new RefreshToken
        {
            Token = "known-token",
            Created = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1",
            Expires = DateTime.UtcNow.AddDays(1)
        });
        var controller = fixture.CreateAuthenticationController(new FakeUserManager());

        var response = await controller.RevokeToken(new RevokeTokenRequest { RefreshToken = "known-token" });

        var ok = Assert.IsType<OkObjectResult>(response);
        Assert.Equal("Token revoked", ok.Value);

        var storedUser = await fixture.Context.Users.Include(x => x.RefreshTokens).SingleAsync();
        Assert.False(storedUser.RefreshTokens.Single(x => x.Token == "known-token").IsActive);
    }

    private sealed class TestIdentityDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestIdentityDatabase(SqliteConnection connection, IdentityContext context, IConfiguration configuration)
        {
            this.connection = connection;
            Context = context;
            Configuration = configuration;
        }

        public IdentityContext Context { get; }

        private IConfiguration Configuration { get; }

        public static async Task<TestIdentityDatabase> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var context = new IdentityContext(new DbContextOptionsBuilder<IdentityContext>()
                .UseSqlite(connection)
                .Options);
            await context.Database.EnsureCreatedAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:Key"] = "0123456789abcdef0123456789abcdef",
                    ["JwtSettings:Issuer"] = "learnword-tests",
                    ["JwtSettings:Audience"] = "learnword-tests",
                    ["JwtSettings:ExpiryInMinutes"] = "30",
                    ["JwtSettings:RefreshTokenTTL"] = "30"
                })
                .Build();

            return new TestIdentityDatabase(connection, context, configuration);
        }

        public AuthenticationController CreateAuthenticationController(UserManager<LwIdentityUser> userManager)
        {
            return new AuthenticationController(
                userManager,
                new JwtHandler(Configuration),
                new RefreshTokenService(Configuration, new JwtUtils(Context), Context))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        public async Task SeedUserWithTokens(params RefreshToken[] refreshTokens)
        {
            Context.Users.Add(new LwIdentityUser("refresh-user@example.com")
            {
                Id = "refresh-user",
                Email = "refresh-user@example.com",
                EmailConfirmed = true,
                RefreshTokens = refreshTokens.ToList()
            });
            await Context.SaveChangesAsync();
        }

        public async Task<LwIdentityUser> SeedUser(string email, string userId)
        {
            var user = new LwIdentityUser(email)
            {
                Id = userId,
                Email = email,
                EmailConfirmed = true,
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                SecurityStamp = Guid.NewGuid().ToString()
            };
            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            Context.Entry(user).State = EntityState.Detached;
            return user;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class FakeUserManager : UserManager<LwIdentityUser>
    {
        private readonly Dictionary<string, (LwIdentityUser User, string Password)> users = new(StringComparer.OrdinalIgnoreCase);

        public FakeUserManager()
            : base(
                new FakeUserStore(),
                Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
                new PasswordHasher<LwIdentityUser>(),
                [],
                [],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null!,
                NullLogger<UserManager<LwIdentityUser>>.Instance)
        {
        }

        public void AddUser(LwIdentityUser user, string password)
        {
            users[user.Email!] = (user, password);
        }

        public override Task<LwIdentityUser?> FindByEmailAsync(string email)
        {
            return Task.FromResult(users.TryGetValue(email, out var entry) ? entry.User : null);
        }

        public override Task<bool> CheckPasswordAsync(LwIdentityUser user, string password)
        {
            return Task.FromResult(users.TryGetValue(user.Email!, out var entry) && entry.Password == password);
        }

        public override Task<bool> IsEmailConfirmedAsync(LwIdentityUser user)
        {
            return Task.FromResult(user.EmailConfirmed);
        }
    }

    private sealed class FakeUserStore : IUserStore<LwIdentityUser>
    {
        public Task<IdentityResult> CreateAsync(LwIdentityUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(LwIdentityUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public void Dispose() { }
        public Task<LwIdentityUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<LwIdentityUser?>(null);
        public Task<LwIdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult<LwIdentityUser?>(null);
        public Task<string?> GetNormalizedUserNameAsync(LwIdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task<string> GetUserIdAsync(LwIdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(LwIdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetNormalizedUserNameAsync(LwIdentityUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }
        public Task SetUserNameAsync(LwIdentityUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }
        public Task<IdentityResult> UpdateAsync(LwIdentityUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
    }
}

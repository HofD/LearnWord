using System.Net;
using System.Net.Sockets;
using System.Text;
using IdentityService.Authorization.Models.Authentication;
using IdentityService.Authorization.Models.Users;
using IdentityService.Authorization.Services;
using IdentityService.DAL.Models.Entities;
using IdentityService.WebApi.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IdentityService.WebApi.Tests;

public class AccountControllerTests
{
    [Fact]
    public async Task Register_NewUser_ReturnsOkCreatesUserAndSendsConfirmation()
    {
        await using var smtp = await TestSmtpServer.Start();
        var userManager = new FakeUserManager();
        var controller = CreateController(userManager, smtp);

        var response = await controller.Register(new UserRegisterRequest
        {
            Email = "new@example.com",
            Password = "Password1!"
        });

        Assert.IsType<OkResult>(response);
        Assert.NotNull(await userManager.FindByEmailAsync("new@example.com"));
        Assert.Equal(1, smtp.Messages.Count);
        Assert.Contains("userId=", smtp.Messages[0]);
        Assert.Contains("code=confirm-token", smtp.Messages[0]);
    }

    [Fact]
    public async Task Register_DuplicateConfirmedUser_ReturnsOkWithoutSendingEmail()
    {
        await using var smtp = await TestSmtpServer.Start();
        var userManager = new FakeUserManager();
        userManager.AddUser(new LwIdentityUser("known@example.com")
        {
            Id = "known-user",
            Email = "known@example.com",
            EmailConfirmed = true
        });
        var controller = CreateController(userManager, smtp);

        var response = await controller.Register(new UserRegisterRequest
        {
            Email = "known@example.com",
            Password = "Password1!"
        });

        Assert.IsType<OkResult>(response);
        Assert.Empty(smtp.Messages);
    }

    [Fact]
    public async Task Register_DuplicateUnconfirmedUser_ReturnsOkAndResendsConfirmation()
    {
        await using var smtp = await TestSmtpServer.Start();
        var userManager = new FakeUserManager();
        userManager.AddUser(new LwIdentityUser("known@example.com")
        {
            Id = "known-user",
            Email = "known@example.com",
            EmailConfirmed = false
        });
        var controller = CreateController(userManager, smtp);

        var response = await controller.Register(new UserRegisterRequest
        {
            Email = "known@example.com",
            Password = "Password1!"
        });

        Assert.IsType<OkResult>(response);
        Assert.Single(smtp.Messages);
    }

    [Fact]
    public async Task Register_IdentityErrors_ReturnsBadRequestWithErrors()
    {
        await using var smtp = await TestSmtpServer.Start();
        var userManager = new FakeUserManager
        {
            CreateResult = IdentityResult.Failed(new IdentityError { Code = "WeakPassword", Description = "Weak password." })
        };
        var controller = CreateController(userManager, smtp);

        var response = await controller.Register(new UserRegisterRequest
        {
            Email = "new@example.com",
            Password = "weak"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        var errors = Assert.IsAssignableFrom<IEnumerable<IdentityError>>(badRequest.Value);
        Assert.Contains(errors, x => x.Code == "WeakPassword");
        Assert.Empty(smtp.Messages);
    }

    [Fact]
    public async Task SendEmailConformation_MissingUser_ReturnsNotFound()
    {
        await using var smtp = await TestSmtpServer.Start();
        var controller = CreateController(new FakeUserManager(), smtp);

        var response = await controller.SendEmailConformation(new SendConfirmationRequest { Email = "missing@example.com" });

        var notFound = Assert.IsType<NotFoundObjectResult>(response);
        Assert.Equal("User not exists.", notFound.Value);
    }

    [Fact]
    public async Task SendEmailConformation_AlreadyConfirmed_ReturnsBadRequest()
    {
        await using var smtp = await TestSmtpServer.Start();
        var userManager = new FakeUserManager();
        userManager.AddUser(new LwIdentityUser("known@example.com")
        {
            Id = "known-user",
            Email = "known@example.com",
            EmailConfirmed = true
        });
        var controller = CreateController(userManager, smtp);

        var response = await controller.SendEmailConformation(new SendConfirmationRequest { Email = "known@example.com" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("Email already confirmed.", badRequest.Value);
    }

    [Fact]
    public async Task SendEmailConformation_UnconfirmedUser_SendsEmailAndReturnsOk()
    {
        await using var smtp = await TestSmtpServer.Start();
        var userManager = new FakeUserManager();
        userManager.AddUser(new LwIdentityUser("known@example.com")
        {
            Id = "known-user",
            Email = "known@example.com",
            EmailConfirmed = false
        });
        var controller = CreateController(userManager, smtp);

        var response = await controller.SendEmailConformation(new SendConfirmationRequest { Email = "known@example.com" });

        Assert.IsType<OkResult>(response);
        Assert.Single(smtp.Messages);
        Assert.Contains("userId=known-user", smtp.Messages[0]);
    }

    [Theory]
    [InlineData(null, "code")]
    [InlineData("user-1", null)]
    public async Task ConfirmEmail_MissingUserIdOrCode_ReturnsBadRequest(string? userId, string? code)
    {
        await using var smtp = await TestSmtpServer.Start();
        var controller = CreateController(new FakeUserManager(), smtp);

        var response = await controller.ConfirmEmail(userId!, code!);

        Assert.IsType<BadRequestResult>(response);
    }

    [Fact]
    public async Task ConfirmEmail_MissingUser_ReturnsNotFound()
    {
        await using var smtp = await TestSmtpServer.Start();
        var controller = CreateController(new FakeUserManager(), smtp);

        var response = await controller.ConfirmEmail("missing-user", "code");

        Assert.IsType<NotFoundResult>(response);
    }

    [Fact]
    public async Task ConfirmEmail_ValidToken_ReturnsOkAndConfirmsUser()
    {
        await using var smtp = await TestSmtpServer.Start();
        var userManager = new FakeUserManager();
        var user = new LwIdentityUser("known@example.com")
        {
            Id = "known-user",
            Email = "known@example.com",
            EmailConfirmed = false
        };
        userManager.AddUser(user);
        var controller = CreateController(userManager, smtp);

        var response = await controller.ConfirmEmail("known-user", "valid-code");

        Assert.IsType<OkResult>(response);
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmail_InvalidToken_ReturnsInternalServerErrorWithErrors()
    {
        await using var smtp = await TestSmtpServer.Start();
        var userManager = new FakeUserManager();
        userManager.AddUser(new LwIdentityUser("known@example.com")
        {
            Id = "known-user",
            Email = "known@example.com"
        });
        var controller = CreateController(userManager, smtp);

        var response = await controller.ConfirmEmail("known-user", "invalid-code");

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(500, objectResult.StatusCode);
        var errors = Assert.IsAssignableFrom<IEnumerable<IdentityError>>(objectResult.Value);
        Assert.Contains(errors, x => x.Code == "InvalidToken");
    }

    private static AccountController CreateController(FakeUserManager userManager, TestSmtpServer smtp)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Registration:EmailConfirmationUrl"] = "https://learnword.test/confirm",
                ["Smtp:Host"] = "127.0.0.1",
                ["Smtp:Port"] = smtp.Port.ToString(),
                ["Smtp:UseSsl"] = "false",
                ["Smtp:FromEmail"] = "register@example.com"
            })
            .Build();

        return new AccountController(
            userManager,
            new EmailService(configuration),
            NullLogger<AccountController>.Instance,
            configuration);
    }

    private sealed class FakeUserManager : UserManager<LwIdentityUser>
    {
        private readonly Dictionary<string, LwIdentityUser> usersByEmail = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LwIdentityUser> usersById = new(StringComparer.OrdinalIgnoreCase);

        public FakeUserManager()
            : base(
                new FakeUserStore(),
                null,
                new PasswordHasher<LwIdentityUser>(),
                [],
                [],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null!,
                NullLogger<UserManager<LwIdentityUser>>.Instance)
        {
        }

        public IdentityResult CreateResult { get; init; } = IdentityResult.Success;

        public void AddUser(LwIdentityUser user)
        {
            usersByEmail[user.Email!] = user;
            usersById[user.Id] = user;
        }

        public override Task<LwIdentityUser?> FindByEmailAsync(string email)
        {
            return Task.FromResult(usersByEmail.TryGetValue(email, out var user) ? user : null);
        }

        public override Task<LwIdentityUser?> FindByIdAsync(string userId)
        {
            return Task.FromResult(usersById.TryGetValue(userId, out var user) ? user : null);
        }

        public override Task<bool> IsEmailConfirmedAsync(LwIdentityUser user)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public override Task<IdentityResult> CreateAsync(LwIdentityUser user, string password)
        {
            if (CreateResult.Succeeded)
            {
                user.Id = "created-user";
                AddUser(user);
            }

            return Task.FromResult(CreateResult);
        }

        public override Task<string> GenerateEmailConfirmationTokenAsync(LwIdentityUser user)
        {
            return Task.FromResult("confirm-token");
        }

        public override Task<IdentityResult> ConfirmEmailAsync(LwIdentityUser user, string token)
        {
            if (token != "valid-code")
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "Invalid token." }));
            }

            user.EmailConfirmed = true;
            return Task.FromResult(IdentityResult.Success);
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

    private sealed class TestSmtpServer : IAsyncDisposable
    {
        private readonly TcpListener listener;
        private readonly CancellationTokenSource cancellation = new();
        private readonly Task acceptLoop;

        private TestSmtpServer(TcpListener listener)
        {
            this.listener = listener;
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            acceptLoop = AcceptLoop();
        }

        public int Port { get; }

        public List<string> Messages { get; } = [];

        public static Task<TestSmtpServer> Start()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return Task.FromResult(new TestSmtpServer(listener));
        }

        private async Task AcceptLoop()
        {
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(cancellation.Token);
                    _ = Task.Run(() => HandleClient(client), cancellation.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            using var acceptedClient = client;
            await using var stream = acceptedClient.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

            await writer.WriteLineAsync("220 localhost");

            while (await reader.ReadLineAsync() is { } line)
            {
                if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("250-localhost");
                    await writer.WriteLineAsync("250 PIPELINING");
                }
                else if (line.StartsWith("MAIL FROM", StringComparison.OrdinalIgnoreCase) ||
                         line.StartsWith("RCPT TO", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("250 OK");
                }
                else if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                    var builder = new StringBuilder();
                    while (await reader.ReadLineAsync() is { } dataLine && dataLine != ".")
                    {
                        builder.AppendLine(dataLine);
                    }
                    Messages.Add(builder.ToString());
                    await writer.WriteLineAsync("250 OK");
                }
                else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("221 Bye");
                    break;
                }
                else
                {
                    await writer.WriteLineAsync("250 OK");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            cancellation.Cancel();
            listener.Stop();

            try
            {
                await acceptLoop;
            }
            catch (SocketException)
            {
            }

            cancellation.Dispose();
        }
    }
}

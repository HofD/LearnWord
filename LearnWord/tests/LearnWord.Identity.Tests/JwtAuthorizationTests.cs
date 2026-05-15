using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LearnWord.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LearnWord.Identity.Tests;

public class JwtAuthorizationTests
{
    private const string SigningKey = "0123456789abcdef0123456789abcdef";

    [Fact]
    public void ValidateJwtToken_ValidToken_ReturnsUserIdClaim()
    {
        var jwtUtils = new JwtUtils(CreateConfiguration());
        var token = CreateToken("user-1");

        var userId = jwtUtils.ValidateJwtToken(token);

        Assert.Equal("user-1", userId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    public void ValidateJwtToken_MissingOrInvalidToken_ReturnsNull(string? token)
    {
        var jwtUtils = new JwtUtils(CreateConfiguration());

        var userId = jwtUtils.ValidateJwtToken(token!);

        Assert.Null(userId);
    }

    [Fact]
    public async Task JwtMiddleware_ValidBearerToken_PutsUserIdIntoHttpContextItems()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {CreateToken("user-1")}";
        var middleware = new JwtMiddleware(nextContext =>
        {
            Assert.Equal("user-1", nextContext.Items["UserId"]);
            return Task.CompletedTask;
        });

        await middleware.Invoke(context, new JwtUtils(CreateConfiguration()));

        Assert.Equal("user-1", context.Items["UserId"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Bearer invalid-token")]
    public async Task JwtMiddleware_MissingOrInvalidBearerToken_DoesNotSetUserId(string? authorization)
    {
        var context = new DefaultHttpContext();
        if (authorization != null)
        {
            context.Request.Headers.Authorization = authorization;
        }
        var middleware = new JwtMiddleware(nextContext =>
        {
            Assert.False(nextContext.Items.ContainsKey("UserId"));
            return Task.CompletedTask;
        });

        await middleware.Invoke(context, new JwtUtils(CreateConfiguration()));

        Assert.False(context.Items.ContainsKey("UserId"));
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = SigningKey
            })
            .Build();
    }

    private static string CreateToken(string userId)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: [new Claim("Id", userId)],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

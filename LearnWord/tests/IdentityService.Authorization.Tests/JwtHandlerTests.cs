using System.IdentityModel.Tokens.Jwt;
using IdentityService.Authorization.Authorization;
using IdentityService.DAL.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace IdentityService.Authorization.Tests;

public class JwtHandlerTests
{
    [Fact]
    public void GetSigningCredentials_UsesConfiguredSymmetricKeyAndHmacSha256()
    {
        var handler = new JwtHandler(CreateConfiguration());

        var credentials = handler.GetSigningCredentials();

        Assert.Equal(SecurityAlgorithms.HmacSha256, credentials.Algorithm);
        Assert.IsType<SymmetricSecurityKey>(credentials.Key);
    }

    [Fact]
    public void GetClaims_AddsUserIdClaim()
    {
        var handler = new JwtHandler(CreateConfiguration());
        var user = new LwIdentityUser("user@example.com") { Id = "user-1" };

        var claims = handler.GetClaims(user);

        Assert.Contains(claims, x => x.Type == "Id" && x.Value == "user-1");
    }

    [Fact]
    public void GenerateTokenOptions_UsesConfiguredIssuerAudienceExpiryAndSigningCredentials()
    {
        var handler = new JwtHandler(CreateConfiguration());
        var credentials = handler.GetSigningCredentials();
        var claims = handler.GetClaims(new LwIdentityUser("user@example.com") { Id = "user-1" });
        var before = DateTime.Now;

        var token = handler.GenerateTokenOptions(credentials, claims);

        Assert.Equal("learnword-tests", token.Issuer);
        Assert.Contains("learnword-client", token.Audiences);
        Assert.Contains(token.Claims, x => x.Type == "Id" && x.Value == "user-1");
        Assert.Same(credentials, token.SigningCredentials);
        Assert.InRange(token.ValidTo, before.AddMinutes(29).ToUniversalTime(), before.AddMinutes(31).ToUniversalTime());

        var serialized = new JwtSecurityTokenHandler().WriteToken(token);
        Assert.False(string.IsNullOrWhiteSpace(serialized));
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "0123456789abcdef0123456789abcdef",
                ["JwtSettings:Issuer"] = "learnword-tests",
                ["JwtSettings:Audience"] = "learnword-client",
                ["JwtSettings:ExpiryInMinutes"] = "30"
            })
            .Build();
    }
}

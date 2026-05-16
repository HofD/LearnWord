using LearnWord.Identity.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LearnWord.Identity.DAL.Tests;

public class CollectionIdentityDbContextDiTests
{
    [Fact]
    public void CollectionIdentityDbContext_ProductionRegistration_ResolvesWithoutOpeningDatabaseConnection()
    {
        var configuration = new ConfigurationManager();
        configuration["ConnectionStrings:LwConnection"] =
            "Host=localhost;Database=learnword_identity_test;Username=test;Password=test";

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<CollectionIdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("LwConnection")));

        using var provider = services.BuildServiceProvider();

        using var context = provider.GetRequiredService<CollectionIdentityDbContext>();

        Assert.NotNull(context);
    }
}

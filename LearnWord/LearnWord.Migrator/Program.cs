using IdentityService.DAL.Context;
using LearnWord.DAL;
using LearnWord.Identity.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__LwConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("ConnectionStrings__LwConnection is required.");
    return 1;
}

await MigrateIdentityContext(connectionString);
await MigrateWordsDbContext(connectionString);
await MigrateCollectionIdentityDbContext(connectionString);

Console.WriteLine("All database migrations completed.");
return 0;

static async Task MigrateIdentityContext(string connectionString)
{
    Console.WriteLine("Applying IdentityContext migrations...");
    var options = new DbContextOptionsBuilder<IdentityContext>()
        .UseNpgsql(connectionString, builder => builder.MigrationsAssembly("IdentityService.Migrations"))
        .Options;

    await using var context = new IdentityContext(options);
    await context.Database.MigrateAsync();
}

static async Task MigrateWordsDbContext(string connectionString)
{
    Console.WriteLine("Applying WordsDbContext migrations...");
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:LwConnection"] = connectionString
        })
        .Build();

    await using var context = new WordsDbContext(configuration);
    await context.Database.MigrateAsync();
}

static async Task MigrateCollectionIdentityDbContext(string connectionString)
{
    Console.WriteLine("Applying CollectionIdentityDbContext migrations...");
    var options = new DbContextOptionsBuilder<CollectionIdentityDbContext>()
        .UseNpgsql(connectionString, builder => builder.MigrationsAssembly("LearnWord.Identity.Migrations"))
        .Options;

    await using var context = new CollectionIdentityDbContext(options);
    await context.Database.MigrateAsync();
}

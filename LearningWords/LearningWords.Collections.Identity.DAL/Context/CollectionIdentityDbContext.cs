using LearningWords.Collections.Identity.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LearningWords.Collections.Identity.DAL.Context
{
    public class CollectionIdentityDbContext : DbContext
    {
        public DbSet<CollectionIdentity> CollectionIdentities { get; set; }

        protected readonly IConfiguration Configuration;
        private const string SchemaName = "identitywords";

        public CollectionIdentityDbContext(IConfiguration configuration) : base()
        {
            Configuration = configuration;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(SchemaName);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseNpgsql(Configuration.GetConnectionString("LwConnection"), b => b.MigrationsAssembly("LearningWords.Collections.Identity.Migrations"));
        }
    }
}

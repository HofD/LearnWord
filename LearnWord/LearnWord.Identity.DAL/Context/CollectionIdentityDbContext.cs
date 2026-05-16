using LearnWord.Identity.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnWord.Identity.DAL.Context
{
    // TODO: RENAME TO LearnWordIdentityDbContext
    public class CollectionIdentityDbContext : DbContext
    {
        public DbSet<CollectionIdentity> CollectionIdentities { get; set; }
        public DbSet<CardIdentity> CardIdentities { get; set; }

        private const string SchemaName = "identitywords";

        public CollectionIdentityDbContext(DbContextOptions<CollectionIdentityDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(SchemaName);
        }
    }
}

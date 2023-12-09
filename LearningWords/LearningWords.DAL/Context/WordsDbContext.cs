using LearningWords.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LearningWords.DAL
{
    public class WordsDbContext : DbContext
    {
        #region DbSets
        public DbSet<Word>? Words { get; set; }
        public DbSet<Collection>? Collections { get; set; }
        public DbSet<Card>? Cards { get; set; }
        #endregion

        protected readonly IConfiguration Configuration;
        private const string SchemaName = "words";

        public WordsDbContext(IConfiguration configuration) : base()
        {
            Configuration = configuration;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(SchemaName);
            //modelBuilder.ApplyUtcDateTimeConverter();

            //modelBuilder.ApplyConfiguration(new DeviceConfiguration());
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseNpgsql(Configuration.GetConnectionString("LwConnection"), b => b.MigrationsAssembly("LearningWords.Migrations"));
        }
    }
}

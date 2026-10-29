using LearnWord.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LearnWord.DAL
{
    public class WordsDbContext : DbContext
    {
        #region DbSets
        public DbSet<Word> Words { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<LearningAttempt> LearningAttempts { get; set; }
        public DbSet<LearnerCardState> LearnerCardStates { get; set; }
        public DbSet<LearningSession> LearningSessions { get; set; }
        public DbSet<LearningSessionCard> LearningSessionCards { get; set; }
        public DbSet<CoachAnalysis> CoachAnalyses { get; set; }
        public DbSet<CoachIntervention> CoachInterventions { get; set; }
        public DbSet<InterventionAttempt> InterventionAttempts { get; set; }
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
            modelBuilder.Entity<Collection>().HasIndex(x => x.PublicId).IsUnique();
            modelBuilder.Entity<Collection>().HasIndex(x => new { x.Visibility, x.Id });
            modelBuilder.Entity<Collection>().Property(x => x.Visibility).HasDefaultValue(CollectionVisibility.Private);
            modelBuilder.Entity<Collection>().Property(x => x.PublicId).HasDefaultValueSql("gen_random_uuid()");
            modelBuilder.Entity<Collection>().HasQueryFilter(x => x.DeletedAt == null);
            modelBuilder.Entity<Card>().HasQueryFilter(x => x.DeletedAt == null);
            modelBuilder.Entity<Card>().HasIndex(x => new { x.CollectionId, x.DueDate, x.Id });
            modelBuilder.Entity<Word>().HasQueryFilter(x => x.DeletedAt == null);
            modelBuilder.Entity<LearnerCardState>().HasKey(x => x.CardId);
            modelBuilder.Entity<LearnerCardState>().Property(x => x.Source).IsRequired();
            modelBuilder.Entity<LearnerCardState>().HasOne<Card>().WithOne().HasForeignKey<LearnerCardState>(x => x.CardId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<LearnerCardState>().HasIndex(x => x.Status);
            modelBuilder.Entity<LearningAttempt>().HasOne<Card>().WithMany().HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<LearningAttempt>().HasOne<LearningSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<LearningAttempt>().HasIndex(x => new { x.CardId, x.ReviewedAt });
            modelBuilder.Entity<LearningAttempt>().HasIndex(x => new { x.CardId, x.RequestId }).IsUnique();
            modelBuilder.Entity<LearningAttempt>().HasIndex(x => new { x.SessionId, x.CardId }).IsUnique();
            modelBuilder.Entity<LearningAttempt>().HasIndex(x => x.ReviewedAt);
            modelBuilder.Entity<LearningSession>().HasOne<Collection>().WithMany().HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<LearningSession>().HasIndex(x => new { x.CollectionId, x.StartedAt });
            modelBuilder.Entity<LearningSessionCard>().HasKey(x => new { x.SessionId, x.CardId });
            modelBuilder.Entity<LearningSessionCard>().HasOne<LearningSession>().WithMany(x => x.Cards).HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<LearningSessionCard>().HasOne<Card>().WithMany().HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Restrict);
            ConfigureCoach(modelBuilder);
            //modelBuilder.ApplyUtcDateTimeConverter();

            //modelBuilder.ApplyConfiguration(new DeviceConfiguration());
        }

        public static void ConfigureCoach(ModelBuilder modelBuilder)
        {
            foreach (var name in new[] { "Status", "Decision", "Language", "Provider", "Model", "PromptVersion", "Summary", "ObservationsJson" })
                modelBuilder.Entity<CoachAnalysis>().Property<string>(name).IsRequired();
            foreach (var name in new[] { "Type", "Title", "Status", "PayloadJson" })
                modelBuilder.Entity<CoachIntervention>().Property<string>(name).IsRequired();
            modelBuilder.Entity<InterventionAttempt>().Property(x => x.OptionId).IsRequired();
            modelBuilder.Entity<CoachAnalysis>().HasIndex(x => new { x.SessionId, x.PromptVersion }).IsUnique();
            modelBuilder.Entity<CoachAnalysis>().HasOne<LearningSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CoachAnalysis>().HasOne<Collection>().WithMany().HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CoachIntervention>().HasOne<CoachAnalysis>().WithMany().HasForeignKey(x => x.AnalysisId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CoachIntervention>().HasOne<Card>().WithMany().HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CoachIntervention>().HasIndex(x => new { x.CardId, x.LearningRevision, x.CompletedAt });
            modelBuilder.Entity<InterventionAttempt>().HasOne<CoachIntervention>().WithMany().HasForeignKey(x => x.InterventionId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InterventionAttempt>().HasOne<Card>().WithMany().HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InterventionAttempt>().HasIndex(x => new { x.InterventionId, x.ExerciseId }).IsUnique();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseNpgsql(Configuration.GetConnectionString("LwConnection"), b => b.MigrationsAssembly("LearnWord.Migrations"));
        }
    }
}

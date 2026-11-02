using LearnWord.BL.Models.Dto;
using LearnWord.DAL;
using LearnWord.DAL.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LearnWord.WebApi.Tests;

internal sealed class CoachTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    public WordsDbContext Context { get; }
    public int SaveCalls => ((TestContext)Context).SaveCalls;
    public DateTimeOffset Now { get; } = new(2026, 10, 29, 9, 0, 0, TimeSpan.Zero);
    private CoachTestDatabase(SqliteConnection connection, WordsDbContext context)
    { this.connection = connection; Context = context; }

    public static async Task<CoachTestDatabase> Create()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var context = new TestContext(connection);
        await context.Database.EnsureCreatedAsync();
        return new CoachTestDatabase(connection, context);
    }

    public async Task<(Card Card, LearningSession Session)> Seed(int revision = 0, int failures = 2, string value = "eventually")
    {
        var card = new Card {
            Collection = new Collection { Name = "Private collection", CreatedAt = Now.AddDays(-10), Cards = [] },
            Words = [new Word { Value = value, Translation = "в конце концов", Transcription = "", CreatedAt = Now.AddDays(-10) }],
            CreatedAt = Now.AddDays(-10), DueDate = Now.AddDays(1), LastReviewedAt = Now.AddMinutes(-1),
            ReviewCount = failures, IntervalDays = 1, EaseFactor = 2.1m, LearningRevision = revision
        };
        Context.Cards.Add(card);
        await Context.SaveChangesAsync();
        var session = new LearningSession {
            Id = Guid.NewGuid(), CollectionId = card.CollectionId, StartedAt = Now.AddMinutes(-5), CompletedAt = Now,
            PlannedCardsCount = 1, CompletedCardsCount = 1, FailedReviews = 1,
            Cards = [new LearningSessionCard { CardId = card.Id, LearningRevision = revision, Position = 0 }]
        };
        foreach (var member in session.Cards) member.SessionId = session.Id;
        Context.LearningSessions.Add(session);
        for (var i = 0; i < failures; i++)
            Context.LearningAttempts.Add(new LearningAttempt {
                Id = Guid.NewGuid(), CardId = card.Id, LearningRevision = revision,
                SessionId = i == failures - 1 ? session.Id : null,
                Outcome = ReviewOutcome.Again, ReviewedAt = Now.AddMinutes(i - failures), ResponseTimeMs = 15000,
                PreviousDueDate = Now.AddDays(-1), NewDueDate = Now.AddDays(1),
                PreviousEaseFactor = 2.3m, NewEaseFactor = 2.1m, NewIntervalDays = 1
            });
        Context.LearnerCardStates.Add(new LearnerCardState {
            CardId = card.Id, LearningRevision = revision, Status = LearnerStatus.Weak, Source = "Observed",
            AttemptsCount = failures, FailedAttempts = failures, ConsecutiveFailures = failures,
            LastOutcome = ReviewOutcome.Again, LastAttemptAt = Now.AddMinutes(-1), DifficultyScore = .9m, MasteryScore = 0
        });
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        return (card, session);
    }

    public async ValueTask DisposeAsync()
    { await Context.DisposeAsync(); await connection.DisposeAsync(); }

    private sealed class TestContext(SqliteConnection connection) : WordsDbContext(null!)
    {
        public int SaveCalls { get; private set; }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        { SaveCalls++; return base.SaveChangesAsync(cancellationToken); }
        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(connection);
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
                foreach (var property in entity.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset))
                        property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, long>(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero)));
                    else if (property.ClrType == typeof(DateTimeOffset?))
                        property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset?, long?>(v => v.HasValue ? v.Value.UtcTicks : null, v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null));
                }
        }
    }
}

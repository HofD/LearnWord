using LearnWord.BL.Abstractions;
using LearnWord.BL.Mapping;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.BL.Services;
using LearnWord.DAL;
using LearnWord.DAL.Models;
using LearnWord.DAL.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnWord.BL.Tests;

public class LearningHistoryTests
{
    [Theory]
    [InlineData("Again", 1, "2.30")]
    [InlineData("Hard", 4, "2.35")]
    [InlineData("Good", 8, "2.50")]
    [InlineData("Easy", 10, "2.65")]
    public async Task Review_PersistsActualBeforeAndAfterWithOptionalTelemetry(string outcome, int interval, string ease)
    {
        await using var db = await Database.Create();
        var before = await db.SeedCard();
        var reviewed = await db.Service.Review(before.Id, new ReviewCardRequest { Outcome = outcome, ResponseTimeMs = 2450 });
        db.Context.ChangeTracker.Clear();
        var attempt = await db.Context.Set<LearningAttempt>().SingleAsync();
        var stored = await db.Context.Cards.SingleAsync();
        Assert.Equal(before.Id, attempt.CardId);
        Assert.Equal(Enum.Parse<ReviewOutcome>(outcome), attempt.Outcome);
        Assert.Equal(3, attempt.PreviousIntervalDays);
        Assert.Equal(interval, attempt.NewIntervalDays);
        Assert.Equal(2.5m, attempt.PreviousEaseFactor);
        Assert.Equal(decimal.Parse(ease, System.Globalization.CultureInfo.InvariantCulture), attempt.NewEaseFactor);
        Assert.Equal(before.DueDate, attempt.PreviousDueDate);
        Assert.Equal(stored.DueDate, attempt.NewDueDate);
        Assert.Equal(stored.LastReviewedAt, attempt.ReviewedAt);
        Assert.Equal(reviewed.ReviewCount, stored.ReviewCount);
        Assert.Equal(2450, attempt.ResponseTimeMs);
        Assert.Null(attempt.SessionId);
        Assert.NotEqual(Guid.Empty, attempt.Id);
        var state = await db.Context.Set<LearnerCardState>().SingleAsync();
        Assert.Equal(1, state.AttemptsCount);
        Assert.Equal(attempt.ReviewedAt, state.LastAttemptAt);
    }

    [Fact]
    public async Task Review_OutcomeOnlyRequestRetainsCompatibility()
    {
        await using var db = await Database.Create();
        var card = await db.SeedCard();
        var result = await db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Good" });
        Assert.Equal(3, result.ReviewCount);
        Assert.Null((await db.Context.Set<LearningAttempt>().SingleAsync()).ResponseTimeMs);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(600001)]
    [InlineData(int.MaxValue)]
    public async Task Review_InvalidTelemetryDoesNotChangeCardOrAppendEvidence(int milliseconds)
    {
        await using var db = await Database.Create();
        var card = await db.SeedCard();
        await Assert.ThrowsAsync<BadRequestException>(() => db.Service.Review(card.Id,
            new ReviewCardRequest { Outcome = "Good", ResponseTimeMs = milliseconds }));
        db.Context.ChangeTracker.Clear();
        Assert.Empty(await db.Context.Set<LearningAttempt>().ToListAsync());
        Assert.Equal(card.DueDate, (await db.Context.Cards.SingleAsync()).DueDate);
        Assert.Equal(2, (await db.Context.Cards.SingleAsync()).ReviewCount);
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("good")]
    [InlineData("99")]
    [InlineData("")]
    public async Task Review_InvalidOutcomeDoesNotCreateAttempt(string outcome)
    {
        await using var db = await Database.Create();
        var card = await db.SeedCard();
        await Assert.ThrowsAsync<BadRequestException>(() => db.Service.Review(card.Id, new ReviewCardRequest { Outcome = outcome }));
        Assert.Empty(await db.Context.Set<LearningAttempt>().ToListAsync());
    }

    [Fact]
    public async Task Review_MissingCardOrSchedulerFailureDoesNotPersistEvidence()
    {
        await using var db = await Database.Create();
        await Assert.ThrowsAsync<NotFoundException>(() => db.Service.Review(404, new ReviewCardRequest { Outcome = "Good" }));
        var card = await db.SeedCard();
        var service = new CardService(new CardRepository(db.Context), new ObjectMapper(), new ThrowingScheduler());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Review(card.Id, new ReviewCardRequest { Outcome = "Good" }));
        db.Context.ChangeTracker.Clear();
        Assert.Empty(await db.Context.Set<LearningAttempt>().ToListAsync());
        Assert.Equal(2, (await db.Context.Cards.SingleAsync()).ReviewCount);
    }

    [Fact]
    public async Task Review_FailedSaveRollsBackBothSrsAndHistory()
    {
        await using var db = await Database.Create();
        var card = await db.SeedCard();
        db.Context.FailSaves = true;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Good" }));
        db.Context.FailSaves = false;
        db.Context.ChangeTracker.Clear();
        Assert.Empty(await db.Context.Set<LearningAttempt>().ToListAsync());
        var stored = await db.Context.Cards.SingleAsync();
        Assert.Equal(2, stored.ReviewCount);
        Assert.Equal(card.DueDate, stored.DueDate);
        Assert.Empty(await db.Context.Set<LearnerCardState>().ToListAsync());
    }

    [Fact]
    public async Task Review_DatabaseRejectingAttemptInsertCannotCommitSrsUpdate()
    {
        await using var db = await Database.Create();
        var card = await db.SeedCard();
        await db.Context.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_attempt BEFORE INSERT ON LearningAttempts BEGIN SELECT RAISE(ABORT, 'injected ledger failure'); END");
        await Assert.ThrowsAsync<DbUpdateException>(() => db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Good" }));
        db.Context.ChangeTracker.Clear();
        Assert.Empty(await db.Context.Set<LearningAttempt>().ToListAsync());
        Assert.Empty(await db.Context.Set<LearnerCardState>().ToListAsync());
        var unchanged = await db.Context.Cards.SingleAsync();
        Assert.Equal(card.DueDate, unchanged.DueDate);
        Assert.Equal(2, unchanged.ReviewCount);
        Assert.Equal(3, unchanged.IntervalDays);
    }

    [Fact]
    public async Task Review_RetrySameRequestDoesNotRepeatSchedulerOrLedger()
    {
        await using var db = await Database.Create();
        var card = await db.SeedCard();
        var request = new ReviewCardRequest { Outcome = "Again", RequestId = Guid.NewGuid() };
        var first = await db.Service.Review(card.Id, request);
        db.Context.ChangeTracker.Clear();
        var retry = await db.Service.Review(card.Id, request);
        Assert.Equal(first.ReviewCount, retry.ReviewCount);
        Assert.Equal(first.DueDate, retry.DueDate);
        Assert.Single(await db.Context.Set<LearningAttempt>().ToListAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(600000)]
    public async Task Review_TelemetryBoundaryValuesAreAccepted(int milliseconds)
    {
        await using var db = await Database.Create();
        var card = await db.SeedCard();
        await db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Good", ResponseTimeMs = milliseconds });
        Assert.Equal(milliseconds, (await db.Context.LearningAttempts.SingleAsync()).ResponseTimeMs);
    }

    [Fact]
    public async Task RetryIdentifier_ReusedForDifferentOutcomeIsAConflict()
    {
        await using var db = await Database.Create();
        var card = await db.SeedCard();
        var requestId = Guid.NewGuid();
        await db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Good", RequestId = requestId });
        await Assert.ThrowsAsync<ConflictException>(() => db.Service.Review(card.Id,
            new ReviewCardRequest { Outcome = "Easy", RequestId = requestId }));
        Assert.Single(await db.Context.LearningAttempts.ToListAsync());
    }

    [Fact]
    public async Task WordEdit_ResetsCurrentEvidenceRevisionWithoutDeletingHistoricalAttempts()
    {
        await using var db = await Database.Create();
        var card = await db.SeedCard();
        await db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Easy" });
        var attempt = await db.Context.LearningAttempts.AsNoTracking().SingleAsync();
        var word = await db.Context.Words.SingleAsync();
        var service = new WordEditService(db.Service, new WordService(new WordRepository(db.Context), new ObjectMapper()), new LearningRepository(db.Context));
        await service.Update(card.Id, word.Id, new WordUpdateDto { Value = "stream", Translation = "поток", Transcription = "stream" });
        db.Context.ChangeTracker.Clear();
        var reset = await db.Context.Cards.SingleAsync();
        Assert.Equal(0, reset.ReviewCount);
        Assert.True(reset.LearningRevision > attempt.LearningRevision);
        Assert.Equal(attempt.Id, (await db.Context.LearningAttempts.SingleAsync()).Id);
        var learning = new LearningService(new LearningRepository(db.Context), new ObjectMapper(), new LearnerStateCalculator());
        Assert.Equal("New", (await learning.State(card.Id)).Status);
        Assert.Equal(0, (await learning.State(card.Id)).AttemptsCount);
        Assert.Equal(1, (await learning.Attempts(card.Id)).TotalCount);
    }

    [Fact]
    public async Task LostModelCache_IsRebuiltFromImmutableEvidenceWithoutChangingSchedule()
    {
        await using var db = await Database.Create();
        var card = await db.SeedCard();
        await db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Again" });
        await db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Good" });
        var schedule = await db.Context.Cards.AsNoTracking().SingleAsync();
        db.Context.LearnerCardStates.Remove(await db.Context.LearnerCardStates.SingleAsync());
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();
        var service = new LearningService(new LearningRepository(db.Context), new ObjectMapper(), new LearnerStateCalculator());
        var rebuilt = await service.Rebuild(card.Id, DateTimeOffset.UtcNow);
        Assert.Equal(2, rebuilt.AttemptsCount);
        Assert.Equal(1, rebuilt.SuccessfulAttempts);
        Assert.Equal(1, rebuilt.FailedAttempts);
        Assert.Equal(ReviewOutcome.Good, rebuilt.LastOutcome);
        Assert.Equal("ObservedWithLegacyBaseline", rebuilt.Source);
        Assert.Equal(2, await db.Context.LearningAttempts.CountAsync());
        Assert.Equal(schedule.DueDate, (await db.Context.Cards.AsNoTracking().SingleAsync()).DueDate);
        Assert.Equal(schedule.ReviewCount, (await db.Context.Cards.AsNoTracking().SingleAsync()).ReviewCount);
    }

    [Fact]
    public async Task Reset_PreservesEvidenceAndStartsNewLearningRevision()
    {
        await using var db = await Database.Create();
        var card = await db.SeedCard();
        await db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Easy" });
        var original = await db.Context.Set<LearningAttempt>().AsNoTracking().SingleAsync();
        await db.Service.Reset(card.Id);
        db.Context.ChangeTracker.Clear();
        var reset = await db.Context.Cards.SingleAsync();
        Assert.Equal(0, reset.ReviewCount);
        Assert.Equal(0, reset.IntervalDays);
        Assert.True(reset.LearningRevision > original.LearningRevision);
        var retained = await db.Context.Set<LearningAttempt>().SingleAsync();
        Assert.Equal(original.Id, retained.Id);
        Assert.Equal(original.NewDueDate, retained.NewDueDate);
        var calculated = new LearnerStateCalculator().Calculate(reset, [retained], DateTimeOffset.UtcNow);
        Assert.Equal(LearnerStatus.New, calculated.Status);
        Assert.Equal(0, calculated.AttemptsCount);
    }

    [Fact]
    public async Task SoftDeleteCard_RetainsContentFreeHistory()
    {
        await using var db = await Database.Create();
        var card = await db.SeedCard();
        await db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Good" });
        await db.Service.Remove(card.Id);
        db.Context.ChangeTracker.Clear();
        Assert.Empty(await db.Context.Cards.ToListAsync());
        Assert.Single(await db.Context.Set<LearningAttempt>().ToListAsync());
        Assert.DoesNotContain(typeof(LearningAttempt).GetProperties(), p => p.Name is "Word" or "Words" or "Translation" or "Value");
    }

    private sealed class ThrowingScheduler : ISpacedRepetitionScheduler
    {
        public void ApplyReview(Card card, ReviewOutcome outcome, DateTimeOffset reviewedAt) => throw new InvalidOperationException("scheduler failed");
        public void Reset(Card card, DateTimeOffset resetAt) => throw new NotSupportedException();
    }

    internal sealed class Database : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public TestContext Context { get; }
        public CardService Service => new(new CardRepository(Context), new ObjectMapper(), new SpacedRepetitionScheduler());
        private Database(SqliteConnection connection, TestContext context) { this.connection = connection; Context = context; }
        public static async Task<Database> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new TestContext(connection);
            await context.Database.EnsureCreatedAsync();
            return new Database(connection, context);
        }
        public async Task<Card> SeedCard()
        {
            var now = DateTimeOffset.UtcNow;
            var card = new Card
            {
                Collection = new Collection { Name = "learning", PublicId = Guid.NewGuid(), CreatedAt = now, Cards = [] },
                Words = [new Word { Value = "river", Translation = "река", CreatedAt = now }],
                CreatedAt = now.AddDays(-5), DueDate = now.AddDays(-1), LastReviewedAt = now.AddDays(-4),
                ReviewCount = 2, IntervalDays = 3, EaseFactor = 2.5m
            };
            Context.Cards.Add(card);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return card;
        }
        public async ValueTask DisposeAsync() { await Context.DisposeAsync(); await connection.DisposeAsync(); }
    }

    internal sealed class TestContext(SqliteConnection connection) : WordsDbContext(null!)
    {
        public bool FailSaves { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite(connection);
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => FailSaves ? throw new DbUpdateException("Injected persistence failure") : base.SaveChangesAsync(cancellationToken);
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // SQLite supports SQL ordering/comparison on UTC ticks, preserving production semantics.
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

using LearnWord.BL.Mapping;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.BL.Services;
using LearnWord.DAL.Models;
using LearnWord.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnWord.BL.Tests;

public class LearningSessionPlannerTests
{
    [Fact]
    public async Task Today_FavorsDueThenWeakThenNewWithoutDuplicates_AndDoesNotMutate()
    {
        await using var db = await LearningHistoryTests.Database.Create();
        var collection = await SeedCollection(db);
        var future = await Seed(db, collection.Id, due: DateTimeOffset.UtcNow.AddDays(5));
        var overdue = await Seed(db, collection.Id, due: DateTimeOffset.UtcNow.AddDays(-3));
        var weakDue = await Seed(db, collection.Id, due: DateTimeOffset.UtcNow.AddDays(-2), weak: true);
        var weakFuture = await Seed(db, collection.Id, due: DateTimeOffset.UtcNow.AddDays(2), weak: true);
        var fresh = await Seed(db, collection.Id, fresh: true);
        var service = Create(db);
        var beforeCards = await db.Context.Cards.AsNoTracking().Select(c => new { c.Id, c.DueDate, c.ReviewCount }).ToListAsync();
        var plan = await service.Today(collection.Id);
        Assert.Equal(new[] { overdue.Id, weakDue.Id, weakFuture.Id, fresh.Id }, plan.Cards.Select(c => c.Id));
        Assert.Equal(2, plan.Groups.Due);
        Assert.Equal(1, plan.Groups.Weak);
        Assert.Equal(1, plan.Groups.New);
        Assert.Equal(4, plan.TotalCount);
        Assert.Equal(4, plan.Cards.Select(c => c.Id).Distinct().Count());
        Assert.DoesNotContain(plan.Cards, c => c.Id == future.Id);
        Assert.Equal(beforeCards, await db.Context.Cards.AsNoTracking().Select(c => new { c.Id, c.DueDate, c.ReviewCount }).ToListAsync());
        Assert.Empty(await db.Context.LearningSessions.ToListAsync());
        Assert.Empty(await db.Context.LearningAttempts.ToListAsync());
    }

    [Fact]
    public async Task Today_DueWorkCannotBeStarvedByWeakOrNewAndLimitsAreEnforced()
    {
        await using var db = await LearningHistoryTests.Database.Create();
        var collection = await SeedCollection(db);
        var dueIds = new List<int>();
        for (int i = 0; i < LearningService.SessionLimit + 3; i++)
            dueIds.Add((await Seed(db, collection.Id, due: DateTimeOffset.UtcNow.AddDays(-2))).Id);
        for (int i = 0; i < 7; i++)
        {
            await Seed(db, collection.Id, due: DateTimeOffset.UtcNow.AddDays(2), weak: true);
            await Seed(db, collection.Id, fresh: true);
        }
        var plan = await Create(db).Today(collection.Id);
        Assert.Equal(LearningService.SessionLimit, plan.TotalCount);
        Assert.Equal(LearningService.SessionLimit, plan.Groups.Due);
        Assert.Equal(0, plan.Groups.Weak);
        Assert.Equal(0, plan.Groups.New);
        Assert.All(plan.Cards, card => Assert.Contains(card.Id, dueIds));
    }

    [Fact]
    public async Task Today_WeakAndNewCapsApplyAndReviewedUnlearntCardIsNotNew()
    {
        await using var db = await LearningHistoryTests.Database.Create();
        var collection = await SeedCollection(db);
        for (int i = 0; i < 9; i++)
        {
            await Seed(db, collection.Id, due: DateTimeOffset.UtcNow.AddDays(2), weak: true);
            await Seed(db, collection.Id, fresh: true);
        }
        var reviewed = await Seed(db, collection.Id, due: DateTimeOffset.UtcNow.AddDays(5));
        reviewed.Learnt = false;
        await db.Context.SaveChangesAsync();
        var plan = await Create(db).Today(collection.Id);
        Assert.Equal(LearningService.WeakLimit, plan.Groups.Weak);
        Assert.Equal(LearningService.NewLimit, plan.Groups.New);
        Assert.DoesNotContain(plan.Cards, c => c.Id == reviewed.Id);
    }

    [Fact]
    public async Task Sessions_ReviewAssociationCountersPlanMembershipAndDoubleCompletion()
    {
        await using var db = await LearningHistoryTests.Database.Create();
        var collection = await SeedCollection(db);
        var first = await Seed(db, collection.Id, fresh: true);
        var second = await Seed(db, collection.Id, fresh: true);
        var unplanned = await Seed(db, collection.Id, due: DateTimeOffset.UtcNow.AddDays(50));
        var service = Create(db);
        var session = await service.Start(collection.Id);
        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Null(session.CompletedAt);
        Assert.Equal(2, session.PlannedCardsCount);
        Assert.Equal(0, session.CompletedCardsCount);
        Assert.Equal(2, session.Cards.Count);
        await Assert.ThrowsAsync<BadRequestException>(() => db.Service.Review(unplanned.Id,
            new ReviewCardRequest { Outcome = "Good", SessionId = session.Id }));
        await db.Service.Review(first.Id, new ReviewCardRequest { Outcome = "Again", SessionId = session.Id });
        await Assert.ThrowsAsync<ConflictException>(() => db.Service.Review(first.Id,
            new ReviewCardRequest { Outcome = "Good", SessionId = session.Id }));
        await db.Service.Review(second.Id, new ReviewCardRequest { Outcome = "Hard", SessionId = session.Id });
        var done = await service.Complete(session.Id);
        Assert.NotNull(done.CompletedAt);
        Assert.Equal(0, done.CompletedAt.Value.Ticks % 10); // PostgreSQL stores microsecond precision.
        db.Context.ChangeTracker.Clear(); // A second HTTP request reads persisted data.
        var twice = await service.Complete(session.Id);
        Assert.Equal(done.CompletedAt, twice.CompletedAt);
        Assert.Equal(2, done.CompletedCardsCount);
        Assert.Equal(1, done.SuccessfulReviews);
        Assert.Equal(1, done.FailedReviews);
        Assert.All(await db.Context.LearningAttempts.ToListAsync(), a => Assert.Equal(session.Id, a.SessionId));
        await Assert.ThrowsAsync<ConflictException>(() => db.Service.Review(second.Id,
            new ReviewCardRequest { Outcome = "Easy", SessionId = session.Id }));
    }

    [Fact]
    public async Task Sessions_AbandonedOrExpiredSessionDoesNotBlockStartingAgain()
    {
        await using var db = await LearningHistoryTests.Database.Create();
        var collection = await SeedCollection(db);
        var card = await Seed(db, collection.Id, fresh: true);
        var service = Create(db);
        var abandoned = await service.Start(collection.Id);
        var record = await db.Context.LearningSessions.SingleAsync(s => s.Id == abandoned.Id);
        record.StartedAt = DateTimeOffset.UtcNow.AddHours(-LearningService.SessionLifetimeHours - 1);
        await db.Context.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() => db.Service.Review(card.Id,
            new ReviewCardRequest { Outcome = "Good", SessionId = abandoned.Id }));
        var next = await service.Start(collection.Id);
        Assert.NotEqual(abandoned.Id, next.Id);
        await db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Good", SessionId = next.Id });
        Assert.Single(await db.Context.LearningAttempts.ToListAsync());
    }

    [Fact]
    public async Task ResetDuringSession_RejectsOldPlannedContentRevision()
    {
        await using var db = await LearningHistoryTests.Database.Create();
        var collection = await SeedCollection(db);
        var card = await Seed(db, collection.Id, fresh: true);
        var session = await Create(db).Start(collection.Id);
        await db.Service.Reset(card.Id);
        await Assert.ThrowsAsync<BadRequestException>(() => db.Service.Review(card.Id,
            new ReviewCardRequest { Outcome = "Good", SessionId = session.Id }));
        Assert.Empty(await db.Context.LearningAttempts.ToListAsync());
    }

    [Fact]
    public async Task SummaryAndHistory_ExcludeDeletedContentButRetainContentFreeEvidence()
    {
        await using var db = await LearningHistoryTests.Database.Create();
        var collection = await SeedCollection(db);
        var card = await Seed(db, collection.Id, fresh: true);
        var service = Create(db);
        await db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Again" });
        var before = await service.Summary(collection.Id);
        Assert.Equal(1, before.Weak);
        Assert.Equal(1, before.AttemptsCount);
        Assert.Equal(1, before.RecentAttemptsCount);
        await db.Service.Remove(card.Id);
        var after = await service.Summary(collection.Id);
        Assert.Equal(0, after.AttemptsCount);
        Assert.Equal(0, after.TotalCardsStudied);
        await Assert.ThrowsAsync<NotFoundException>(() => service.Attempts(card.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.State(card.Id));
        Assert.Single(await db.Context.LearningAttempts.ToListAsync());
    }

    [Fact]
    public async Task History_ValidatesPaginationAndReturnsActualNewestFirstEvidence()
    {
        await using var db = await LearningHistoryTests.Database.Create();
        var collection = await SeedCollection(db);
        var card = await Seed(db, collection.Id, fresh: true);
        await db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Again" });
        await db.Service.Review(card.Id, new ReviewCardRequest { Outcome = "Good" });
        var service = Create(db);
        var newest = await service.Attempts(card.Id, 1, 1);
        var older = await service.Attempts(card.Id, 2, 1);
        Assert.Equal(2, newest.TotalCount);
        Assert.Equal("Good", Assert.Single(newest.Items).Outcome);
        Assert.Equal("Again", Assert.Single(older.Items).Outcome);
        await Assert.ThrowsAsync<BadRequestException>(() => service.Attempts(card.Id, 0, 1));
        await Assert.ThrowsAsync<BadRequestException>(() => service.Attempts(card.Id, 1, 101));
    }

    private static LearningService Create(LearningHistoryTests.Database db) => new(new LearningRepository(db.Context), new ObjectMapper(), new LearnerStateCalculator());
    private static async Task<Collection> SeedCollection(LearningHistoryTests.Database db)
    {
        var collection = new Collection { Name = "Planner", PublicId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow, Cards = [] };
        db.Context.Collections.Add(collection);
        await db.Context.SaveChangesAsync();
        return collection;
    }
    private static async Task<Card> Seed(LearningHistoryTests.Database db, int collectionId, DateTimeOffset? due = null, bool weak = false, bool fresh = false)
    {
        var now = DateTimeOffset.UtcNow;
        var card = new Card
        {
            CollectionId = collectionId, CreatedAt = now.AddDays(-5), DueDate = due ?? now,
            ReviewCount = fresh ? 0 : 2, LastReviewedAt = fresh ? null : now.AddDays(-1),
            IntervalDays = fresh ? 0 : 3, EaseFactor = 2.5m, Learnt = !fresh && !weak,
            Words = [new Word { Value = "river", Translation = "река", CreatedAt = now }]
        };
        db.Context.Cards.Add(card);
        await db.Context.SaveChangesAsync();
        if (weak)
        {
            db.Context.LearnerCardStates.Add(new LearnerCardState
            {
                CardId = card.Id, AttemptsCount = 2, FailedAttempts = 2, ConsecutiveFailures = 2,
                LastOutcome = ReviewOutcome.Again, LastAttemptAt = now.AddDays(-1), Status = LearnerStatus.Weak
            });
            await db.Context.SaveChangesAsync();
        }
        return card;
    }
}

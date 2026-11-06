using System.Text.Json;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Services;
using LearnWord.DAL.Models;
using LearnWord.WebApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace LearnWord.WebApi.Tests;
public class CoachCandidateRankerTests
{
    private static CoachCardContext Card(int id, params string[] outcomes) => new() {
        CardId = id, CurrentLearnerState = new() { Status = "Learning" },
        SessionAttempt = new() { Outcome = outcomes.Last() },
        RecentAttempts = outcomes.Select((o, i) => new CoachAttemptContext { Outcome = o,
            ReviewedAt = DateTimeOffset.UnixEpoch.AddMinutes(i) }).ToList() };
    [Theory]
    [InlineData("Again", "Again", CoachSeverityCategory.RepeatedFailure)]
    [InlineData("Good", "Again", CoachSeverityCategory.Regression)]
    [InlineData("Again", "Good", CoachSeverityCategory.Other)]
    [InlineData("Hard", "Hard", CoachSeverityCategory.RepeatedHard)]
    public void CategoriesFollowChronology(string first, string last, CoachSeverityCategory category)
        => Assert.Equal(category, CoachCandidateRanker.Evaluate(Card(1, first, last)).Category);
    [Fact]
    public void MissingTimingAndHistoricalRevisionDoNotAddSeverity()
    {
        var card = Card(1, "Hard");
        card.RecentAttempts.Add(new() { Outcome = "Again", LearningRevision = 1, ResponseTimeMs = 20000 });
        card.SessionAttempt.ResponseTimeMs = -1;
        var signals = CoachCandidateRanker.Evaluate(card);
        Assert.Equal(0, signals.AgainCount); Assert.Equal(0, signals.SlowCount); Assert.Null(signals.ResponseTimeMs);
        Assert.Equal(CoachSeverityCategory.Other, signals.Category);
    }
    [Fact]
    public void RankingIsStableAndSeverityOutranksIds()
    {
        var cards = Enumerable.Range(1, 10).Select(i => i > 5 ? Card(i, "Again", "Again") : Card(i, "Good", "Hard")).ToArray();
        var expected = new[] { 6, 7, 8, 9, 10 };
        Assert.Equal(expected, CoachCandidateRanker.Rank(cards).Take(5).Select(c => c.Card.CardId));
        Assert.Equal(expected, CoachCandidateRanker.Rank(cards.Reverse()).Take(5).Select(c => c.Card.CardId));
        Assert.Equal(4, CoachCandidateRanker.Rank(cards.Take(4)).Count());
        var slow = Card(0, "Good"); slow.SessionAttempt.ResponseTimeMs = 20000;
        Assert.Equal(10, CoachCandidateRanker.Rank(new[] { slow, cards[9] }).First().Card.CardId);
    }
    [Fact]
    public void OldSnapshotsDeserialize()
    {
        var row = JsonSerializer.Deserialize<CoachDiagnosticCardDto>("{\"CardId\":1,\"SelectionReason\":\"Included\"}")!;
        Assert.Null(row.SeverityCategory); Assert.Null(row.SeverityRank); Assert.Null(row.SeveritySignals);
    }
    [Fact]
    public async Task TenSessionMembersRankBeforeLimitWithoutWrites()
    {
        await using var db = await CoachTestDatabase.Create();
        var first = await db.Seed();
        for (var i = 0; i < 9; i++)
        {
            var next = await db.Seed();
            var card = await db.Context.Cards.SingleAsync(c => c.Id == next.Card.Id);
            card.CollectionId = first.Card.CollectionId;
            db.Context.LearningSessionCards.Add(new() { SessionId = first.Session.Id, CardId = card.Id, Position = i + 1 });
            var attempts = await db.Context.LearningAttempts.Where(a => a.CardId == card.Id).ToListAsync();
            attempts.Single(a => a.SessionId != null).SessionId = first.Session.Id;
            if (i < 4) foreach (var a in attempts) { a.Outcome = ReviewOutcome.Hard; a.ResponseTimeMs = null; }
            await db.Context.SaveChangesAsync(); db.Context.ChangeTracker.Clear();
        }
        var firstAttempts = await db.Context.LearningAttempts.Where(a => a.CardId == first.Card.Id).ToListAsync();
        foreach (var a in firstAttempts) { a.Outcome = ReviewOutcome.Hard; a.ResponseTimeMs = null; }
        await db.Context.SaveChangesAsync(); db.Context.ChangeTracker.Clear();
        var saves = db.SaveCalls;
        var result = await new CoachContextBuilder(db.Context, new LearnerStateCalculator()).BuildWithDiagnostics(first.Session.Id, "en");
        Assert.Equal(saves, db.SaveCalls); Assert.Equal(5, result.Context.Cards.Count);
        Assert.Equal(5, result.DiagnosticsContext.CandidateCount);
        Assert.Equal(5, result.DiagnosticsContext.Cards.Count(c => c.SelectionReason == "CandidateLimit"));
        Assert.Contains(result.Context.Cards, c => c.CardId == 10);
        Assert.DoesNotContain(result.Context.Cards, c => c.CardId == 2);
        foreach (var row in result.DiagnosticsContext.Cards)
            Assert.Equal(row.SeverityRank <= 5, result.Context.Cards.Any(c => c.CardId == row.CardId));
        Assert.DoesNotContain("Severity", JsonSerializer.Serialize(result.Context));
    }
}

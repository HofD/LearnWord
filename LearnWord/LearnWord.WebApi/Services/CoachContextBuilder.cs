using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.BL.Services;
using LearnWord.DAL;
using LearnWord.DAL.Models;
using Microsoft.EntityFrameworkCore;
namespace LearnWord.WebApi.Services;

public class CoachContextBuilder
{
    private readonly WordsDbContext db;
    private readonly LearnerStateCalculator calculator;
    public CoachContextBuilder(WordsDbContext db, LearnerStateCalculator calculator) { this.db = db; this.calculator = calculator; }
    public async Task<CoachSessionContext> Build(Guid sessionId, string language)
    {
        await using var snapshot = db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL"
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead)
            : await db.Database.BeginTransactionAsync();
        var session = await db.LearningSessions.AsNoTracking().Include(s => s.Cards).SingleOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new NotFoundException("Session not found.", "session_not_found");
        if (!await db.Collections.AnyAsync(c => c.Id == session.CollectionId)) throw new NotFoundException("Collection not found.", "collection_not_found");
        if (!session.CompletedAt.HasValue) throw new ConflictException("Complete the session first.", "coach_session_incomplete");
        var end = session.CompletedAt.Value;
        var context = new CoachSessionContext { Language = language, CompletedAt = end, Summary = new() {
            Planned = session.PlannedCardsCount, Completed = session.CompletedCardsCount,
            Successful = session.SuccessfulReviews, Failed = session.FailedReviews } };
        // Membership is already bounded by the deterministic planner; no unrelated cards enter this query.
        var ids = session.Cards.Select(c => c.CardId).ToArray();
        var cards = await db.Cards.AsNoTracking().Where(c => ids.Contains(c.Id) && c.CollectionId == session.CollectionId)
            .OrderBy(c => c.Id).ToListAsync();
        foreach (var card in cards)
        {
            var member = session.Cards.Single(c => c.CardId == card.Id);
            if (member.LearningRevision != card.LearningRevision) { context.RevisionMismatchCards++; continue; }
            var query = db.LearningAttempts.AsNoTracking().Where(a => a.CardId == card.Id && a.ReviewedAt <= end);
            var current = query.Where(a => a.LearningRevision == card.LearningRevision);
            var count = await current.CountAsync();
            var sessionAttempt = await current.SingleOrDefaultAsync(a => a.SessionId == sessionId);
            // One isolated mistake cannot justify paid intervention. At least two current-revision observations required.
            if (count < 2 || sessionAttempt == null || (sessionAttempt.Outcome != ReviewOutcome.Again && sessionAttempt.Outcome != ReviewOutcome.Hard && sessionAttempt.ResponseTimeMs is not >= 15000)) continue;
            var recent = await current.OrderByDescending(a => a.ReviewedAt).ThenByDescending(a => a.Id).Take(CoachLimits.RecentAttempts).ToListAsync();
            // Keep current aggregate explicitly current; the list of observed attempts is anchored to completion.
            var cached = await db.LearnerCardStates.AsNoTracking().SingleOrDefaultAsync(s => s.CardId == card.Id);
            var state = cached == null || cached.LearningRevision != card.LearningRevision ? calculator.Calculate(card, recent, DateTimeOffset.UtcNow) : calculator.Refresh(card, cached, DateTimeOffset.UtcNow);
            if (cached == null || cached.LearningRevision != card.LearningRevision) state.Source = "BoundedObserved";
            var words = await db.Words.AsNoTracking().Where(w => w.CardId == card.Id).OrderBy(w => w.Id).Take(CoachLimits.Words + 1).ToListAsync();
            // Do not truncate a card into misleading content. Oversized cards are omitted with an evidence gap.
            if (words.Count == 0 || words.Count > CoachLimits.Words || words.Any(w => string.IsNullOrWhiteSpace(w.Value) || string.IsNullOrWhiteSpace(w.Translation) || w.Value.Length > CoachLimits.ShortText || w.Translation.Length > CoachLimits.ShortText)) { context.ContentOmittedCards++; continue; }
            context.Cards.Add(new CoachCardContext { CardId = card.Id, LearningRevision = card.LearningRevision,
                CurrentContent = words.Select(w => new CoachWordContext { Value = w.Value, Translation = w.Translation }).ToList(),
                CurrentRevisionEvidenceCount = count, HistoricalRevisionEvidenceCount = await query.CountAsync(a => a.LearningRevision != card.LearningRevision),
                CurrentLearnerState = new LearnerCardStateDto { CardId = card.Id, LearningRevision = card.LearningRevision, Status = state.Status.ToString(),
                    MasteryScore = state.MasteryScore, DifficultyScore = state.DifficultyScore, AttemptsCount = state.AttemptsCount,
                    ConsecutiveSuccesses = state.ConsecutiveSuccesses, ConsecutiveFailures = state.ConsecutiveFailures,
                    LastOutcome = state.LastOutcome?.ToString(), LastAttemptAt = state.LastAttemptAt, Source = state.Source, ModelVersion = state.ModelVersion,
                    SuccessfulAttempts = state.SuccessfulAttempts, FailedAttempts = state.FailedAttempts },
                SessionAttempt = Attempt(sessionAttempt), RecentAttempts = recent.Select(Attempt).ToList() });
            if (context.Cards.Count == CoachLimits.Cards) break;
        }
        await snapshot.CommitAsync();
        return context;
    }
    private static CoachAttemptContext Attempt(LearningAttempt a) => new() { Id = a.Id, Outcome = a.Outcome.ToString(), ReviewedAt = a.ReviewedAt,
        ResponseTimeMs = a.ResponseTimeMs, LearningRevision = a.LearningRevision };
}

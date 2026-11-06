using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.BL.Services;
using LearnWord.DAL;
using LearnWord.DAL.Models;
using Microsoft.EntityFrameworkCore;
namespace LearnWord.WebApi.Services;

// This envelope is never serialized into the model prompt. Context remains the existing strict model contract.
public class CoachContextBuildResult
{
    public CoachSessionContext Context { get; set; } = new();
    public CoachDiagnosticContextDto DiagnosticsContext { get; set; } = new();
}
public class CoachContextBuilder
{
    private readonly WordsDbContext db;
    private readonly LearnerStateCalculator calculator;
    public CoachContextBuilder(WordsDbContext db, LearnerStateCalculator calculator) { this.db = db; this.calculator = calculator; }
    public async Task<CoachSessionContext> Build(Guid sessionId, string language)
        => (await BuildWithDiagnostics(sessionId, language)).Context;
    public async Task<CoachContextBuildResult> BuildWithDiagnostics(Guid sessionId, string language, bool requireCompleted = true)
    {
        await using var snapshot = db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL"
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead)
            : await db.Database.BeginTransactionAsync();
        var session = await db.LearningSessions.AsNoTracking().Include(s => s.Cards).SingleOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new NotFoundException("Session not found.", "session_not_found");
        if (!await db.Collections.AnyAsync(c => c.Id == session.CollectionId)) throw new NotFoundException("Collection not found.", "collection_not_found");
        if (!session.CompletedAt.HasValue && requireCompleted) throw new ConflictException("Complete the session first.", "coach_session_incomplete");
        var end = session.CompletedAt ?? DateTimeOffset.UtcNow;
        var context = new CoachSessionContext { Language = language, CompletedAt = end, Summary = new() {
            Planned = session.PlannedCardsCount, Completed = session.CompletedCardsCount,
            Successful = session.SuccessfulReviews, Failed = session.FailedReviews } };
        var diagnostic = new CoachDiagnosticContextDto { Planned = session.PlannedCardsCount, Completed = session.CompletedCardsCount,
            Successful = session.SuccessfulReviews, Failed = session.FailedReviews };
        // Membership is planner-bounded; CardId here only makes the safety bound stable.
        var members = session.Cards.OrderBy(c => c.CardId).Take(LearningService.SessionLimit).ToList();
        var ids = members.Select(c => c.CardId).ToArray();
        var cards = await db.Cards.AsNoTracking().Where(c => ids.Contains(c.Id) && c.CollectionId == session.CollectionId).ToListAsync();
        var candidates = new List<(CoachCardContext Card, CoachDiagnosticCardDto Row)>();
        foreach (var member in members)
        {
            var card = cards.SingleOrDefault(c => c.Id == member.CardId);
            var row = new CoachDiagnosticCardDto { CardId = member.CardId, SessionRevision = member.LearningRevision, CurrentRevision = card?.LearningRevision };
            diagnostic.Cards.Add(row);
            if (card == null) { diagnostic.ContentOmittedCards++; continue; }
            var query = db.LearningAttempts.AsNoTracking().Where(a => a.CardId == card.Id && a.ReviewedAt <= end);
            var current = query.Where(a => a.LearningRevision == card.LearningRevision);
            row.CurrentRevisionEvidenceCount = await current.CountAsync();
            row.HistoricalRevisionEvidenceCount = await query.CountAsync(a => a.LearningRevision != card.LearningRevision);
            var sessionAttempt = await query.SingleOrDefaultAsync(a => a.SessionId == sessionId);
            row.SessionOutcome = sessionAttempt?.Outcome.ToString(); row.SessionResponseTimeMs = sessionAttempt?.ResponseTimeMs;
            // Bounded metadata for every planned active card; never read unbounded history into memory.
            var recent = await current.OrderByDescending(a => a.ReviewedAt).ThenByDescending(a => a.Id).Take(CoachLimits.RecentAttempts).ToListAsync();
            row.RecentAttempts = recent.Select(a => new CoachDiagnosticAttemptDto { Outcome = a.Outcome.ToString(), ReviewedAt = a.ReviewedAt,
                ResponseTimeMs = a.ResponseTimeMs, LearningRevision = a.LearningRevision }).ToList();
            var cached = await db.LearnerCardStates.AsNoTracking().SingleOrDefaultAsync(s => s.CardId == card.Id);
            var state = cached == null || cached.LearningRevision != card.LearningRevision
                ? calculator.Calculate(card, recent, end) : calculator.Refresh(card, cached, end);
            if (cached == null || cached.LearningRevision != card.LearningRevision) state.Source = "BoundedObserved";
            row.Status = state.Status.ToString(); row.MasteryScore = state.MasteryScore; row.Source = state.Source; row.ModelVersion = state.ModelVersion;
            if (member.LearningRevision != card.LearningRevision)
            { row.SelectionReason = "RevisionMismatch"; diagnostic.RevisionMismatchCards++; continue; }
            if (row.CurrentRevisionEvidenceCount < 2) { row.SelectionReason = "InsufficientEvidence"; continue; }
            if (sessionAttempt == null || sessionAttempt.LearningRevision != card.LearningRevision) { row.SelectionReason = "NoSessionAttempt"; continue; }
            if (sessionAttempt.Outcome != ReviewOutcome.Again && sessionAttempt.Outcome != ReviewOutcome.Hard && sessionAttempt.ResponseTimeMs is not >= 15000)
            { row.SelectionReason = "SessionNotProblematic"; continue; }
            var words = await db.Words.AsNoTracking().Where(w => w.CardId == card.Id).OrderBy(w => w.Id).Take(CoachLimits.Words + 1).ToListAsync();
            if (words.Count == 0 || words.Any(w => string.IsNullOrWhiteSpace(w.Value) || string.IsNullOrWhiteSpace(w.Translation)))
            { row.SelectionReason = "ContentUnavailable"; diagnostic.ContentOmittedCards++; continue; }
            if (words.Count > CoachLimits.Words || words.Any(w => w.Value.Length > CoachLimits.ShortText || w.Translation.Length > CoachLimits.ShortText))
            { row.SelectionReason = "ContentTooLarge"; diagnostic.ContentOmittedCards++; continue; }
            candidates.Add((new CoachCardContext { CardId = card.Id, LearningRevision = card.LearningRevision,
                CurrentContent = words.Select(w => new CoachWordContext { Value = w.Value, Translation = w.Translation }).ToList(),
                CurrentRevisionEvidenceCount = row.CurrentRevisionEvidenceCount, HistoricalRevisionEvidenceCount = row.HistoricalRevisionEvidenceCount,
                CurrentLearnerState = new LearnerCardStateDto { CardId = card.Id, LearningRevision = card.LearningRevision, Status = state.Status.ToString(),
                    MasteryScore = state.MasteryScore, DifficultyScore = state.DifficultyScore, AttemptsCount = state.AttemptsCount,
                    ConsecutiveSuccesses = state.ConsecutiveSuccesses, ConsecutiveFailures = state.ConsecutiveFailures,
                    LastOutcome = state.LastOutcome?.ToString(), LastAttemptAt = state.LastAttemptAt, Source = state.Source, ModelVersion = state.ModelVersion,
                    SuccessfulAttempts = state.SuccessfulAttempts, FailedAttempts = state.FailedAttempts },
                SessionAttempt = Attempt(sessionAttempt), RecentAttempts = recent.Select(Attempt).ToList() }, row));
        }
        var ranked = CoachCandidateRanker.Rank(candidates.Select(c => c.Card)).ToList();
        for (var i = 0; i < ranked.Count; i++)
        {
            var item = ranked[i];
            var row = candidates.Single(c => c.Card.CardId == item.Card.CardId).Row;
            row.SeverityCategory = item.Signals.Category.ToString();
            row.SeverityRank = i + 1;
            row.SeveritySignals = item.Signals;
            row.SelectionReason = i < CoachLimits.Cards ? "Included" : "CandidateLimit";
            if (i < CoachLimits.Cards) context.Cards.Add(item.Card);
        }
        diagnostic.CandidateCount = context.Cards.Count;
        context.ContentOmittedCards = diagnostic.ContentOmittedCards;
        context.RevisionMismatchCards = diagnostic.RevisionMismatchCards;
        await snapshot.CommitAsync();
        return new CoachContextBuildResult { Context = context, DiagnosticsContext = diagnostic };
    }
    private static CoachAttemptContext Attempt(LearningAttempt a) => new() { Id = a.Id, Outcome = a.Outcome.ToString(), ReviewedAt = a.ReviewedAt,
        ResponseTimeMs = a.ResponseTimeMs, LearningRevision = a.LearningRevision };
}

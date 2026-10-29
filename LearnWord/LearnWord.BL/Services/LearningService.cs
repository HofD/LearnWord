using LearnWord.BL.Abstractions;
using LearnWord.BL.Mapping;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.DAL.Models;
using LearnWord.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LearnWord.BL.Services;

public class LearningService
{
    public const int SessionLimit = 20;
    public const int WeakLimit = 5;
    public const int NewLimit = 5;
    public const int MaxResponseTimeMs = 600000;
    public const int SessionLifetimeHours = 24;
    private readonly LearningRepository repository;
    private readonly ObjectMapper mapper;
    private readonly LearnerStateCalculator calculator;
    public LearningService(LearningRepository repository, ObjectMapper mapper, LearnerStateCalculator calculator)
    { this.repository = repository; this.mapper = mapper; this.calculator = calculator; }
    private IQueryable<Card> ActiveCards => repository.Context.Cards.Where(c => c.Collection.DeletedAt == null);
    private static bool IsNew(Card c) => c.ReviewCount == 0 && c.LastReviewedAt == null && c.IntervalDays == 0 && !c.Learnt;

    public async Task<TodayPlanDto> Today(int collectionId)
    {
        await RequireCollection(collectionId);
        var now = UtcNow();
        var query = ActiveCards.AsNoTracking().Where(c => c.CollectionId == collectionId && c.Words.Any());
        var due = await query.Where(c => (c.ReviewCount > 0 || c.LastReviewedAt != null || c.IntervalDays > 0 || c.Learnt) && c.DueDate <= now)
            .OrderBy(c => c.DueDate).ThenBy(c => c.Id).Take(SessionLimit).Include(c => c.Words).ToListAsync();
        var ids = due.Select(c => c.Id).ToArray();
        var weak = await query.Where(c => !ids.Contains(c.Id) && repository.Context.LearnerCardStates.Any(s => s.CardId == c.Id
                && s.LearningRevision == c.LearningRevision && s.Status == LearnerStatus.Weak))
            .OrderBy(c => c.Id).Take(Math.Min(WeakLimit, SessionLimit - due.Count)).Include(c => c.Words).ToListAsync();
        ids = ids.Concat(weak.Select(c => c.Id)).ToArray();
        var fresh = await query.Where(c => !ids.Contains(c.Id) && c.ReviewCount == 0 && c.LastReviewedAt == null && c.IntervalDays == 0 && !c.Learnt)
            .OrderBy(c => c.Id).Take(Math.Min(NewLimit, SessionLimit - due.Count - weak.Count)).Include(c => c.Words).ToListAsync();
        var cards = due.Concat(weak).Concat(fresh).Select(mapper.Map<CardDto>).ToList();
        return new TodayPlanDto { Cards = cards, TotalCount = cards.Count, EstimatedMinutes = (int)Math.Ceiling(cards.Count * .5),
            Groups = new TodayGroupsDto { Due = due.Count, Weak = weak.Count, New = fresh.Count } };
    }

    public async Task<LearnerSummaryDto> Summary(int collectionId)
    {
        await RequireCollection(collectionId);
        var now = UtcNow();
        // One compact current-state query, no words or historical rows. Time-dependent scores are refreshed in memory.
        var rows = await (from card in ActiveCards.AsNoTracking().Where(c => c.CollectionId == collectionId)
                          join state in repository.Context.LearnerCardStates.AsNoTracking() on card.Id equals state.CardId into states
                          from state in states.DefaultIfEmpty()
                          select new { Card = card, State = state }).ToListAsync();
        var statesNow = rows.Select(x => calculator.Refresh(x.Card, x.State, now)).ToList();
        var attempts = repository.Context.LearningAttempts.Where(a => ActiveCards.Any(c => c.Id == a.CardId && c.CollectionId == collectionId));
        var since = now.AddDays(-7);
        return new LearnerSummaryDto {
            New = statesNow.Count(s => s.Status == LearnerStatus.New), Learning = statesNow.Count(s => s.Status == LearnerStatus.Learning),
            Weak = statesNow.Count(s => s.Status == LearnerStatus.Weak), Strong = statesNow.Count(s => s.Status == LearnerStatus.Strong),
            TotalCardsStudied = rows.Count(x => !IsNew(x.Card)), AttemptsCount = await attempts.CountAsync(),
            RecentAttemptsCount = await attempts.CountAsync(a => a.ReviewedAt >= since)
        };
    }

    public async Task<LearnerCardStateDto> State(int cardId)
    {
        var card = await RequireCard(cardId);
        var cached = await repository.Context.LearnerCardStates.AsNoTracking().SingleOrDefaultAsync(s => s.CardId == cardId);
        var now = UtcNow();
        var s = cached == null
            ? calculator.Calculate(card, await repository.Context.LearningAttempts.AsNoTracking().Where(a => a.CardId == cardId).ToListAsync(), now)
            : calculator.Refresh(card, cached, now);
        return new LearnerCardStateDto { CardId = cardId, MasteryScore = s.MasteryScore, DifficultyScore = s.DifficultyScore,
            AttemptsCount = s.AttemptsCount, SuccessfulAttempts = s.SuccessfulAttempts, FailedAttempts = s.FailedAttempts,
            ConsecutiveSuccesses = s.ConsecutiveSuccesses, ConsecutiveFailures = s.ConsecutiveFailures,
            LastOutcome = s.LastOutcome?.ToString(), LastAttemptAt = s.LastAttemptAt, Status = s.Status.ToString(),
            Source = s.Source, ModelVersion = s.ModelVersion, LearningRevision = s.LearningRevision };
    }

    public async Task<PagedResult<LearningAttemptDto>> Attempts(int cardId, int page = 1, int pageSize = 50)
    {
        await RequireCard(cardId);
        if (page < 1 || pageSize < 1 || pageSize > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new BadRequestException("Invalid history pagination.", "invalid_pagination");
        var query = repository.Context.LearningAttempts.AsNoTracking().Where(a => a.CardId == cardId);
        var count = await query.CountAsync();
        var rows = await query.OrderByDescending(a => a.ReviewedAt).ThenByDescending(a => a.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<LearningAttemptDto> { Page = page, PageSize = pageSize, TotalCount = count,
            Items = rows.Select(a => new LearningAttemptDto { Id = a.Id, CardId = a.CardId, SessionId = a.SessionId,
                Outcome = a.Outcome.ToString(), ReviewedAt = a.ReviewedAt, PreviousIntervalDays = a.PreviousIntervalDays,
                NewIntervalDays = a.NewIntervalDays, PreviousEaseFactor = a.PreviousEaseFactor, NewEaseFactor = a.NewEaseFactor,
                PreviousDueDate = a.PreviousDueDate, NewDueDate = a.NewDueDate, ResponseTimeMs = a.ResponseTimeMs,
                LearningRevision = a.LearningRevision }).ToList() };
    }

    public async Task<LearningSessionDto> Start(int collectionId)
    {
        // Freeze content/revisions across the plan and its durable membership snapshot.
        await using var tx = repository.Context.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL"
            ? await repository.Context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead)
            : await repository.Context.Database.BeginTransactionAsync();
        var plan = await Today(collectionId);
        var ids = plan.Cards.Select(c => c.Id).ToArray();
        var revisions = await ActiveCards.Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.LearningRevision);
        var session = new LearningSession { Id = Guid.NewGuid(), CollectionId = collectionId, StartedAt = UtcNow(),
            PlannedCardsCount = plan.TotalCount };
        session.Cards = ids.Select((id, position) => new LearningSessionCard { SessionId = session.Id, CardId = id,
            Position = position, LearningRevision = revisions[id] }).ToList();
        repository.Context.LearningSessions.Add(session);
        await repository.SaveChangesAsync();
        await tx.CommitAsync();
        return SessionDto(session, plan.Cards);
    }

    public async Task<LearningSessionDto> Session(Guid id)
    {
        var session = await RequireSession(id);
        var plannedIds = session.Cards.OrderBy(c => c.Position).Select(c => c.CardId).ToArray();
        var cards = await ActiveCards.AsNoTracking().Where(c => plannedIds.Contains(c.Id)).Include(c => c.Words).ToListAsync();
        return SessionDto(session, plannedIds.Where(id => cards.Any(c => c.Id == id)).Select(id => mapper.Map<CardDto>(cards.Single(c => c.Id == id))).ToList());
    }

    public async Task<LearningSessionDto> Complete(Guid id)
    {
        await using var tx = await repository.BeginMutation(sessionId: id);
        var session = await RequireSession(id);
        session.CompletedAt ??= UtcNow();
        await repository.SaveChangesAsync();
        await tx.CommitAsync();
        return SessionDto(session, new());
    }

    public async Task<CardDto> Review(int id, ReviewCardRequest request, ISpacedRepetitionScheduler scheduler)
    {
        if (request == null || !Enum.TryParse<ReviewOutcome>(request.Outcome, false, out var outcome) || !Enum.IsDefined(outcome) || outcome.ToString() != request.Outcome)
            throw new BadRequestException("Outcome must be one of Again, Hard, Good, or Easy.", "invalid_review_outcome");
        if (request.ResponseTimeMs is < 0 or > MaxResponseTimeMs)
            throw new BadRequestException("responseTimeMs must be between 0 and 600000.", "invalid_response_time");
        if (request.RequestId == Guid.Empty || request.SessionId == Guid.Empty)
            throw new BadRequestException("Identifiers cannot be empty.", "invalid_review_identifier");
        await using var tx = await repository.BeginMutation(id, request.SessionId);
        var card = await RequireCard(id, true);
        if (request.RequestId.HasValue)
        {
            var existing = await repository.Context.LearningAttempts.SingleOrDefaultAsync(a => a.CardId == id && a.RequestId == request.RequestId);
            if (existing != null)
            {
                if (existing.Outcome != outcome || existing.SessionId != request.SessionId || existing.ResponseTimeMs != request.ResponseTimeMs)
                    throw new ConflictException("Review request identifier was reused for different input.", "review_request_conflict");
                await tx.CommitAsync();
                return mapper.Map<CardDto>(card);
            }
        }
        var now = UtcNow();
        LearningSession? session = null;
        if (request.SessionId.HasValue)
        {
            session = await RequireSession(request.SessionId.Value);
            if (session.CollectionId != card.CollectionId || !session.Cards.Any(c => c.CardId == id && c.LearningRevision == card.LearningRevision))
                throw new BadRequestException("Card is not in the current session plan.", "session_card_mismatch");
            if (session.CompletedAt.HasValue || session.StartedAt.AddHours(SessionLifetimeHours) <= now)
                throw new ConflictException("Learning session is closed or expired.", "session_closed");
            if (await repository.Context.LearningAttempts.AnyAsync(a => a.SessionId == session.Id && a.CardId == id))
                throw new ConflictException("Card has already been reviewed in this session.", "session_card_already_reviewed");
        }
        var attempt = new LearningAttempt { Id = Guid.NewGuid(), CardId = id, SessionId = request.SessionId,
            RequestId = request.RequestId, Outcome = outcome, ReviewedAt = now, ResponseTimeMs = request.ResponseTimeMs,
            LearningRevision = card.LearningRevision, PreviousIntervalDays = card.IntervalDays,
            PreviousEaseFactor = card.EaseFactor, PreviousDueDate = card.DueDate };
        scheduler.ApplyReview(card, outcome, now);
        attempt.NewIntervalDays = card.IntervalDays;
        attempt.NewEaseFactor = card.EaseFactor;
        attempt.NewDueDate = card.DueDate;
        var cached = await repository.Context.LearnerCardStates.SingleOrDefaultAsync(s => s.CardId == id);
        var baseline = cached;
        if (baseline == null)
            baseline = calculator.Calculate(card, await repository.Context.LearningAttempts.AsNoTracking()
                .Where(a => a.CardId == id).ToListAsync(), now);
        var calculated = calculator.Apply(card, baseline, attempt, now);
        if (cached == null) repository.Context.LearnerCardStates.Add(calculated);
        else repository.Context.Entry(cached).CurrentValues.SetValues(calculated);
        repository.Context.LearningAttempts.Add(attempt);
        if (session != null)
        {
            session.CompletedCardsCount++;
            if (outcome == ReviewOutcome.Again) session.FailedReviews++; else session.SuccessfulReviews++;
        }
        // Card, evidence, model cache and session counters commit together in this database transaction.
        await repository.SaveChangesAsync();
        await tx.CommitAsync();
        return mapper.Map<CardDto>(card);
    }

    // Internal recovery/migration operation, deliberately not exposed by the gateway.
    // Replays one card's evidence and atomically replaces its disposable aggregate.
    public async Task<LearnerCardState> Rebuild(int cardId, DateTimeOffset now)
    {
        await using var tx = await repository.BeginMutation(cardId);
        var card = await RequireCard(cardId);
        var attempts = await repository.Context.LearningAttempts.AsNoTracking().Where(a => a.CardId == cardId).ToListAsync();
        var state = calculator.Calculate(card, attempts, now);
        var cached = await repository.Context.LearnerCardStates.SingleOrDefaultAsync(s => s.CardId == cardId);
        if (cached == null) repository.Context.LearnerCardStates.Add(state);
        else repository.Context.Entry(cached).CurrentValues.SetValues(state);
        await repository.SaveChangesAsync();
        await tx.CommitAsync();
        return state;
    }

    // PostgreSQL stores microseconds. Return exactly the timestamp precision we persist.
    private static DateTimeOffset UtcNow()
    {
        var now = DateTimeOffset.UtcNow;
        return new DateTimeOffset(now.Ticks - now.Ticks % 10, TimeSpan.Zero);
    }

    private async Task RequireCollection(int id)
    {
        if (!await repository.Context.Collections.AnyAsync(c => c.Id == id)) throw new NotFoundException("Collection not found.", "collection_not_found");
    }
    private async Task<Card> RequireCard(int id, bool words = false)
    {
        var query = ActiveCards.AsQueryable();
        if (words) query = query.Include(c => c.Words);
        return await query.SingleOrDefaultAsync(c => c.Id == id) ?? throw new NotFoundException("Card not found.", "card_not_found");
    }
    private async Task<LearningSession> RequireSession(Guid id)
    {
        var session = await repository.Context.LearningSessions.Include(s => s.Cards).SingleOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException("Session not found.", "session_not_found");
        await RequireCollection(session.CollectionId);
        return session;
    }
    private static LearningSessionDto SessionDto(LearningSession s, List<CardDto> cards) => new()
    {
        Id = s.Id, CollectionId = s.CollectionId, StartedAt = s.StartedAt, CompletedAt = s.CompletedAt,
        PlannedCardsCount = s.PlannedCardsCount, CompletedCardsCount = s.CompletedCardsCount,
        SuccessfulReviews = s.SuccessfulReviews, FailedReviews = s.FailedReviews, Cards = cards
    };
}

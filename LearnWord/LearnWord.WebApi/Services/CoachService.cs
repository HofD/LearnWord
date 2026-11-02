using System.Diagnostics;
using System.Text.Json;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.DAL;
using LearnWord.DAL.Models;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
namespace LearnWord.WebApi.Services;

public class CoachService
{
    private readonly WordsDbContext db;
    private readonly CoachContextBuilder contextBuilder;
    private readonly IAiCoachProvider provider;
    private readonly AiCoachOptions options;
    private readonly ILogger<CoachService> logger;
    public CoachService(WordsDbContext db, CoachContextBuilder contextBuilder, IAiCoachProvider provider, IOptions<AiCoachOptions> options, ILogger<CoachService> logger)
    { this.db = db; this.contextBuilder = contextBuilder; this.provider = provider; this.options = options.Value; this.logger = logger; }
    public async Task<CoachAnalysisDto> Analyze(Guid sessionId, AnalyzeCoachRequest request)
    {
        if (request == null || request.Language is not ("en" or "ru")) throw new BadRequestException("Invalid coach language.", "coach_invalid_language");
        if (!options.Enabled)
        {
            var disabledSession = await RequireSession(sessionId);
            if (!disabledSession.CompletedAt.HasValue) throw new ConflictException("Complete the session first.", "coach_session_incomplete");
            return Disabled(disabledSession);
        }
        var now = Now();
        CoachAnalysis analysis;
        await using (var claim = await db.Database.BeginTransactionAsync())
        {
            await Lock("LearningSessions", sessionId);
            var session = await RequireSession(sessionId);
            if (!session.CompletedAt.HasValue) throw new ConflictException("Complete the session first.", "coach_session_incomplete");
            analysis = await db.CoachAnalyses.SingleOrDefaultAsync(a => a.SessionId == sessionId && a.PromptVersion == CoachLimits.PromptVersion)
                ?? new CoachAnalysis { Id = Guid.NewGuid(), SessionId = sessionId, CollectionId = session.CollectionId, CreatedAt = now, Language = request.Language,
                    PromptVersion = CoachLimits.PromptVersion, SchemaVersion = CoachLimits.SchemaVersion };
            if (analysis.Generation > 0)
            {
                if (analysis.Status == "Completed" || analysis.Status == "Pending" && analysis.UpdatedAt.AddSeconds(CoachLimits.LeaseSeconds) > now
                    || analysis.RetryCount >= CoachLimits.MaxRetries || analysis.Status == "Failed" && analysis.UpdatedAt.AddSeconds(CoachLimits.RetrySeconds) > now)
                { await claim.CommitAsync(); return await MapAnalysis(analysis); }
                if (analysis.Status != "Disabled") analysis.RetryCount++;
            }
            else db.CoachAnalyses.Add(analysis);
            analysis.Status = "Pending";
            analysis.Generation++; analysis.UpdatedAt = now; analysis.FailureCode = null;
            analysis.DiagnosticJson = null; // Never expose an earlier retry as the current generation snapshot.
            analysis.Provider = SafeProvenance(provider.Provider); analysis.Model = SafeProvenance(provider.Model);
            await db.SaveChangesAsync(); await claim.CommitAsync();
        }
        var generation = analysis.Generation;
        var elapsed = Stopwatch.StartNew();
        CoachDiagnosticsDto? diagnostic = null;
        try
        {
            var built = await contextBuilder.BuildWithDiagnostics(sessionId, analysis.Language);
            var context = built.Context;
            var inputCharacters = JsonSerializer.Serialize(context, CoachOutputValidator.Json).Length;
            diagnostic = new CoachDiagnosticsDto { SessionId = sessionId, AnalysisId = analysis.Id, Mode = "CapturedAtAnalysis", CapturedAt = Now(),
                Context = built.DiagnosticsContext, Analysis = DiagnosticAnalysis(analysis, "Pending"), ProviderCalled = false, InputCharacters = inputCharacters };
            // Persist the selected evidence metadata before invoking the provider. The generation fence
            // prevents an interrupted/superseded worker from replacing the current retry's snapshot.
            if (!await CaptureDiagnostic(analysis.Id, generation, diagnostic)) return await Analysis(sessionId);
            CoachAnalysisResult result;
            if (context.Cards.Count == 0) result = new CoachAnalysisResult { Decision = "None" };
            else
            {
                if (inputCharacters > CoachLimits.PromptCharacters - 8000)
                    throw CoachFailure.WithDetails(new UpstreamServiceException("AI Coach context is too large.", "coach_context_too_large"), new("ContextSize"));
                diagnostic.ProviderCalled = true;
                if (!await CaptureDiagnostic(analysis.Id, generation, diagnostic)) return await Analysis(sessionId);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
                var output = await provider.Analyze(context, timeout.Token).WaitAsync(timeout.Token);
                diagnostic.OutputCharacters = output?.Length;
                result = CoachOutputValidator.ParseAndValidate(output!, context);
            }
            db.ChangeTracker.Clear();
            await using var save = await db.Database.BeginTransactionAsync();
            await Lock("CoachAnalyses", analysis.Id);
            var current = await db.CoachAnalyses.SingleAsync(a => a.Id == analysis.Id);
            if (current.Generation != generation || current.Status != "Pending") { await save.CommitAsync(); return await MapAnalysis(current); }
            await RequireSession(sessionId);
            // Revalidate revisions under the same card locks used by ordinary content edits/resets.
            foreach (var c in context.Cards.OrderBy(c => c.CardId))
            {
                await LockCard(c.CardId);
                if (!await db.Cards.AnyAsync(card => card.Id == c.CardId && card.LearningRevision == c.LearningRevision && card.Collection.DeletedAt == null))
                    throw CoachFailure.WithDetails(new UpstreamServiceException("Coach context changed during analysis.", "coach_context_stale"), new("ContextRevalidation"));
            }
            current.Decision = result.Decision; current.Summary = result.Summary;
            // Keep validated grounding internally; public DTO intentionally removes attempt identifiers.
            current.ObservationsJson = JsonSerializer.Serialize(result.Observations, CoachOutputValidator.Json);
            foreach (var i in result.Interventions)
            {
                foreach (var e in i.Exercises) e.Id = Guid.NewGuid();
                db.CoachInterventions.Add(new CoachIntervention { Id = Guid.NewGuid(), AnalysisId = current.Id, CardId = i.CardId,
                    LearningRevision = i.LearningRevision, Type = i.Type, Title = i.Title, CreatedAt = Now(), PayloadJson = JsonSerializer.Serialize(i, CoachOutputValidator.Json) });
            }
            current.Status = "Completed"; current.UpdatedAt = Now();
            diagnostic.Analysis = DiagnosticAnalysis(current, diagnostic.ProviderCalled == true ? "Provider" : "NoCandidates");
            diagnostic.DurationMs = elapsed.ElapsedMilliseconds;
            current.DiagnosticJson = SerializeDiagnostic(diagnostic);
            await db.SaveChangesAsync(); await save.CommitAsync();
            LogDiagnostic(diagnostic);
            return await MapAnalysis(current);
        }
        catch (Exception ex) when (ex is UpstreamServiceException or HttpRequestException or OperationCanceledException)
        {
            var code = ex is UpstreamServiceException api ? api.ErrorCode : "ai_provider_unavailable";
            // Never log untrusted output, card content, auth headers or provider error bodies.
            var details = CoachFailure.Details(ex) ?? (ex is OperationCanceledException ? new CoachFailureDetails("AnalysisTimeout") : ex is HttpRequestException ? new CoachFailureDetails("ProviderNetwork") : null);
            logger.LogWarning("Coach analysis {AnalysisId} generation {Generation} failed with {FailureCode}; stage={FailureStage} rule={ValidationCode} path={ValidationPath} HTTP={ProviderHttpStatus}.", analysis.Id, generation, code, details?.FailureStage, details?.ValidationCode, details?.ValidationPath, details?.ProviderHttpStatus);
            db.ChangeTracker.Clear();
            await using var fail = await db.Database.BeginTransactionAsync();
            await Lock("CoachAnalyses", analysis.Id);
            var current = await db.CoachAnalyses.SingleAsync(a => a.Id == analysis.Id);
            if (current.Generation == generation && current.Status == "Pending")
            {
                current.Status = "Failed"; current.FailureCode = code; current.UpdatedAt = Now();
                if (diagnostic != null)
                {
                    diagnostic.Analysis = DiagnosticAnalysis(current, "Failed");
                    diagnostic.Analysis.FailureStage = details?.FailureStage;
                    diagnostic.Analysis.ValidationCode = details?.ValidationCode;
                    diagnostic.Analysis.ValidationPath = details?.ValidationPath;
                    diagnostic.Analysis.ProviderHttpStatus = details?.ProviderHttpStatus;
                    diagnostic.DurationMs = elapsed.ElapsedMilliseconds;
                    current.DiagnosticJson = SerializeDiagnostic(diagnostic);
                }
                await db.SaveChangesAsync();
            }
            await fail.CommitAsync();
            if (diagnostic != null && current.Generation == generation) LogDiagnostic(diagnostic);
            return await MapAnalysis(current);
        }
    }
    public async Task<CoachAnalysisDto> Analysis(Guid sessionId)
    {
        var session = await RequireSession(sessionId);
        if (!options.Enabled) return Disabled(session);
        var a = await db.CoachAnalyses.AsNoTracking().SingleOrDefaultAsync(a => a.SessionId == sessionId && a.PromptVersion == CoachLimits.PromptVersion)
            ?? throw new NotFoundException("Coach analysis not found.", "coach_analysis_not_found");
        return await MapAnalysis(a);
    }
    public async Task<CoachDiagnosticsDto> Diagnostics(Guid sessionId)
    {
        await RequireSession(sessionId);
        // Prefer the current prompt's record, otherwise the latest historical analysis. GET never creates
        // or upgrades an analysis and never invokes a provider, even when the feature is disabled.
        var analyses = db.CoachAnalyses.AsNoTracking().Where(a => a.SessionId == sessionId);
        var analysis = await analyses.SingleOrDefaultAsync(a => a.PromptVersion == CoachLimits.PromptVersion)
            ?? await analyses.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).FirstOrDefaultAsync();
        if (analysis?.DiagnosticJson is string stored)
            return JsonSerializer.Deserialize<CoachDiagnosticsDto>(stored, CoachOutputValidator.Json)!;
        var built = await contextBuilder.BuildWithDiagnostics(sessionId, analysis?.Language ?? "en", requireCompleted: false);
        return new CoachDiagnosticsDto { SessionId = sessionId, AnalysisId = analysis?.Id, Mode = "ReconstructedNow", CapturedAt = Now(), Context = built.DiagnosticsContext,
            Analysis = analysis == null ? new CoachDiagnosticAnalysisDto { Status = options.Enabled ? "NotRequested" : "Disabled", DecisionSource = options.Enabled ? "LegacyUnknown" : "Disabled" }
                : DiagnosticAnalysis(analysis, analysis.Status switch { "Failed" => "Failed", "Pending" => "Pending", "Disabled" => "Disabled", _ => "LegacyUnknown" }) };
    }
    private async Task<bool> CaptureDiagnostic(Guid analysisId, int generation, CoachDiagnosticsDto diagnostic)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync();
        await Lock("CoachAnalyses", analysisId);
        var current = await db.CoachAnalyses.SingleAsync(a => a.Id == analysisId);
        if (current.Generation != generation || current.Status != "Pending") { await tx.CommitAsync(); return false; }
        current.DiagnosticJson = SerializeDiagnostic(diagnostic);
        await db.SaveChangesAsync(); await tx.CommitAsync(); return true;
    }
    private const int MaxDiagnosticCharacters = 48000;
    private static string SerializeDiagnostic(CoachDiagnosticsDto diagnostic)
    {
        var json = JsonSerializer.Serialize(diagnostic, CoachOutputValidator.Json);
        // All fields are already bounded; keep the durable payload hard-bounded even if metadata grows.
        while (json.Length > MaxDiagnosticCharacters && diagnostic.Context.Cards.Any(c => c.RecentAttempts.Count > 0))
        {
            diagnostic.Truncated = true;
            foreach (var card in diagnostic.Context.Cards.Where(c => c.RecentAttempts.Count > 0)) card.RecentAttempts.RemoveAt(card.RecentAttempts.Count - 1);
            json = JsonSerializer.Serialize(diagnostic, CoachOutputValidator.Json);
        }
        if (json.Length > MaxDiagnosticCharacters) throw CoachFailure.WithDetails(new UpstreamServiceException("Coach diagnostic exceeds its safe storage limit.", "coach_diagnostic_too_large"), new("DiagnosticSize"));
        return json;
    }
    private static CoachDiagnosticAnalysisDto DiagnosticAnalysis(CoachAnalysis a, string source) => new()
    {
        Status = a.Status, Decision = a.Decision, DecisionSource = source, Provider = a.Provider, Model = a.Model,
        PromptVersion = a.PromptVersion, SchemaVersion = a.SchemaVersion, FailureCode = a.FailureCode,
        Generation = a.Generation, RetryCount = a.RetryCount
    };
    private void LogDiagnostic(CoachDiagnosticsDto d) => logger.LogInformation(
        "Coach generation completed: AnalysisId={AnalysisId} SessionId={SessionId} Generation={Generation} DecisionSource={DecisionSource} Decision={Decision} CandidateCount={CandidateCount} ProviderCalled={ProviderCalled} DurationMs={DurationMs} InputCharacters={InputCharacters} OutputCharacters={OutputCharacters} FailureCode={FailureCode}",
        d.AnalysisId, d.SessionId, d.Analysis.Generation, d.Analysis.DecisionSource, d.Analysis.Decision, d.Context.CandidateCount,
        d.ProviderCalled, d.DurationMs, d.InputCharacters, d.OutputCharacters, d.Analysis.FailureCode);
    public async Task<int> Scope(Guid id)
    {
        var i = await RequireIntervention(id);
        return await db.CoachAnalyses.Where(a => a.Id == i.AnalysisId).Select(a => a.CollectionId).SingleAsync();
    }
    public async Task<CoachInterventionDto> Intervention(Guid id) => await MapIntervention(await RequireIntervention(id));
    public Task<CoachInterventionDto> Accept(Guid id) => Transition(id, "accept");
    public Task<CoachInterventionDto> Dismiss(Guid id) => Transition(id, "dismiss");
    public Task<CoachInterventionDto> Complete(Guid id) => Transition(id, "complete");
    private async Task<CoachInterventionDto> Transition(Guid id, string action)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await Lock("CoachInterventions", id);
        var i = await RequireIntervention(id); await LockCard(i.CardId); await RequireCurrent(i);
        if (action == "accept")
        {
            if (i.Status == "Suggested") { i.Status = "Accepted"; i.StartedAt = Now(); }
            else if (i.Status is not ("Accepted" or "Completed")) throw Closed();
        }
        else if (action == "dismiss")
        {
            if (i.Status is "Suggested" or "Accepted") i.Status = "Dismissed";
            else if (i.Status != "Dismissed") throw Closed();
        }
        else
        {
            if (i.Type == "ExtraPractice") throw new ConflictException("Answer the exercises to complete practice.", "coach_practice_incomplete");
            if (i.Status == "Accepted") { i.Status = "Completed"; i.CompletedAt = Now(); }
            else if (i.Status != "Completed") throw Closed();
        }
        await db.SaveChangesAsync(); await tx.CommitAsync(); return await MapIntervention(i);
    }
    public async Task<CoachAnswerDto> Answer(Guid id, Guid exerciseId, CoachAnswerRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.OptionId) || request.OptionId.Length > 24 || request.ResponseTimeMs is < 0 or > 600000)
            throw new BadRequestException("Invalid supplementary answer.", "coach_invalid_answer");
        await using var tx = await db.Database.BeginTransactionAsync();
        await Lock("CoachInterventions", id);
        var intervention = await RequireIntervention(id); await LockCard(intervention.CardId); await RequireCurrent(intervention);
        if (intervention.Type != "ExtraPractice") throw new NotFoundException("Exercise not found.", "coach_exercise_not_found");
        var payload = Payload(intervention);
        var exercise = payload.Exercises.SingleOrDefault(e => e.Id == exerciseId) ?? throw new NotFoundException("Exercise not found.", "coach_exercise_not_found");
        if (!exercise.Options.Any(o => o.Id == request.OptionId)) throw new BadRequestException("Unknown option.", "coach_invalid_answer");
        var existing = await db.InterventionAttempts.SingleOrDefaultAsync(a => a.InterventionId == id && a.ExerciseId == exerciseId);
        if (existing != null)
        {
            if (existing.OptionId != request.OptionId || existing.ResponseTimeMs != request.ResponseTimeMs) throw new ConflictException("Exercise has already been answered.", "coach_answer_conflict");
            await tx.CommitAsync();
            return new CoachAnswerDto { ExerciseId = exerciseId, Correct = existing.Correct, Explanation = exercise.Explanation, Intervention = await MapIntervention(intervention) };
        }
        if (intervention.Status != "Accepted") throw Closed();
        var answer = new InterventionAttempt { Id = Guid.NewGuid(), InterventionId = id, ExerciseId = exerciseId, CardId = intervention.CardId,
            SubmittedAt = Now(), Correct = request.OptionId == exercise.CorrectOptionId, OptionId = request.OptionId, ResponseTimeMs = request.ResponseTimeMs };
        db.InterventionAttempts.Add(answer);
        var answered = await db.InterventionAttempts.CountAsync(a => a.InterventionId == id);
        if (answered + 1 == payload.Exercises.Count) { intervention.Status = "Completed"; intervention.CompletedAt = answer.SubmittedAt; }
        // Deliberately writes only InterventionAttempt + intervention lifecycle. No scheduling/history/learner mutation.
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return new CoachAnswerDto { ExerciseId = exerciseId, Correct = answer.Correct, Explanation = exercise.Explanation, Intervention = await MapIntervention(intervention) };
    }
    private async Task<CoachAnalysisDto> MapAnalysis(CoachAnalysis a)
    {
        var result = new CoachAnalysisDto { Id = a.Id, SessionId = a.SessionId, Status = a.Status, Decision = a.Decision,
            Summary = a.Summary, FailureCode = a.FailureCode, Provider = a.Provider, Model = a.Model,
            PromptVersion = a.PromptVersion, SchemaVersion = a.SchemaVersion, CreatedAt = a.CreatedAt };
        var observations = JsonSerializer.Deserialize<List<CoachObservationResult>>(a.ObservationsJson, CoachOutputValidator.Json) ?? new();
        foreach (var o in observations)
            if (await db.Cards.AnyAsync(c => c.Id == o.CardId && c.Collection.DeletedAt == null))
                result.Observations.Add(new CoachObservationDto { CardId = o.CardId, LearningRevision = o.LearningRevision, Type = o.Type, EvidenceSummary = o.EvidenceSummary, Explanation = o.Explanation });
        foreach (var i in await db.CoachInterventions.AsNoTracking().Where(i => i.AnalysisId == a.Id).OrderBy(i => i.CreatedAt).ThenBy(i => i.Id).ToListAsync())
            result.Interventions.Add(await MapIntervention(i));
        return result;
    }
    private async Task<CoachInterventionDto> MapIntervention(CoachIntervention i)
    {
        var card = await db.Cards.AsNoTracking().SingleOrDefaultAsync(c => c.Id == i.CardId && c.Collection.DeletedAt == null);
        var active = card != null && card.LearningRevision == i.LearningRevision;
        var status = card == null ? "Unavailable" : !active && (i.Status is "Suggested" or "Accepted") ? "Stale" : i.Status;
        var dto = new CoachInterventionDto { Id = i.Id, AnalysisId = i.AnalysisId, CardId = i.CardId, LearningRevision = i.LearningRevision,
            Type = i.Type, Title = card == null ? "" : i.Title, Status = status, CreatedAt = i.CreatedAt, StartedAt = i.StartedAt, CompletedAt = i.CompletedAt };
        if (active)
        {
            var payload = Payload(i); dto.Lesson = payload.Lesson;
            var answers = await db.InterventionAttempts.AsNoTracking().Where(x => x.InterventionId == i.Id).ToListAsync();
            dto.Exercises = payload.Exercises.Select(e => { var answer = answers.SingleOrDefault(x => x.ExerciseId == e.Id); return new CoachExerciseDto {
                Id = e.Id, Type = e.Type, Prompt = e.Prompt, Options = e.Options, Answered = answer != null, Correct = answer?.Correct }; }).ToList();
        }
        if (i.CompletedAt.HasValue && active)
        {
            var query = db.LearningAttempts.AsNoTracking().Where(x => x.CardId == i.CardId && x.LearningRevision == i.LearningRevision && x.ReviewedAt > i.CompletedAt.Value);
            var first = await query.OrderBy(x => x.ReviewedAt).ThenBy(x => x.Id).FirstOrDefaultAsync();
            dto.Evaluation = new CoachEvaluationDto { Status = first == null ? "NoFollowUpEvidence" : first.Outcome == ReviewOutcome.Again ? "FollowUpFailure" : "FollowUpSuccess",
                PostInterventionAttempts = await query.CountAsync(), FirstPostInterventionOutcome = first?.Outcome.ToString(), FirstPostInterventionReviewedAt = first?.ReviewedAt };
        }
        return dto;
    }
    private async Task<LearningSession> RequireSession(Guid id)
    {
        var s = await db.LearningSessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id) ?? throw new NotFoundException("Session not found.", "session_not_found");
        if (!await db.Collections.AnyAsync(c => c.Id == s.CollectionId)) throw new NotFoundException("Collection not found.", "collection_not_found"); return s;
    }
    private async Task<CoachIntervention> RequireIntervention(Guid id)
    {
        var i = await db.CoachInterventions.SingleOrDefaultAsync(i => i.Id == id) ?? throw new NotFoundException("Intervention not found.", "coach_intervention_not_found");
        var a = await db.CoachAnalyses.AsNoTracking().SingleAsync(a => a.Id == i.AnalysisId); await RequireSession(a.SessionId); return i;
    }
    private async Task RequireCurrent(CoachIntervention i)
    {
        var revision = await db.Cards.Where(c => c.Id == i.CardId && c.Collection.DeletedAt == null).Select(c => (int?)c.LearningRevision).SingleOrDefaultAsync();
        if (!revision.HasValue) throw new NotFoundException("Intervention content unavailable.", "coach_content_unavailable");
        if (revision != i.LearningRevision) throw new ConflictException("Intervention belongs to an older card revision.", "coach_intervention_stale");
    }
    private static CoachInterventionResult Payload(CoachIntervention i) => JsonSerializer.Deserialize<CoachInterventionResult>(i.PayloadJson, CoachOutputValidator.Json)!;
    private async Task Lock(string table, Guid id)
    {
        if (db.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL") return;
        // table names are exclusively private hardcoded call sites; values remain parameterized.
        if (table == "LearningSessions") await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM words.\"LearningSessions\" WHERE \"Id\" = {id} FOR UPDATE");
        else if (table == "CoachAnalyses") await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM words.\"CoachAnalyses\" WHERE \"Id\" = {id} FOR UPDATE");
        else await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM words.\"CoachInterventions\" WHERE \"Id\" = {id} FOR UPDATE");
    }
    private async Task LockCard(int id)
    { if (db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL") await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM words.\"Cards\" WHERE \"Id\" = {id} FOR UPDATE"); }
    private static CoachAnalysisDto Disabled(LearningSession session) => new()
    {
        SessionId = session.Id, Status = "Disabled", Decision = "None",
        PromptVersion = CoachLimits.PromptVersion, SchemaVersion = CoachLimits.SchemaVersion, CreatedAt = session.CompletedAt ?? session.StartedAt
    };
    private static ConflictException Closed() => new("Intervention is not available for this action.", "coach_intervention_closed");
    private static string SafeProvenance(string value) => (value ?? "").Length <= 200 ? value ?? "" : value[..200];
    private static DateTimeOffset Now() { var n = DateTimeOffset.UtcNow; return new DateTimeOffset(n.Ticks - n.Ticks % 10, TimeSpan.Zero); }
}

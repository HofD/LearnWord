using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.Identity.Abstactions;
using LearnWord.Identity.Controllers;
using LearnWord.Identity.DAL.Context;
using LearnWord.Identity.DAL.Models;
using LearnWord.Identity.DAL.Repositories;
using LearnWord.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnWord.Identity.Tests;

public class CoachIdentityServiceTests
{
    [Fact]
    public async Task ForeignSessionCannotReadAnalyzeOrPayForCoach()
    {
        await using var fixture = await Fixture.Create();
        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.Analyze(fixture.SessionId, new(), "visitor"));
        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.Analysis(fixture.SessionId, "visitor"));
        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.Diagnostics(fixture.SessionId, "visitor"));
        Assert.Equal(0, fixture.Coach.ContentOrMutationCalls);
        Assert.Equal(3, fixture.Learning.SessionCalls);
        Assert.Equal(0, fixture.Coach.DiagnosticsCalls);
    }

    [Fact]
    public async Task ForeignInterventionAndExerciseCannotReadAcceptDismissCompleteOrSubmit()
    {
        await using var fixture = await Fixture.Create();
        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.Intervention(fixture.InterventionId, "visitor"));
        foreach (var action in new[] { "accept", "dismiss", "complete" })
            await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.Transition(fixture.InterventionId, action, "visitor"));
        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.Answer(fixture.InterventionId, fixture.ExerciseId, new() { OptionId = "a" }, "visitor"));
        Assert.Equal(0, fixture.Coach.ContentOrMutationCalls);
        Assert.Equal(5, fixture.Coach.ScopeCalls);
    }

    [Fact]
    public async Task OwnedCoachRequestsForwardOnlyAuthorizedIdsAndExplicitPayload()
    {
        await using var fixture = await Fixture.Create();
        var analysis = await fixture.Service.Analyze(fixture.SessionId, new() { Language = "ru" }, "owner");
        Assert.Equal(fixture.SessionId, analysis.SessionId);
        Assert.Equal("ru", fixture.Coach.Language);
        await fixture.Service.Analysis(fixture.SessionId, "owner");
        await fixture.Service.Intervention(fixture.InterventionId, "owner");
        foreach (var action in new[] { "accept", "dismiss", "complete" })
        {
            await fixture.Service.Transition(fixture.InterventionId, action, "owner");
            Assert.Equal(action, fixture.Coach.Action);
        }
        await fixture.Service.Answer(fixture.InterventionId, fixture.ExerciseId, new() { OptionId = "b", ResponseTimeMs = 350 }, "owner");
        Assert.Equal((fixture.InterventionId, fixture.ExerciseId, "b", (int?)350), fixture.Coach.AnswerPayload);
        Assert.Equal(7, fixture.Coach.ContentOrMutationCalls);
    }

    [Fact]
    public async Task AnonymousControllerActionsReturn401WithoutEvenLookingUpScope()
    {
        await using var fixture = await Fixture.Create();
        var controller = new CoachIdentityController(fixture.Service) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        Assert.IsType<UnauthorizedResult>((await controller.Analyze(fixture.SessionId, new())).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Analysis(fixture.SessionId)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Diagnostics(fixture.SessionId)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Intervention(fixture.InterventionId)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Accept(fixture.InterventionId)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Dismiss(fixture.InterventionId)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Complete(fixture.InterventionId)).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Answer(fixture.InterventionId, fixture.ExerciseId, new())).Result);
        Assert.Equal(0, fixture.Coach.ContentOrMutationCalls);
        Assert.Equal(0, fixture.Coach.ScopeCalls);
        Assert.Equal(0, fixture.Learning.SessionCalls);
        Assert.Equal(0, fixture.Coach.DiagnosticsCalls);
    }

    [Fact]
    public async Task OwnedDiagnosticsOnlyForwardTheAuthorizedSessionAndNeverGenerateOrMutate()
    {
        await using var fixture = await Fixture.Create();
        var result = await fixture.Service.Diagnostics(fixture.SessionId, "owner");
        Assert.Equal(fixture.SessionId, result.SessionId);
        Assert.Equal(1, fixture.Learning.SessionCalls);
        Assert.Equal(1, fixture.Coach.DiagnosticsCalls);
        Assert.Equal(0, fixture.Coach.ContentOrMutationCalls);
        Assert.Equal(0, fixture.Coach.ScopeCalls);
    }

    [Fact]
    public void DiagnosticControllerRouteExplicitlyDisablesCachingOfPrivateEvidence()
    {
        var policy = Assert.Single(typeof(CoachIdentityController).GetMethod(nameof(CoachIdentityController.Diagnostics))!
            .GetCustomAttributes(typeof(ResponseCacheAttribute), inherit: true).Cast<ResponseCacheAttribute>());
        Assert.True(policy.NoStore);
        Assert.Equal(ResponseCacheLocation.None, policy.Location);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly CollectionIdentityDbContext context;
        public Guid SessionId { get; } = Guid.NewGuid();
        public Guid InterventionId { get; } = Guid.NewGuid();
        public Guid ExerciseId { get; } = Guid.NewGuid();
        public CoachUpstream Coach { get; } = new();
        public LearningUpstream Learning { get; } = new();
        public CoachIdentityService Service => new(Coach, Learning, new CollectionIdentityRepository(context));
        private Fixture(SqliteConnection connection, CollectionIdentityDbContext context) { this.connection = connection; this.context = context; }
        public static async Task<Fixture> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new CollectionIdentityDbContext(new DbContextOptionsBuilder<CollectionIdentityDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            context.CollectionIdentities.Add(new CollectionIdentity { CollectionId = 10, UserId = "owner" });
            await context.SaveChangesAsync();
            return new Fixture(connection, context);
        }
        public async ValueTask DisposeAsync() { await context.DisposeAsync(); await connection.DisposeAsync(); }
    }
    private sealed class CoachUpstream : ICoachHttpService
    {
        public int ContentOrMutationCalls { get; private set; }
        public int ScopeCalls { get; private set; }
        public int DiagnosticsCalls { get; private set; }
        public string? Language { get; private set; }
        public string? Action { get; private set; }
        public (Guid, Guid, string, int?) AnswerPayload { get; private set; }
        public Task<CoachAnalysisDto> Analyze(Guid sessionId, AnalyzeCoachRequest request) { ContentOrMutationCalls++; Language = request.Language; return Task.FromResult(new CoachAnalysisDto { SessionId = sessionId }); }
        public Task<CoachAnalysisDto> Analysis(Guid sessionId) { ContentOrMutationCalls++; return Task.FromResult(new CoachAnalysisDto { SessionId = sessionId }); }
        public Task<CoachDiagnosticsDto> Diagnostics(Guid sessionId) { DiagnosticsCalls++; return Task.FromResult(new CoachDiagnosticsDto { SessionId = sessionId }); }
        public Task<int> Scope(Guid id) { ScopeCalls++; return Task.FromResult(10); }
        public Task<CoachInterventionDto> Intervention(Guid id) { ContentOrMutationCalls++; return Task.FromResult(new CoachInterventionDto { Id = id }); }
        public Task<CoachInterventionDto> Transition(Guid id, string action) { ContentOrMutationCalls++; Action = action; return Task.FromResult(new CoachInterventionDto { Id = id }); }
        public Task<CoachAnswerDto> Answer(Guid id, Guid exerciseId, CoachAnswerRequest request) { ContentOrMutationCalls++; AnswerPayload = (id, exerciseId, request.OptionId, request.ResponseTimeMs); return Task.FromResult(new CoachAnswerDto { ExerciseId = exerciseId }); }
    }
    private sealed class LearningUpstream : ILearningHttpService
    {
        public int SessionCalls { get; private set; }
        public Task<LearningSessionDto> Session(Guid sessionId) { SessionCalls++; return Task.FromResult(new LearningSessionDto { Id = sessionId, CollectionId = 10 }); }
        public Task<TodayPlanDto> Today(int id) => throw new NotSupportedException();
        public Task<LearnerSummaryDto> Summary(int id) => throw new NotSupportedException();
        public Task<LearningSessionDto> Start(int id) => throw new NotSupportedException();
        public Task<LearningSessionDto> Complete(Guid id) => throw new NotSupportedException();
        public Task<LearnerCardStateDto> State(int id) => throw new NotSupportedException();
        public Task<PagedResult<LearningAttemptDto>> Attempts(int id, int page, int pageSize) => throw new NotSupportedException();
    }
}

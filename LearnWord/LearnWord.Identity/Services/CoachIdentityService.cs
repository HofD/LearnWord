using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.Identity.Abstactions;
using LearnWord.Identity.DAL.Repositories;
namespace LearnWord.Identity.Services;
public class CoachIdentityService
{
    private readonly ICoachHttpService upstream;
    private readonly ILearningHttpService learning;
    private readonly CollectionIdentityRepository collections;
    public CoachIdentityService(ICoachHttpService upstream, ILearningHttpService learning, CollectionIdentityRepository collections)
    { this.upstream = upstream; this.learning = learning; this.collections = collections; }
    public async Task<CoachAnalysisDto> Analyze(Guid id, AnalyzeCoachRequest request, string userId)
    { await CheckSession(id, userId); return await upstream.Analyze(id, request); }
    public async Task<CoachAnalysisDto> Analysis(Guid id, string userId)
    { await CheckSession(id, userId); return await upstream.Analysis(id); }
    public async Task<CoachInterventionDto> Intervention(Guid id, string userId)
    { await CheckIntervention(id, userId); return await upstream.Intervention(id); }
    public async Task<CoachInterventionDto> Transition(Guid id, string action, string userId)
    { await CheckIntervention(id, userId); return await upstream.Transition(id, action); }
    public async Task<CoachAnswerDto> Answer(Guid id, Guid exerciseId, CoachAnswerRequest request, string userId)
    { await CheckIntervention(id, userId); return await upstream.Answer(id, exerciseId, request); }
    private async Task CheckSession(Guid id, string userId) { var session = await learning.Session(id); await CheckCollection(session.CollectionId, userId); }
    private async Task CheckIntervention(Guid id, string userId) => await CheckCollection(await upstream.Scope(id), userId);
    private async Task CheckCollection(int id, string userId)
    { if (await collections.Get(id, userId) == null) throw new NotFoundException("Coach resource not found.", "coach_not_found"); }
}

using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.Identity.Abstactions;
using LearnWord.Identity.DAL.Repositories;
namespace LearnWord.Identity.Services;

public class LearningIdentityService
{
    private readonly ILearningHttpService upstream;
    private readonly CollectionIdentityRepository collections;
    private readonly CardIdentityRepository cards;
    public LearningIdentityService(ILearningHttpService upstream, CollectionIdentityRepository collections, CardIdentityRepository cards)
    { this.upstream = upstream; this.collections = collections; this.cards = cards; }
    public async Task<TodayPlanDto> Today(int id, string userId)
    { await CheckCollection(id, userId); return await upstream.Today(id); }
    public async Task<LearnerSummaryDto> Summary(int id, string userId)
    { await CheckCollection(id, userId); return await upstream.Summary(id); }
    public async Task<LearningSessionDto> Start(int id, string userId)
    { await CheckCollection(id, userId); return await upstream.Start(id); }
    public async Task<LearningSessionDto> Session(Guid id, string userId)
    { var result = await upstream.Session(id); await CheckCollection(result.CollectionId, userId); return result; }
    public async Task<LearningSessionDto> Complete(Guid id, string userId)
    { await Session(id, userId); return await upstream.Complete(id); }
    public async Task<LearnerCardStateDto> State(int id, string userId)
    { await CheckCard(id, userId); return await upstream.State(id); }
    public async Task<PagedResult<LearningAttemptDto>> Attempts(int id, int page, int pageSize, string userId)
    { await CheckCard(id, userId); return await upstream.Attempts(id, page, pageSize); }
    private async Task CheckCollection(int id, string userId)
    { if (await collections.Get(id, userId) == null) throw new NotFoundException("Collection not found.", "collection_not_found"); }
    private async Task CheckCard(int id, string userId)
    { if (await cards.Get(id, userId) == null) throw new ForbiddenException("Card does not belong to current user.", "card_forbidden"); }
}

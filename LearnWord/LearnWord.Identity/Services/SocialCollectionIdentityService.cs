using System.Security.Cryptography;
using System.Text;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.DAL.Models;
using LearnWord.Identity.Abstactions;
using LearnWord.Identity.DAL.Repositories;

namespace LearnWord.Identity.Services;

public class SocialCollectionIdentityService
{
    private readonly ISocialCollectionsHttpService upstream;
    private readonly CollectionIdentityRepository repository;
    private readonly ILogger<SocialCollectionIdentityService>? logger;

    public SocialCollectionIdentityService(ISocialCollectionsHttpService upstream, CollectionIdentityRepository repository,
        ILogger<SocialCollectionIdentityService>? logger = null)
    { this.upstream = upstream; this.repository = repository; this.logger = logger; }

    public async Task<PagedResult<PublicCollectionSummary>> Explore(int page, int pageSize, string? search)
    {
        ValidatePagination(page, pageSize);
        var result = await upstream.Explore(page, pageSize, search);
        var owners = await repository.GetOwners(result.Items.Select(c => c.CollectionId));
        return new PagedResult<PublicCollectionSummary>
        {
            Page = result.Page, PageSize = result.PageSize, TotalCount = result.TotalCount,
            Items = result.Items.Select(c => new PublicCollectionSummary
            {
                PublicId = c.PublicId, Name = c.Name, CardsCount = c.CardsCount,
                Author = Author(owners.GetValueOrDefault(c.CollectionId))
            }).ToList()
        };
    }

    public async Task<PublicCollectionDetails> Get(Guid publicId, int page, int pageSize, string userId, string? search = null)
    {
        ValidatePagination(page, pageSize);
        var result = await upstream.Get(publicId, page, pageSize, search);
        var owner = await repository.GetOwner(result.CollectionId);
        var isOwner = owner?.UserId == userId;
        CheckAccess(result.Visibility, isOwner);
        result.Details.Author = Author(owner?.UserId);
        result.Details.IsOwner = isOwner;
        result.Details.OwnerCollectionId = isOwner ? result.CollectionId : null;
        return result.Details;
    }

    public async Task<CollectionDto> SetVisibility(int id, CollectionVisibility visibility, string userId)
    {
        if (!Enum.IsDefined(visibility)) throw new BadRequestException("Unknown collection visibility.");
        if (await repository.Get(id, userId) == null) throw Missing();
        return await upstream.SetVisibility(id, visibility);
    }

    public async Task<CollectionDto> Fork(Guid publicId, string userId)
    {
        var source = await upstream.Get(publicId, 1, 1);
        var owner = await repository.GetOwner(source.CollectionId);
        CheckAccess(source.Visibility, owner?.UserId == userId);
        if (owner?.UserId == userId)
            throw new ConflictException("This collection is already yours. Open it in the editor.", "own_collection_fork");

        // The core service rechecks current visibility before copying the source.
        var clone = await upstream.Fork(publicId);
        try
        {
            await repository.LinkFork(clone.Id, clone.Cards.Select(c => c.Id), userId);
        }
        catch
        {
            // Cross-service compensation: a failed link must not leave a visible usable orphan.
            try { await upstream.Remove(clone.Id); }
            catch (Exception cleanupError)
            { logger?.LogError(cleanupError, "Failed to remove incomplete fork {CollectionId} after ownership linking failed.", clone.Id); }
            throw;
        }
        return clone;
    }

    private static void CheckAccess(CollectionVisibility visibility, bool isOwner)
    {
        if (visibility == CollectionVisibility.Private && isOwner) return;
        if (visibility == CollectionVisibility.Public || visibility == CollectionVisibility.Unlisted) return;
        throw Missing();
    }

    private static string Author(string? userId) => string.IsNullOrEmpty(userId) ? "Learner" :
        "Learner " + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId)))[..12].ToLowerInvariant();

    private static NotFoundException Missing() => new("Collection not found.", "collection_not_found");

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 50 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new BadRequestException("Page must be positive and pageSize must be between 1 and 50.", "invalid_pagination");
    }
}

using LearnWord.BL.Mapping;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.DAL.Models;
using LearnWord.DAL.Repositories;

namespace LearnWord.BL.Services;

public class SocialCollectionService
{
    private readonly SocialCollectionRepository repository;
    private readonly ObjectMapper mapper;
    public SocialCollectionService(SocialCollectionRepository repository, ObjectMapper mapper)
    { this.repository = repository; this.mapper = mapper; }

    public Task<PagedResult<SocialCollectionRecord>> Explore(int page, int pageSize, string? search)
    { ValidatePagination(page, pageSize); return repository.Explore(page, pageSize, search ?? string.Empty); }

    public Task<SocialCollectionPage> Get(Guid publicId, int page, int pageSize, string? search = null)
    { ValidatePagination(page, pageSize); return repository.Get(publicId, page, pageSize, search); }

    public async Task<CollectionDto> Fork(Guid publicId) => mapper.Map<CollectionDto>(await repository.Fork(publicId));

    public async Task<CollectionDto> SetVisibility(int id, CollectionVisibility visibility)
    {
        if (!Enum.IsDefined(visibility)) throw new BadRequestException("Unknown collection visibility.");
        return mapper.Map<CollectionDto>(await repository.SetVisibility(id, visibility));
    }

    public static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 50 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new BadRequestException("Page must be positive and pageSize must be between 1 and 50.", "invalid_pagination");
    }
}

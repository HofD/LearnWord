using System.ComponentModel.DataAnnotations;
using LearnWord.DAL.Models;

namespace LearnWord.BL.Models.Dto;

public class CollectionVisibilityDto
{
    [Required]
    [EnumDataType(typeof(CollectionVisibility))]
    public CollectionVisibility? Visibility { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class PublicCollectionSummary
{
    public Guid PublicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int CardsCount { get; set; }
}

public class PublicCollectionDetails : PublicCollectionSummary
{
    public bool IsOwner { get; set; }
    public int? OwnerCollectionId { get; set; }
    public List<PublicCardDto> Cards { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class PublicCardDto
{
    public List<PublicWordDto> Words { get; set; } = [];
}

public class PublicWordDto
{
    public string Value { get; set; } = string.Empty;
    public string Transcription { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
}

// Internal transport only. The authenticated facade emits explicit public DTOs.
public class SocialCollectionRecord : PublicCollectionSummary
{
    public int CollectionId { get; set; }
    public CollectionVisibility Visibility { get; set; }
}

public class SocialCollectionPage
{
    public int CollectionId { get; set; }
    public CollectionVisibility Visibility { get; set; }
    public PublicCollectionDetails Details { get; set; } = new();
}

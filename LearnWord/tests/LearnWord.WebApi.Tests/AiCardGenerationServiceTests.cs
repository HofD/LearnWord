using LearnWord.BL.Abstractions;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Options;
using LearnWord.WebApi.Services;
using Xunit;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace LearnWord.WebApi.Tests;

public class AiCardGenerationServiceTests
{
    [Theory]
    [InlineData("", "ai_source_text_required")]
    [InlineData("   ", "ai_source_text_required")]
    public async Task GenerateCards_MissingSourceText_RejectsBeforeProviderCall(string sourceText, string expectedErrorCode)
    {
        var provider = new RecordingProvider();
        var service = CreateService(provider);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.GenerateCards(1, new AiCardGenerationRequest { SourceText = sourceText, MaxCards = 1 }, CancellationToken.None));

        Assert.Equal(expectedErrorCode, exception.ErrorCode);
        Assert.False(provider.WasCalled);
    }

    [Fact]
    public async Task GenerateCards_SourceTextTooLong_RejectsBeforeProviderCall()
    {
        var provider = new RecordingProvider();
        var service = CreateService(provider, options: new AiCardGenerationOptions { MaxSourceTextLength = 5, MaxCards = 3 });

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.GenerateCards(1, new AiCardGenerationRequest { SourceText = "123456", MaxCards = 1 }, CancellationToken.None));

        Assert.Equal("ai_source_text_too_long", exception.ErrorCode);
        Assert.False(provider.WasCalled);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task GenerateCards_MaxCardsOutOfConfiguredRange_RejectsBeforeProviderCall(int maxCards)
    {
        var provider = new RecordingProvider();
        var service = CreateService(provider, options: new AiCardGenerationOptions { MaxSourceTextLength = 100, MaxCards = 3 });

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.GenerateCards(1, new AiCardGenerationRequest { SourceText = "alpha beta", MaxCards = maxCards }, CancellationToken.None));

        Assert.Equal("ai_max_cards_out_of_range", exception.ErrorCode);
        Assert.False(provider.WasCalled);
    }

    [Fact]
    public async Task GenerateCards_FakeProvider_ReturnsDeterministicDraftSuggestions()
    {
        var service = CreateService(
            new FakeAiCardGenerationProvider(),
            options: new AiCardGenerationOptions { Provider = "Fake", MaxSourceTextLength = 100, MaxCards = 5 });

        var result = await service.GenerateCards(
            1,
            new AiCardGenerationRequest
            {
                SourceText = "Apple apple banana cat.",
                TargetLanguage = "Ukrainian",
                Level = "A2",
                MaxCards = 2
            },
            CancellationToken.None);

        Assert.Collection(
            result.Cards,
            first =>
            {
                Assert.Equal("Apple", first.Value);
                Assert.Equal("[Ukrainian] Apple", first.Translation);
                Assert.Equal("A2", first.Difficulty);
                Assert.Equal("Local fake AI suggestion for development and tests.", first.Explanation);
            },
            second =>
            {
                Assert.Equal("banana", second.Value);
                Assert.Equal("[Ukrainian] banana", second.Translation);
                Assert.Equal("A2", second.Difficulty);
            });
    }

    [Theory]
    [MemberData(nameof(InvalidProviderResponses))]
    public async Task GenerateCards_ProviderResponseMissingValueOrTranslation_IsRejected(AiCardGenerationResponse response)
    {
        var service = CreateService(new RecordingProvider { Response = response });

        var exception = await Assert.ThrowsAsync<UpstreamServiceException>(() =>
            service.GenerateCards(1, new AiCardGenerationRequest { SourceText = "alpha beta", MaxCards = 2 }, CancellationToken.None));

        Assert.Equal("ai_invalid_provider_response", exception.ErrorCode);
    }

    [Fact]
    public async Task GenerateCards_ProviderSuggestionsAlreadyInCollection_AreFilteredAfterProviderResponse()
    {
        var service = CreateService(
            new RecordingProvider
            {
                Response = new AiCardGenerationResponse
                {
                    Cards =
                    [
                        new AiCardSuggestionDto { Value = " apple ", Translation = "apple-translation" },
                        new AiCardSuggestionDto { Value = "BANANA", Translation = "banana-translation" },
                        new AiCardSuggestionDto { Value = "banana", Translation = "banana-repeat" },
                        new AiCardSuggestionDto { Value = "pear", Translation = "pear-translation" }
                    ]
                }
            },
            collectionService: new StubCollectionService
            {
                Collection = new CollectionDto
                {
                    Id = 7,
                    Name = "Food",
                    Cards =
                    [
                        new CardDto
                        {
                            Id = 11,
                            CollectionId = 7,
                            Words =
                            [
                                new WordDto { Id = 13, Value = "Apple", Translation = "apple-translation" }
                            ]
                        }
                    ]
                }
            });

        var result = await service.GenerateCards(
            7,
            new AiCardGenerationRequest { SourceText = "apple banana pear", MaxCards = 4 },
            CancellationToken.None);

        Assert.Collection(
            result.Cards,
            first => Assert.Equal("BANANA", first.Value),
            second => Assert.Equal("pear", second.Value));
    }

    public static TheoryData<AiCardGenerationResponse> InvalidProviderResponses()
    {
        return new TheoryData<AiCardGenerationResponse>
        {
            new()
            {
                Cards =
                [
                    new AiCardSuggestionDto
                    {
                        Value = "",
                        Translation = "translation"
                    }
                ]
            },
            new()
            {
                Cards =
                [
                    new AiCardSuggestionDto
                    {
                        Value = "value",
                        Translation = " "
                    }
                ]
            }
        };
    }

    private static AiCardGenerationService CreateService(
        IAiCardGenerationProvider provider,
        StubCollectionService? collectionService = null,
        AiCardGenerationOptions? options = null)
    {
        return new AiCardGenerationService(
            provider,
            collectionService ?? new StubCollectionService(),
            OptionsFactory.Create(options ?? new AiCardGenerationOptions { MaxSourceTextLength = 100, MaxCards = 5 }));
    }

    private sealed class RecordingProvider : IAiCardGenerationProvider
    {
        public bool WasCalled { get; private set; }

        public AiCardGenerationResponse Response { get; init; } = new()
        {
            Cards =
            [
                new AiCardSuggestionDto
                {
                    Value = "alpha",
                    Translation = "translation",
                    Transcription = " transcription ",
                    Example = " example ",
                    Explanation = " explanation ",
                    Difficulty = " A1 "
                }
            ]
        };

        public Task<AiCardGenerationResponse> GenerateCards(AiCardGenerationRequest request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(Response);
        }
    }

    private sealed class StubCollectionService : ICollectionService
    {
        public CollectionDto Collection { get; init; } = new()
        {
            Id = 1,
            Name = "Default",
            Cards = []
        };

        public Task<CollectionDto> Add(CollectionCreateDto createDto)
        {
            throw new NotImplementedException();
        }

        public Task<CollectionDto> Rename(int id, CollectionRenameDto renameDto)
        {
            throw new NotImplementedException();
        }

        public Task Remove(int id)
        {
            throw new NotImplementedException();
        }

        public Task<CollectionDto> Get(int id)
        {
            return Task.FromResult(Collection);
        }

        public Task<CollectionListDto> GetList(List<int> ids)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CardDto>> GetReviewCards(int collectionId)
        {
            throw new NotImplementedException();
        }
    }
}

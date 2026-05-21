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
            service.GenerateCards(new AiCardGenerationRequest { SourceText = sourceText, MaxCards = 1 }, CancellationToken.None));

        Assert.Equal(expectedErrorCode, exception.ErrorCode);
        Assert.False(provider.WasCalled);
    }

    [Fact]
    public async Task GenerateCards_SourceTextTooLong_RejectsBeforeProviderCall()
    {
        var provider = new RecordingProvider();
        var service = CreateService(provider, new AiCardGenerationOptions { MaxSourceTextLength = 5, MaxCards = 3 });

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.GenerateCards(new AiCardGenerationRequest { SourceText = "123456", MaxCards = 1 }, CancellationToken.None));

        Assert.Equal("ai_source_text_too_long", exception.ErrorCode);
        Assert.False(provider.WasCalled);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task GenerateCards_MaxCardsOutOfConfiguredRange_RejectsBeforeProviderCall(int maxCards)
    {
        var provider = new RecordingProvider();
        var service = CreateService(provider, new AiCardGenerationOptions { MaxSourceTextLength = 100, MaxCards = 3 });

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.GenerateCards(new AiCardGenerationRequest { SourceText = "alpha beta", MaxCards = maxCards }, CancellationToken.None));

        Assert.Equal("ai_max_cards_out_of_range", exception.ErrorCode);
        Assert.False(provider.WasCalled);
    }

    [Fact]
    public async Task GenerateCards_FakeProvider_ReturnsDeterministicDraftSuggestions()
    {
        var service = CreateService(
            new FakeAiCardGenerationProvider(),
            new AiCardGenerationOptions { Provider = "Fake", MaxSourceTextLength = 100, MaxCards = 5 });

        var result = await service.GenerateCards(
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
            service.GenerateCards(new AiCardGenerationRequest { SourceText = "alpha beta", MaxCards = 2 }, CancellationToken.None));

        Assert.Equal("ai_invalid_provider_response", exception.ErrorCode);
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
        AiCardGenerationOptions? options = null)
    {
        return new AiCardGenerationService(
            provider,
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
}

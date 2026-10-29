using LearnWord.BL.Models.Dto;
using Xunit;

namespace LearnWord.WebApi.Tests;

public class CoachBoundaryTests
{
    [Theory]
    [InlineData(typeof(PublicCollectionSummary), "Author,CardsCount,Name,PublicId")]
    [InlineData(typeof(PublicCollectionDetails), "Author,Cards,CardsCount,IsOwner,Name,OwnerCollectionId,Page,PageSize,PublicId,TotalCount")]
    [InlineData(typeof(PublicCardDto), "Words")]
    [InlineData(typeof(PublicWordDto), "Transcription,Translation,Value")]
    public void SocialContracts_KeepExplicitContentOnlyAllowlists(Type type, string expected)
    {
        Assert.Equal(expected, string.Join(",", type.GetProperties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal)));
    }

    [Fact]
    public void PublicExerciseContract_CannotExposeExpectedAnswerOrSolution()
    {
        Assert.Equal(new[] { "Answered", "Correct", "Id", "Options", "Prompt", "Type" },
            typeof(CoachExerciseDto).GetProperties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.Equal(new[] { "Id", "Text" }, typeof(CoachOptionDto).GetProperties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(typeof(AnalyzeCoachRequest))]
    [InlineData(typeof(CoachAnswerRequest))]
    public void CoachRequests_DoNotAcceptUserOwnershipOrSchedulingFields(Type type)
    {
        var forbidden = new[] { "UserId", "OwnerId", "CollectionId", "CardId", "DueDate", "IntervalDays", "EaseFactor", "ReviewCount", "MasteryScore", "LearningRevision", "CorrectAnswer" };
        Assert.DoesNotContain(type.GetProperties(), p => forbidden.Contains(p.Name));
    }
}

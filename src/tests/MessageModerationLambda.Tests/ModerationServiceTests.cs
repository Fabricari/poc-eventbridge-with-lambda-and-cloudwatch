using MessageModerationLambda;
using Xunit;

namespace MessageModerationLambda.Tests;

public class ModerationServiceTests
{
    private readonly ModerationService _service = new();

    [Fact]
    public void CleanText_ReturnsClean()
    {
        var result = _service.Evaluate("this architecture is delightful");

        Assert.Equal(ModerationStatus.Clean, result.Status);
        Assert.Empty(result.MatchedTerms);
        Assert.Equal("this architecture is delightful", result.OriginalText);
    }

    [Fact]
    public void FlaggedText_ReturnsFlagged()
    {
        var result = _service.Evaluate("oh crap this demo is gosh darn interesting");

        Assert.Equal(ModerationStatus.Flagged, result.Status);
        Assert.Contains("crap", result.MatchedTerms);
        Assert.Contains("gosh", result.MatchedTerms);
        Assert.Contains("darn", result.MatchedTerms);
        Assert.Equal(3, result.MatchedTerms.Length);
    }

    [Fact]
    public void SingleFlaggedWord_ReturnsFlagged()
    {
        var result = _service.Evaluate("what the heck");

        Assert.Equal(ModerationStatus.Flagged, result.Status);
        Assert.Single(result.MatchedTerms);
        Assert.Equal("heck", result.MatchedTerms[0]);
    }

    [Fact]
    public void MixedCase_StillDetected()
    {
        var result = _service.Evaluate("Oh CRAP and DANG");

        Assert.Equal(ModerationStatus.Flagged, result.Status);
        Assert.Contains("crap", result.MatchedTerms);
        Assert.Contains("dang", result.MatchedTerms);
    }

    [Fact]
    public void DuplicateFlaggedWords_DeduplicatedInResult()
    {
        var result = _service.Evaluate("crap crap crap");

        Assert.Equal(ModerationStatus.Flagged, result.Status);
        Assert.Single(result.MatchedTerms);
        Assert.Equal("crap", result.MatchedTerms[0]);
    }

    [Fact]
    public void PunctuationAdjacentToFlaggedWord_StillDetected()
    {
        var result = _service.Evaluate("oh crap! that is dang.");

        Assert.Equal(ModerationStatus.Flagged, result.Status);
        Assert.Contains("crap", result.MatchedTerms);
        Assert.Contains("dang", result.MatchedTerms);
    }

    [Fact]
    public void EmptyText_ReturnsClean()
    {
        var result = _service.Evaluate("");

        Assert.Equal(ModerationStatus.Clean, result.Status);
        Assert.Empty(result.MatchedTerms);
    }

    [Theory]
    [InlineData("poop")]
    [InlineData("crap")]
    [InlineData("dang")]
    [InlineData("gosh")]
    [InlineData("darn")]
    [InlineData("heck")]
    [InlineData("frick")]
    [InlineData("butt")]
    [InlineData("crud")]
    public void EachFlaggedTerm_IsDetected(string term)
    {
        var result = _service.Evaluate($"this is {term} right here");

        Assert.Equal(ModerationStatus.Flagged, result.Status);
        Assert.Single(result.MatchedTerms);
        Assert.Equal(term, result.MatchedTerms[0]);
    }

    [Fact]
    public void MatchedTerms_AreSortedAlphabetically()
    {
        var result = _service.Evaluate("heck dang crap");

        Assert.Equal(new[] { "crap", "dang", "heck" }, result.MatchedTerms);
    }
}

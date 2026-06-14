using ImageManager.Application;

namespace ImageManager.Tests;

public class SourcesParserTests
{
    [Fact]
    public void ParsesNameAndUrl()
    {
        var result = SourcesParser.Parse("Jane - https://ko-fi.com/jane");

        var artist = Assert.Single(result);
        Assert.Equal("Jane", artist.Name);
        Assert.Equal("https://ko-fi.com/jane", artist.CreditUrl);
    }

    [Fact]
    public void KeepsMultiWordNamesAndExtractsUrlFromSurroundingText()
    {
        var result = SourcesParser.Parse("Jane Doe - commissions at https://x.com/jd thanks");

        var artist = Assert.Single(result);
        Assert.Equal("Jane Doe", artist.Name);
        Assert.Equal("https://x.com/jd", artist.CreditUrl);
    }

    [Fact]
    public void MissingUrl_YieldsEmptyCreditUrl()
    {
        var result = SourcesParser.Parse("Jane - no link provided");

        Assert.Equal("", Assert.Single(result).CreditUrl);
    }

    [Fact]
    public void LinesWithoutSeparator_AreSkipped()
    {
        var result = SourcesParser.Parse("Some heading line\nJane - https://x");

        Assert.Equal("Jane", Assert.Single(result).Name);
    }

    [Fact]
    public void StopsAtEndArtistsMarker_IgnoringCaseAndWhitespace()
    {
        var result = SourcesParser.Parse("A - https://a\nEND ARTISTS\nB - https://b");

        Assert.Equal("A", Assert.Single(result).Name);
    }

    [Theory]
    [InlineData("endartists")]
    [InlineData("End Artists")]
    [InlineData("  END   ARTISTS  ")]
    public void EndMarkerMatchesAreCaseAndSpacingInsensitive(string marker)
    {
        var result = SourcesParser.Parse($"A - https://a\n{marker}\nB - https://b");

        Assert.Equal("A", Assert.Single(result).Name);
    }

    [Fact]
    public void HandlesCrlfLineEndings()
    {
        var result = SourcesParser.Parse("A - https://a\r\nB - https://b\r\n");

        Assert.Collection(result,
            x => Assert.Equal("A", x.Name),
            x => Assert.Equal("B", x.Name));
    }

    [Fact]
    public void EmptyText_YieldsNoArtists()
    {
        Assert.Empty(SourcesParser.Parse(""));
    }
}

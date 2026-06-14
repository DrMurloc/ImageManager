using ImageManager.Domain;

namespace ImageManager.Tests;

public class Ao3NumberingTests
{
    [Theory]
    [InlineData("1. Prologue", 1, "Prologue")]
    [InlineData("2. Armor", 2, "Armor")]
    [InlineData("10. The Long Goodbye", 10, "The Long Goodbye")]
    public void ParseTitle_SplitsNumberAndName(string title, int number, string name)
    {
        var parsed = Ao3Numbering.ParseTitle(title);

        Assert.NotNull(parsed);
        Assert.Equal(number, parsed!.Value.Number);
        Assert.Equal(name, parsed.Value.Name);
    }

    [Theory]
    [InlineData("Chapter 5")]
    [InlineData("Armor")]
    [InlineData("")]
    public void ParseTitle_WithoutNumberPrefix_ReturnsNull(string title)
        => Assert.Null(Ao3Numbering.ParseTitle(title));

    private static readonly string[] Titles = { "1. Prologue", "2. Armor", "3. Shield" };

    [Theory]
    [InlineData("Armor", 1)]
    [InlineData("Shield", 2)]
    [InlineData("armor", 1)]      // case-insensitive
    [InlineData("Prologue", 0)]   // AO3 chapter 1 -> manuscript 0
    public void Resolve_MatchesNameAndSubtractsOne(string chapterName, int expected)
        => Assert.Equal(expected, Ao3Numbering.Resolve(Titles, chapterName));

    [Fact]
    public void Resolve_UnknownName_ReturnsNull()
        => Assert.Null(Ao3Numbering.Resolve(Titles, "Unknown"));

    [Fact]
    public void Resolve_PrologueFallsBackToZeroWithoutAo3Data()
        => Assert.Equal(0, Ao3Numbering.Resolve(Array.Empty<string>(), "Prologue"));

    [Fact]
    public void Resolve_NonPrologueWithoutMatch_ReturnsNull()
        => Assert.Null(Ao3Numbering.Resolve(Array.Empty<string>(), "Armor"));
}

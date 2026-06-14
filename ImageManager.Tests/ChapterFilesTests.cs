using ImageManager.Domain;

namespace ImageManager.Tests;

public class ChapterFilesTests
{
    [Theory]
    [InlineData("Connected - Armor.docx", "Connected", "Armor")]
    [InlineData("Connected - Armor.edited.docx", "Connected", "Armor")]
    [InlineData("Connected - Armor (2).docx", "Connected", "Armor")]
    [InlineData("Connected - Armor.edited (3).docx", "Connected", "Armor")]
    [InlineData("Connected - Armor (2).edited.docx", "Connected", "Armor")]
    [InlineData("Connected - Prologue.docx", "Connected", "Prologue")]
    public void Parse_StripsNoiseAndSplitsBookFromChapter(string fileName, string book, string chapter)
    {
        var result = ChapterFiles.Parse(fileName);

        Assert.Equal(book, result.Book);
        Assert.Equal(chapter, result.ChapterName);
    }

    [Fact]
    public void Parse_SplitsOnFirstSeparatorOnly()
    {
        var result = ChapterFiles.Parse("Connected - The Long - Goodbye.docx");

        Assert.Equal("Connected", result.Book);
        Assert.Equal("The Long - Goodbye", result.ChapterName);
    }

    [Fact]
    public void Parse_WithoutSeparator_LeavesBookEmpty()
    {
        var result = ChapterFiles.Parse("JustAChapter.docx");

        Assert.Equal("", result.Book);
        Assert.Equal("JustAChapter", result.ChapterName);
    }

    [Fact]
    public void Parse_KeepsNonNumericParenthesesInName()
    {
        var result = ChapterFiles.Parse("Connected - Armor (Reprise).docx");

        Assert.Equal("Armor (Reprise)", result.ChapterName);
    }

    [Theory]
    [InlineData(0, "Prologue", "Chapter 0 - Prologue.docx")]
    [InlineData(1, "Armor", "Chapter 1 - Armor.docx")]
    [InlineData(12, "The End", "Chapter 12 - The End.docx")]
    public void TargetFileName_FormatsChapterAndName(int number, string name, string expected)
        => Assert.Equal(expected, ChapterFiles.TargetFileName(number, name));

    [Theory]
    [InlineData("Chapter 0 - Prologue.docx", 0, "Prologue")]
    [InlineData("Chapter 1 - Armor.docx", 1, "Armor")]
    [InlineData("Chapter 12 - The End.docx", 12, "The End")]
    [InlineData("Chapter 1 - The Long - Goodbye.docx", 1, "The Long - Goodbye")]
    public void ParseTarget_RoundTripsTheMigratedName(string fileName, int number, string name)
    {
        var parsed = ChapterFiles.ParseTarget(fileName);

        Assert.NotNull(parsed);
        Assert.Equal(number, parsed!.Value.Number);
        Assert.Equal(name, parsed.Value.Name);
    }

    [Theory]
    [InlineData("notes.docx")]
    [InlineData("Armor.docx")]
    [InlineData("Chapter X - Armor.docx")]
    public void ParseTarget_ReturnsNullForNonConvention(string fileName)
        => Assert.Null(ChapterFiles.ParseTarget(fileName));
}

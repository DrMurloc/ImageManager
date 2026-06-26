using System.Text.RegularExpressions;
using ImageManager.Domain;

namespace ImageManager.Tests;

public class NotePathTests
{
    [Theory]
    [InlineData("books/connected/index.md", "books/connected/index.md")]
    [InlineData("books\\connected\\index.md", "books/connected/index.md")]
    [InlineData("/books/connected/index.md/", "books/connected/index.md")]
    [InlineData("books//connected///index.md", "books/connected/index.md")]
    [InlineData("  books/connected/index.md  ", "books/connected/index.md")]
    public void Parse_NormalizesSlashesAndTrim(string raw, string expected)
        => Assert.Equal(expected, NotePath.Parse(raw).Value);

    [Theory]
    [InlineData("books/connected/index", "books/connected/index.md")]
    [InlineData("books/connected/index.md", "books/connected/index.md")]
    [InlineData("notes/todo.txt", "notes/todo.txt")]
    public void Parse_DefaultsToMarkdownOnlyWhenNoExtension(string raw, string expected)
        => Assert.Equal(expected, NotePath.Parse(raw).Value);

    [Fact]
    public void Parse_DerivesFolderFileNameAndTitle()
    {
        var path = NotePath.Parse("books/connected/character/Astrid.md");

        Assert.Equal("books/connected/character", path.Folder);
        Assert.Equal("Astrid.md", path.FileName);
        Assert.Equal("Astrid", path.Title);
    }

    [Fact]
    public void Parse_RootLevelNote_HasEmptyFolder()
    {
        var path = NotePath.Parse("scratch.md");

        Assert.Equal("", path.Folder);
        Assert.Equal("scratch.md", path.FileName);
        Assert.Equal("scratch", path.Title);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/")]
    [InlineData("books/../secrets.md")]
    [InlineData("./notes.md")]
    public void TryParse_RejectsInvalidPaths(string? raw)
    {
        Assert.False(NotePath.TryParse(raw, out var path, out var error));
        Assert.Null(path);
        Assert.NotEqual("", error);
    }

    [Fact]
    public void Parse_ThrowsOnInvalid()
        => Assert.Throws<ArgumentException>(() => NotePath.Parse("books/../oops.md"));

    [Fact]
    public void Key_IsStableForTheSameNormalizedPath()
        => Assert.Equal(
            NotePath.Parse("books/connected/index.md").Key,
            NotePath.Parse("/books\\connected//index.md").Key);

    [Fact]
    public void Key_DiffersForDifferentPaths()
        => Assert.NotEqual(
            NotePath.Parse("a/b.md").Key,
            NotePath.Parse("a/c.md").Key);

    [Fact]
    public void Key_UsesOnlyAzureSearchSafeCharacters()
    {
        var key = NotePath.Parse("books/connected/character/Astrid Hofferson.md").Key;

        Assert.Matches(new Regex("^[A-Za-z0-9_=-]+$"), key);
    }
}

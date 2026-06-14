using ImageManager.Domain;

namespace ImageManager.Tests;

public class ChapterChunkerTests
{
    [Fact]
    public void EmptyText_ProducesNoChunks()
        => Assert.Empty(ChapterChunker.Chunk("   "));

    [Fact]
    public void ShortText_IsASingleChunk()
    {
        var chunks = ChapterChunker.Chunk("One paragraph.\n\nTwo paragraph.", maxChars: 1000);

        Assert.Equal(new[] { "One paragraph.\n\nTwo paragraph." }, chunks);
    }

    [Fact]
    public void PacksParagraphsUpToMaxChars()
    {
        var para = new string('a', 100);
        var text = string.Join("\n\n", Enumerable.Repeat(para, 10)); // ~1000 chars + separators

        var chunks = ChapterChunker.Chunk(text, maxChars: 300);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(c.Length <= 300));
        // every paragraph survives across the chunks
        Assert.Equal(10, chunks.Sum(c => c.Split("\n\n").Length));
    }

    [Fact]
    public void HardWrapsAParagraphLongerThanMax()
    {
        var huge = new string('x', 5000);

        var chunks = ChapterChunker.Chunk(huge, maxChars: 1000);

        Assert.Equal(5, chunks.Count);
        Assert.All(chunks, c => Assert.True(c.Length <= 1000));
        Assert.Equal(5000, chunks.Sum(c => c.Length));
    }

    [Fact]
    public void CollapsesBlankLineRuns()
    {
        var chunks = ChapterChunker.Chunk("A\n\n\n\nB", maxChars: 1000);

        Assert.Equal(new[] { "A\n\nB" }, chunks);
    }
}

using ImageManager.Domain;

namespace ImageManager.Tests;

public class BookSearchDocsTests
{
    [Fact]
    public void ChunkKey_IsStableAndAzureSafe()
    {
        var key = BookSearchDocs.ChunkKey("Connected", 1, 3);

        Assert.Equal("Connected_1_3", key);
    }

    [Fact]
    public void ChunkKey_SlugsNonAlphanumericBookCharacters()
    {
        var key = BookSearchDocs.ChunkKey("Astrid & Cale: Book 2", 0, 0);

        Assert.Matches("^[A-Za-z0-9_=-]+$", key);
    }

    [Fact]
    public void ChunkKey_DistinctPerChunk()
    {
        Assert.NotEqual(
            BookSearchDocs.ChunkKey("Connected", 1, 0),
            BookSearchDocs.ChunkKey("Connected", 1, 1));
    }
}

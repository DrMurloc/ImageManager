namespace ImageManager.Application;

// Read side of the search index, used by the MCP server to answer book questions.
public interface ISearchQuery
{
    // Top BM25 passages for a query, optionally restricted to one book.
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, string? book, int top, CancellationToken ct = default);

    // The full text of a chapter (its chunks reassembled in order), or null if not indexed.
    Task<ChapterText?> GetChapterAsync(string book, int chapterNumber, CancellationToken ct = default);
}

public sealed record SearchHit(string Book, int ChapterNumber, string ChapterName, int ChunkIndex, string Content, double Score);

public sealed record ChapterText(string Book, int ChapterNumber, string ChapterName, string Content);

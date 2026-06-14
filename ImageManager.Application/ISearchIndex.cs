namespace ImageManager.Application;

// Writes chapter chunks into the search index. Implemented over Azure AI Search.
public interface ISearchIndex
{
    // Creates or updates the index definition. Safe to call repeatedly.
    Task EnsureIndexAsync(CancellationToken ct = default);

    // Replaces all indexed chunks for a chapter with the supplied ones.
    Task IndexChapterAsync(string book, int chapterNumber, string chapterName, IReadOnlyList<string> chunks, CancellationToken ct = default);
}

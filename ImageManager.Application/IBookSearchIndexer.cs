namespace ImageManager.Application;

// Lists manuscript chapters from Drive and indexes their text into search.
public interface IBookSearchIndexer
{
    Task EnsureIndexAsync(CancellationToken ct = default);

    // Every "Chapter N - Name.docx" found under /Books/<Book>/, across all books.
    Task<IReadOnlyList<BookChapterRef>> ListChaptersAsync(string accessToken, string booksRootFolderId, CancellationToken ct = default);

    Task IndexChapterAsync(string accessToken, BookChapterRef chapter, CancellationToken ct = default);

    // Indexes every listed chapter; returns the number indexed.
    Task<int> IndexAllAsync(string accessToken, string booksRootFolderId, CancellationToken ct = default);
}

public sealed record BookChapterRef(string Book, int Number, string Name, string DriveFileId);

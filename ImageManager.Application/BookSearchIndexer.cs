using ImageManager.Domain;

namespace ImageManager.Application;

public sealed class BookSearchIndexer : IBookSearchIndexer
{
    private readonly IDriveUploader _drive;
    private readonly IDocxTextExtractor _extractor;
    private readonly ISearchIndex _index;

    public BookSearchIndexer(IDriveUploader drive, IDocxTextExtractor extractor, ISearchIndex index)
    {
        _drive = drive;
        _extractor = extractor;
        _index = index;
    }

    public Task EnsureIndexAsync(CancellationToken ct = default) => _index.EnsureIndexAsync(ct);

    public async Task<IReadOnlyList<BookChapterRef>> ListChaptersAsync(string accessToken, string booksRootFolderId, CancellationToken ct = default)
    {
        var chapters = new List<BookChapterRef>();
        foreach (var book in await _drive.ListFoldersAsync(accessToken, booksRootFolderId, ct))
        {
            foreach (var file in await _drive.ListFilesAsync(accessToken, book.Id, ct))
            {
                if (ChapterFiles.ParseTarget(file.Name) is { } parsed)
                    chapters.Add(new BookChapterRef(book.Name, parsed.Number, parsed.Name, file.Id));
            }
        }
        return chapters;
    }

    public async Task IndexChapterAsync(string accessToken, BookChapterRef chapter, CancellationToken ct = default)
    {
        var bytes = await _drive.DownloadAsync(accessToken, chapter.DriveFileId, ct);
        var text = _extractor.Extract(bytes);
        var chunks = ChapterChunker.Chunk(text);
        await _index.IndexChapterAsync(chapter.Book, chapter.Number, chapter.Name, chunks, ct);
    }

    public async Task<int> IndexAllAsync(string accessToken, string booksRootFolderId, CancellationToken ct = default)
    {
        var chapters = await ListChaptersAsync(accessToken, booksRootFolderId, ct);
        foreach (var chapter in chapters)
            await IndexChapterAsync(accessToken, chapter, ct);
        return chapters.Count;
    }
}

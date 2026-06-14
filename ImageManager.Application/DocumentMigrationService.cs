using ImageManager.Domain;

namespace ImageManager.Application;

public sealed class DocumentMigrationService : IDocumentMigrationService
{
    private readonly IDriveUploader _uploader;

    public DocumentMigrationService(IDriveUploader uploader)
    {
        _uploader = uploader;
    }

    public async Task<MigrationResult> MigrateAsync(MigrateRequest request, CancellationToken ct = default)
    {
        var bookFolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var uploaded = 0;
        var skipped = new List<string>();

        foreach (var doc in request.Documents)
        {
            if (!bookFolders.TryGetValue(doc.Book, out var folderId))
            {
                folderId = await _uploader.EnsureFolderAsync(request.AccessToken, request.BooksRootFolderId, doc.Book, ct);
                bookFolders[doc.Book] = folderId;
            }

            var fileName = ChapterFiles.TargetFileName(doc.ChapterNumber, doc.ChapterName);
            if (await _uploader.FileExistsAsync(request.AccessToken, folderId, fileName, ct))
            {
                skipped.Add(fileName);
                continue;
            }

            using var content = new MemoryStream(doc.Content);
            await _uploader.UploadAsync(request.AccessToken, folderId, fileName, doc.ContentType, content, ct);
            uploaded++;
        }

        return new MigrationResult(uploaded, skipped);
    }
}

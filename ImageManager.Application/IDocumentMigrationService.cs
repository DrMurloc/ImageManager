namespace ImageManager.Application;

// Migrates downloaded chapter documents into /Books/<Book>/ on Drive, naming each
// "Chapter <number> - <name>.docx" and skipping any that already exist.
public interface IDocumentMigrationService
{
    Task<MigrationResult> MigrateAsync(MigrateRequest request, CancellationToken ct = default);
}

public sealed record MigrateDocument(
    string Book,
    int ChapterNumber,
    string ChapterName,
    string ContentType,
    byte[] Content);

public sealed record MigrateRequest(string AccessToken, string BooksRootFolderId, IReadOnlyList<MigrateDocument> Documents);

public sealed record MigrationResult(int Uploaded, IReadOnlyList<string> Skipped);

namespace ImageManager.Application;

// Orchestrates syncing group images to Azure: downloads each from Drive, uploads to blob
// storage, and records the result on the persisted ImageAsset.
public interface IImageSyncService
{
    Task<IReadOnlyList<SyncResult>> SyncAsync(string groupFolderId, IReadOnlyList<SyncItem> items, CancellationToken ct = default);
}

public sealed record SyncItem(
    string DriveFileId,
    string SourceFileName,
    string BlobName,
    string AltText,
    int? Width,
    int? Height,
    string MimeType,
    string? Md5);

public sealed record SyncResult(string DriveFileId, string BlobName, string PublicUrl);

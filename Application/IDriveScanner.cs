namespace ImageManager.Application;

public interface IDriveScanner
{
    Task<IReadOnlyList<ScannedGroup>> ScanGroupsAsync(CancellationToken ct = default);

    // Top-level "collection" folders directly under the Commissions root.
    Task<IReadOnlyList<DriveFolderRef>> ListCollectionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DriveImageFile>> ListImagesAsync(string folderId, CancellationToken ct = default);
    Task<byte[]> DownloadAsync(string fileId, CancellationToken ct = default);

    // Plain-text contents of the "Sources" Google Doc in the Commissions root, or null if none is found.
    Task<string?> ReadSourcesDocAsync(CancellationToken ct = default);
}

using ImageManager.Domain;

namespace ImageManager.Application;

public sealed class ImageSyncService : IImageSyncService
{
    private readonly IDriveScanner _drive;
    private readonly IBlobSyncService _blob;
    private readonly IMetadataStore _store;

    public ImageSyncService(IDriveScanner drive, IBlobSyncService blob, IMetadataStore store)
    {
        _drive = drive;
        _blob = blob;
        _store = store;
    }

    public async Task<IReadOnlyList<SyncResult>> SyncAsync(string groupFolderId, IReadOnlyList<SyncItem> items, CancellationToken ct = default)
    {
        var db = await _store.LoadAsync(ct);
        var group = db.Groups.FirstOrDefault(g => g.DriveFolderId == groupFolderId)
            ?? throw new InvalidOperationException($"Group '{groupFolderId}' was not found.");

        var results = new List<SyncResult>();
        foreach (var item in items)
        {
            var bytes = await _drive.DownloadAsync(item.DriveFileId, ct);
            var url = await _blob.UploadImageAsync(item.BlobName, bytes, item.MimeType, ct);

            var asset = group.Images.FirstOrDefault(i => i.DriveFileId == item.DriveFileId);
            if (asset is null)
            {
                asset = new ImageAsset { DriveFileId = item.DriveFileId };
                group.Images.Add(asset);
            }

            asset.SourceFileName = item.SourceFileName;
            asset.BlobName = item.BlobName;
            asset.Width = item.Width;
            asset.Height = item.Height;
            asset.AltText = item.AltText;
            asset.Synced = true;
            asset.SyncedAt = DateTimeOffset.UtcNow;
            asset.SyncedMd5 = item.Md5;

            results.Add(new SyncResult(item.DriveFileId, item.BlobName, url));
        }

        await _store.SaveAsync(db, ct);
        return results;
    }
}

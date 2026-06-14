using ImageManager.Application;
using ImageManager.Domain;

namespace ImageManager.Tests;

// In-memory test doubles for the Application ports, so orchestration services can be tested
// without real Drive/Azure access.

public sealed class FakeDriveUploader : IDriveUploader
{
    public string FolderId = "folder-1";
    public string? CreatedUnderParentId;
    public string? CreatedFolderName;
    public readonly List<string> UploadedFileNames = new();

    public Task<string> CreateFolderAsync(string accessToken, string parentId, string name, CancellationToken ct = default)
    {
        CreatedUnderParentId = parentId;
        CreatedFolderName = name;
        return Task.FromResult(FolderId);
    }

    public Task UploadAsync(string accessToken, string folderId, string fileName, string contentType, Stream content, CancellationToken ct = default)
    {
        UploadedFileNames.Add(fileName);
        return Task.CompletedTask;
    }
}

public sealed class FakeDriveScanner : IDriveScanner
{
    public byte[] Bytes = { 1, 2, 3 };
    public readonly List<string> Downloaded = new();

    public Task<byte[]> DownloadAsync(string fileId, CancellationToken ct = default)
    {
        Downloaded.Add(fileId);
        return Task.FromResult(Bytes);
    }

    public Task<IReadOnlyList<ScannedGroup>> ScanGroupsAsync(CancellationToken ct = default)
        => throw new NotSupportedException();
    public Task<IReadOnlyList<DriveFolderRef>> ListCollectionsAsync(CancellationToken ct = default)
        => throw new NotSupportedException();
    public Task<IReadOnlyList<DriveImageFile>> ListImagesAsync(string folderId, CancellationToken ct = default)
        => throw new NotSupportedException();
    public Task<string?> ReadSourcesDocAsync(CancellationToken ct = default)
        => throw new NotSupportedException();
}

public sealed class FakeBlobSyncService : IBlobSyncService
{
    public readonly List<(string BlobName, string ContentType, int Bytes)> Uploads = new();

    public Task<string> UploadImageAsync(string blobName, byte[] content, string contentType, CancellationToken ct = default)
    {
        Uploads.Add((blobName, contentType, content.Length));
        return Task.FromResult(GetPublicUrl(blobName));
    }

    public string GetPublicUrl(string blobName) => $"https://cdn/{blobName}";
}

public sealed class InMemoryMetadataStore : IMetadataStore
{
    public CommissionDatabase Db = new();
    public int SaveCount;

    public Task<CommissionDatabase> LoadAsync(CancellationToken ct = default) => Task.FromResult(Db);

    public Task SaveAsync(CommissionDatabase db, CancellationToken ct = default)
    {
        Db = db;
        SaveCount++;
        return Task.CompletedTask;
    }
}

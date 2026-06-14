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

    // Document-migration tracking.
    public string? EnsuredUnderParentId;
    public readonly List<string> EnsuredFolderNames = new();
    public readonly HashSet<string> ExistingFiles = new();

    // Drive read fixtures (for indexing): parentId -> child folders, folderId -> files, fileId -> bytes.
    public readonly Dictionary<string, List<DriveFolderRef>> FoldersByParent = new();
    public readonly Dictionary<string, List<DriveFolderRef>> FilesByFolder = new();
    public readonly Dictionary<string, byte[]> ContentByFileId = new();

    public Task<string> CreateFolderAsync(string accessToken, string parentId, string name, CancellationToken ct = default)
    {
        CreatedUnderParentId = parentId;
        CreatedFolderName = name;
        return Task.FromResult(FolderId);
    }

    public Task<string> EnsureFolderAsync(string accessToken, string parentId, string name, CancellationToken ct = default)
    {
        EnsuredUnderParentId = parentId;
        EnsuredFolderNames.Add(name);
        return Task.FromResult($"folder-{name}");
    }

    public Task<bool> FileExistsAsync(string accessToken, string folderId, string fileName, CancellationToken ct = default)
        => Task.FromResult(ExistingFiles.Contains(fileName));

    public Task<IReadOnlyList<DriveFolderRef>> ListFoldersAsync(string accessToken, string parentId, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<DriveFolderRef>)(FoldersByParent.GetValueOrDefault(parentId) ?? new List<DriveFolderRef>()));

    public Task<IReadOnlyList<DriveFolderRef>> ListFilesAsync(string accessToken, string folderId, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<DriveFolderRef>)(FilesByFolder.GetValueOrDefault(folderId) ?? new List<DriveFolderRef>()));

    public Task<byte[]> DownloadAsync(string accessToken, string fileId, CancellationToken ct = default)
        => Task.FromResult(ContentByFileId.GetValueOrDefault(fileId) ?? Array.Empty<byte>());

    public Task UploadAsync(string accessToken, string folderId, string fileName, string contentType, Stream content, CancellationToken ct = default)
    {
        UploadedFileNames.Add(fileName);
        return Task.CompletedTask;
    }
}

public sealed class FakeSearchIndex : ISearchIndex
{
    public int EnsureCalls;
    public readonly List<(string Book, int Number, string Name, IReadOnlyList<string> Chunks)> Indexed = new();

    public Task EnsureIndexAsync(CancellationToken ct = default)
    {
        EnsureCalls++;
        return Task.CompletedTask;
    }

    public Task IndexChapterAsync(string book, int chapterNumber, string chapterName, IReadOnlyList<string> chunks, CancellationToken ct = default)
    {
        Indexed.Add((book, chapterNumber, chapterName, chunks));
        return Task.CompletedTask;
    }
}

public sealed class FakeDocxTextExtractor : IDocxTextExtractor
{
    public string Text = "A short chapter body.";
    public string Extract(byte[] docx) => Text;
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

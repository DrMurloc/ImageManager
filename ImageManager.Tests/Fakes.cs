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

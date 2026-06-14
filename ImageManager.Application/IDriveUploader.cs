namespace ImageManager.Application;

// Drive writes performed as the signed-in user (their OAuth token), so files are owned by them.
public interface IDriveUploader
{
    Task<string> CreateFolderAsync(string accessToken, string parentId, string name, CancellationToken ct = default);

    // Returns the id of the child folder named <name> under <parentId>, creating it if absent.
    Task<string> EnsureFolderAsync(string accessToken, string parentId, string name, CancellationToken ct = default);

    // True when a non-trashed file named <fileName> already exists directly in <folderId>.
    Task<bool> FileExistsAsync(string accessToken, string folderId, string fileName, CancellationToken ct = default);

    // Child folders directly under <parentId> (id + name).
    Task<IReadOnlyList<DriveFolderRef>> ListFoldersAsync(string accessToken, string parentId, CancellationToken ct = default);

    // Non-folder files directly under <folderId> (id + name).
    Task<IReadOnlyList<DriveFolderRef>> ListFilesAsync(string accessToken, string folderId, CancellationToken ct = default);

    // Raw bytes of a Drive file, downloaded as the signed-in user.
    Task<byte[]> DownloadAsync(string accessToken, string fileId, CancellationToken ct = default);

    Task UploadAsync(string accessToken, string folderId, string fileName, string contentType, Stream content, CancellationToken ct = default);
}

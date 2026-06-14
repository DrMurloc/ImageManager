namespace ImageManager.Application;

// Drive writes performed as the signed-in user (their OAuth token), so files are owned by them.
public interface IDriveUploader
{
    Task<string> CreateFolderAsync(string accessToken, string parentId, string name, CancellationToken ct = default);
    Task UploadAsync(string accessToken, string folderId, string fileName, string contentType, Stream content, CancellationToken ct = default);
}

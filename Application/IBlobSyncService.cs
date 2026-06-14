namespace ImageManager.Application;

public interface IBlobSyncService
{
    Task<string> UploadImageAsync(string blobName, byte[] content, string contentType, CancellationToken ct = default);
    string GetPublicUrl(string blobName);
}

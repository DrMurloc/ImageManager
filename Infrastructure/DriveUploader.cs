using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using ImageManager.Application;
using DriveData = Google.Apis.Drive.v3.Data;

namespace ImageManager.Infrastructure;

public sealed class DriveUploader : IDriveUploader
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";

    private static DriveService Build(string accessToken) =>
        new(new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            ApplicationName = "ImageManager"
        });

    public async Task<string> CreateFolderAsync(string accessToken, string parentId, string name, CancellationToken ct = default)
    {
        var service = Build(accessToken);
        var metadata = new DriveData.File
        {
            Name = name,
            MimeType = FolderMimeType,
            Parents = new[] { parentId }
        };
        var request = service.Files.Create(metadata);
        request.Fields = "id";
        var created = await request.ExecuteAsync(ct);
        return created.Id;
    }

    public async Task UploadAsync(string accessToken, string folderId, string fileName, string contentType, Stream content, CancellationToken ct = default)
    {
        var service = Build(accessToken);
        var metadata = new DriveData.File
        {
            Name = fileName,
            Parents = new[] { folderId }
        };
        var request = service.Files.Create(metadata, content, contentType);
        request.Fields = "id";
        var progress = await request.UploadAsync(ct);
        if (progress.Status != UploadStatus.Completed)
            throw progress.Exception ?? new InvalidOperationException($"Upload of {fileName} did not complete.");
    }
}

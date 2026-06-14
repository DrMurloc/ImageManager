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

    public async Task<string> EnsureFolderAsync(string accessToken, string parentId, string name, CancellationToken ct = default)
    {
        var service = Build(accessToken);
        var list = service.Files.List();
        list.Q = $"'{parentId}' in parents and name = '{Escape(name)}' and mimeType = '{FolderMimeType}' and trashed = false";
        list.Fields = "files(id)";
        list.PageSize = 1;
        var existing = await list.ExecuteAsync(ct);
        if (existing.Files is { Count: > 0 })
            return existing.Files[0].Id;

        return await CreateFolderAsync(accessToken, parentId, name, ct);
    }

    public async Task<bool> FileExistsAsync(string accessToken, string folderId, string fileName, CancellationToken ct = default)
    {
        var service = Build(accessToken);
        var list = service.Files.List();
        list.Q = $"'{folderId}' in parents and name = '{Escape(fileName)}' and trashed = false";
        list.Fields = "files(id)";
        list.PageSize = 1;
        var found = await list.ExecuteAsync(ct);
        return found.Files is { Count: > 0 };
    }

    public async Task<IReadOnlyList<DriveFolderRef>> ListFoldersAsync(string accessToken, string parentId, CancellationToken ct = default)
        => await ListAsync(accessToken, $"'{parentId}' in parents and mimeType = '{FolderMimeType}' and trashed = false", ct);

    public async Task<IReadOnlyList<DriveFolderRef>> ListFilesAsync(string accessToken, string folderId, CancellationToken ct = default)
        => await ListAsync(accessToken, $"'{folderId}' in parents and mimeType != '{FolderMimeType}' and trashed = false", ct);

    public async Task<byte[]> DownloadAsync(string accessToken, string fileId, CancellationToken ct = default)
    {
        var service = Build(accessToken);
        using var stream = new MemoryStream();
        await service.Files.Get(fileId).DownloadAsync(stream, ct);
        return stream.ToArray();
    }

    private async Task<IReadOnlyList<DriveFolderRef>> ListAsync(string accessToken, string query, CancellationToken ct)
    {
        var service = Build(accessToken);
        var entries = new List<DriveFolderRef>();
        string? pageToken = null;
        do
        {
            var request = service.Files.List();
            request.Q = query;
            request.Fields = "nextPageToken, files(id,name)";
            request.PageSize = 1000;
            request.OrderBy = "name";
            request.PageToken = pageToken;

            var response = await request.ExecuteAsync(ct);
            if (response.Files is not null)
                entries.AddRange(response.Files.Select(f => new DriveFolderRef(f.Id, f.Name)));
            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return entries;
    }

    // Drive query strings are single-quoted; escape backslashes and single quotes in user values.
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("'", "\\'");

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

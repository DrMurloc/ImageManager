using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using ImageManager.Application;
using ImageManager.Configuration;
using Microsoft.Extensions.Options;
using DriveData = Google.Apis.Drive.v3.Data;

namespace ImageManager.Infrastructure;

public sealed class GoogleDriveScanner : IDriveScanner
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";

    private readonly GoogleDriveOptions _options;
    private readonly Lazy<DriveService> _service;

    public GoogleDriveScanner(IOptions<GoogleDriveOptions> options)
    {
        _options = options.Value;
        _service = new Lazy<DriveService>(CreateService);
    }

    private DriveService Service => _service.Value;

    private DriveService CreateService()
    {
        ServiceAccountCredential serviceAccount;
        if (!string.IsNullOrWhiteSpace(_options.ServiceAccountKeyPath))
            serviceAccount = CredentialFactory.FromFile<ServiceAccountCredential>(_options.ServiceAccountKeyPath);
        else if (!string.IsNullOrWhiteSpace(_options.ServiceAccountJson))
            serviceAccount = CredentialFactory.FromJson<ServiceAccountCredential>(_options.ServiceAccountJson);
        else
            throw new InvalidOperationException(
                "Google Drive credentials are not configured. Set GoogleDrive:ServiceAccountKeyPath or GoogleDrive:ServiceAccountJson in User Secrets.");

        var credential = serviceAccount.ToGoogleCredential().CreateScoped(DriveService.Scope.DriveReadonly);
        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ImageManager"
        });
    }

    public async Task<IReadOnlyList<ScannedGroup>> ScanGroupsAsync(CancellationToken ct = default)
    {
        var root = _options.CommissionsRootFolderId;
        if (string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException("GoogleDrive:CommissionsRootFolderId is not configured.");

        var groups = new List<ScannedGroup>();
        foreach (var character in await ListFoldersAsync(root, ct))
        {
            foreach (var group in await ListFoldersAsync(character.Id, ct))
                groups.Add(new ScannedGroup(group.Id, character.Name, group.Name));
        }
        return groups;
    }

    public async Task<IReadOnlyList<DriveFolderRef>> ListCollectionsAsync(CancellationToken ct = default)
    {
        var root = _options.CommissionsRootFolderId;
        if (string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException("GoogleDrive:CommissionsRootFolderId is not configured.");

        var folders = await ListFoldersAsync(root, ct);
        return folders.Select(f => new DriveFolderRef(f.Id, f.Name)).ToList();
    }

    public async Task<IReadOnlyList<DriveImageFile>> ListImagesAsync(string folderId, CancellationToken ct = default)
    {
        var images = new List<DriveImageFile>();
        await CollectImagesAsync(folderId, "", images, ct);
        return images;
    }

    public async Task<byte[]> DownloadAsync(string fileId, CancellationToken ct = default)
    {
        using var stream = new MemoryStream();
        await Service.Files.Get(fileId).DownloadAsync(stream, ct);
        return stream.ToArray();
    }

    public async Task<string?> ReadSourcesDocAsync(CancellationToken ct = default)
    {
        var root = _options.CommissionsRootFolderId;
        if (string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException("GoogleDrive:CommissionsRootFolderId is not configured.");

        const string docMimeType = "application/vnd.google-apps.document";
        var docs = await ListChildrenAsync(root, $"mimeType = '{docMimeType}'", "nextPageToken, files(id,name)", ct);
        var sources = docs.FirstOrDefault(f => f.Name.Contains("source", StringComparison.OrdinalIgnoreCase));
        if (sources is null) return null;

        using var stream = new MemoryStream();
        await Service.Files.Export(sources.Id, "text/plain").DownloadAsync(stream, ct);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private async Task CollectImagesAsync(string folderId, string prefix, List<DriveImageFile> images, CancellationToken ct)
    {
        const string fields = "nextPageToken, files(id,name,mimeType,size,md5Checksum,imageMediaMetadata(width,height))";
        foreach (var file in await ListChildrenAsync(folderId, null, fields, ct))
        {
            if (file.MimeType == FolderMimeType)
            {
                await CollectImagesAsync(file.Id, $"{prefix}{file.Name}/", images, ct);
            }
            else if (file.MimeType is not null &&
                     file.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                images.Add(new DriveImageFile(
                    file.Id,
                    file.Name,
                    $"{prefix}{file.Name}",
                    file.MimeType,
                    file.Size,
                    file.ImageMediaMetadata?.Width,
                    file.ImageMediaMetadata?.Height,
                    file.Md5Checksum));
            }
        }
    }

    private Task<List<DriveData.File>> ListFoldersAsync(string parentId, CancellationToken ct)
        => ListChildrenAsync(parentId, $"mimeType = '{FolderMimeType}'", "nextPageToken, files(id,name)", ct);

    private async Task<List<DriveData.File>> ListChildrenAsync(
        string parentId, string? typeFilter, string fields, CancellationToken ct)
    {
        var query = $"'{parentId}' in parents and trashed = false";
        if (!string.IsNullOrEmpty(typeFilter))
            query += $" and {typeFilter}";

        var results = new List<DriveData.File>();
        string? pageToken = null;
        do
        {
            var request = Service.Files.List();
            request.Q = query;
            request.Fields = fields;
            request.PageSize = 1000;
            request.OrderBy = "name";
            request.PageToken = pageToken;

            var response = await request.ExecuteAsync(ct);
            if (response.Files is not null)
                results.AddRange(response.Files);
            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return results;
    }
}

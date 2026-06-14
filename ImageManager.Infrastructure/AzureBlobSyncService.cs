using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageManager.Application;
using ImageManager.Configuration;
using Microsoft.Extensions.Options;

namespace ImageManager.Infrastructure;

public sealed class AzureBlobSyncService : IBlobSyncService
{
    private readonly AzureStorageOptions _options;
    private readonly BlobServiceClient _client;

    public AzureBlobSyncService(BlobServiceClient client, IOptions<AzureStorageOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<string> UploadImageAsync(string blobName, byte[] content, string contentType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("A blob name is required.", nameof(blobName));

        var container = _client.GetBlobContainerClient(_options.WebContainer);
        var blob = container.GetBlobClient(_options.HolisticPrefix + blobName);

        using var stream = new MemoryStream(content);
        await blob.UploadAsync(stream, overwrite: true, ct);
        await blob.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        return GetPublicUrl(blobName);
    }

    public string GetPublicUrl(string blobName)
        => $"{_options.PublicBaseUrl.TrimEnd('/')}/{_options.HolisticPrefix.TrimStart('/')}{blobName}";
}

using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageManager.Application;
using ImageManager.Configuration;
using ImageManager.Domain;
using Microsoft.Extensions.Options;

namespace ImageManager.Infrastructure;

public sealed class JsonBlobMetadataStore : IMetadataStore
{
    private const string DbBlobName = "commissions.json";
    private const string BackupPrefix = "backups/";
    private const int MaxBackups = 20;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly AzureStorageOptions _options;
    private readonly BlobServiceClient _client;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public JsonBlobMetadataStore(BlobServiceClient client, IOptions<AzureStorageOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    private BlobContainerClient Container => _client.GetBlobContainerClient(_options.DataContainer);

    public async Task<CommissionDatabase> LoadAsync(CancellationToken ct = default)
    {
        var container = Container;
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        var blob = container.GetBlobClient(DbBlobName);
        if (!await blob.ExistsAsync(ct))
            return new CommissionDatabase();

        var download = await blob.DownloadContentAsync(ct);
        var json = download.Value.Content.ToString();
        return JsonSerializer.Deserialize<CommissionDatabase>(json, JsonOptions) ?? new CommissionDatabase();
    }

    public async Task SaveAsync(CommissionDatabase db, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(db, JsonOptions);

        await _saveLock.WaitAsync(ct);
        try
        {
            var container = Container;
            await container.CreateIfNotExistsAsync(cancellationToken: ct);
            var blob = container.GetBlobClient(DbBlobName);

            if (await blob.ExistsAsync(ct))
            {
                var current = await blob.DownloadContentAsync(ct);
                var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
                var backup = container.GetBlobClient($"{BackupPrefix}commissions-{stamp}.json");
                await backup.UploadAsync(current.Value.Content.ToStream(), overwrite: true, ct);
            }

            await blob.UploadAsync(BinaryData.FromString(json).ToStream(), overwrite: true, ct);
            await PruneBackupsAsync(container, ct);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private static async Task PruneBackupsAsync(BlobContainerClient container, CancellationToken ct)
    {
        var backups = new List<string>();
        await foreach (var item in container.GetBlobsAsync(BlobTraits.None, BlobStates.None, BackupPrefix, ct))
            backups.Add(item.Name);

        backups.Sort(StringComparer.Ordinal);
        for (var i = 0; i < backups.Count - MaxBackups; i++)
            await container.DeleteBlobIfExistsAsync(backups[i], cancellationToken: ct);
    }
}

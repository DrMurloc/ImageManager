using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageManager.Application;
using ImageManager.Configuration;
using ImageManager.Domain;
using Microsoft.Extensions.Options;

namespace ImageManager.Infrastructure;

// Notes live as one markdown blob per path in a private container. The blob name IS the
// note path, so Azure's virtual folders give us the tree for free. Both the web app and the
// MCP server reach this with the storage connection string, so notes are editable everywhere.
public sealed class BlobNoteStore : INoteStore
{
    private const string MarkdownContentType = "text/markdown; charset=utf-8";

    private readonly BlobServiceClient _client;
    private readonly AzureStorageOptions _options;

    public BlobNoteStore(BlobServiceClient client, IOptions<AzureStorageOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    private BlobContainerClient Container => _client.GetBlobContainerClient(_options.NotesContainer);

    private async Task<BlobContainerClient> ReadyContainerAsync(CancellationToken ct)
    {
        var container = Container;
        await container.CreateIfNotExistsAsync(cancellationToken: ct);
        return container;
    }

    public async Task<string?> ReadAsync(NotePath path, CancellationToken ct = default)
    {
        var blob = (await ReadyContainerAsync(ct)).GetBlobClient(path.Value);
        if (!await blob.ExistsAsync(ct))
            return null;

        var download = await blob.DownloadContentAsync(ct);
        return download.Value.Content.ToString();
    }

    public async Task WriteAsync(NotePath path, string content, CancellationToken ct = default)
    {
        var blob = (await ReadyContainerAsync(ct)).GetBlobClient(path.Value);
        await blob.UploadAsync(
            BinaryData.FromString(content).ToStream(),
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = MarkdownContentType } },
            ct);
    }

    public async Task<bool> DeleteAsync(NotePath path, CancellationToken ct = default)
    {
        var response = await (await ReadyContainerAsync(ct))
            .GetBlobClient(path.Value)
            .DeleteIfExistsAsync(cancellationToken: ct);
        return response.Value;
    }

    public async Task<bool> ExistsAsync(NotePath path, CancellationToken ct = default)
        => await (await ReadyContainerAsync(ct)).GetBlobClient(path.Value).ExistsAsync(ct);

    public async Task<NoteListing> ListAsync(string prefix, CancellationToken ct = default)
    {
        var container = await ReadyContainerAsync(ct);
        var p = string.IsNullOrEmpty(prefix) ? "" : prefix.TrimEnd('/') + "/";

        var folders = new List<string>();
        var notes = new List<NoteEntry>();
        await foreach (var item in container.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", p, ct))
        {
            if (item.IsPrefix)
                folders.Add(item.Prefix.TrimEnd('/'));
            else
                notes.Add(new NoteEntry(item.Blob.Name, NotePath.Parse(item.Blob.Name).Title));
        }

        return new NoteListing(p.TrimEnd('/'), folders, notes);
    }

    public async Task<IReadOnlyList<NotePath>> ListAllAsync(string prefix, CancellationToken ct = default)
    {
        var container = await ReadyContainerAsync(ct);
        var p = string.IsNullOrEmpty(prefix) ? "" : prefix.TrimEnd('/') + "/";

        var paths = new List<NotePath>();
        await foreach (var blob in container.GetBlobsAsync(BlobTraits.None, BlobStates.None, p, ct))
            paths.Add(NotePath.Parse(blob.Name));
        return paths;
    }
}

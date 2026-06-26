using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using ImageManager.Application;
using ImageManager.Configuration;
using ImageManager.Domain;
using Microsoft.Extensions.Options;

namespace ImageManager.Infrastructure;

public sealed class AzureNoteSearchIndex : INoteSearchIndex
{
    private readonly AzureSearchOptions _options;
    private readonly SemaphoreSlim _ensureLock = new(1, 1);
    private bool _ensured;

    public AzureNoteSearchIndex(IOptions<AzureSearchOptions> options)
    {
        _options = options.Value;
    }

    private AzureKeyCredential Credential => new(_options.ApiKey);
    private SearchIndexClient IndexClient => new(new Uri(_options.Endpoint), Credential);
    private SearchClient Documents => new(new Uri(_options.Endpoint), _options.NotesIndexName, Credential);

    public async Task EnsureIndexAsync(CancellationToken ct = default)
    {
        var index = new SearchIndex(_options.NotesIndexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("path") { IsFilterable = true, IsSortable = true },
                new SimpleField("folder", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SearchableField("title") { IsFilterable = true },
                new SearchableField("content"),
            }
        };

        await IndexClient.CreateOrUpdateIndexAsync(index, cancellationToken: ct);
        _ensured = true;
    }

    // Self-creates the index on first write, so notes can be saved from anywhere (e.g. the phone)
    // without a manual "create index" step first. CreateOrUpdate is idempotent; we just cache it.
    private async Task EnsureOnceAsync(CancellationToken ct)
    {
        if (_ensured) return;
        await _ensureLock.WaitAsync(ct);
        try
        {
            if (!_ensured)
                await EnsureIndexAsync(ct);
        }
        finally
        {
            _ensureLock.Release();
        }
    }

    public async Task IndexAsync(NotePath path, string content, CancellationToken ct = default)
    {
        await EnsureOnceAsync(ct);

        var doc = new SearchDocument
        {
            ["id"] = path.Key,
            ["path"] = path.Value,
            ["folder"] = path.Folder,
            ["title"] = path.Title,
            ["content"] = content
        };
        await Documents.MergeOrUploadDocumentsAsync(new[] { doc }, cancellationToken: ct);
    }

    public async Task DeleteAsync(NotePath path, CancellationToken ct = default)
        => await Documents.DeleteDocumentsAsync("id", new[] { path.Key }, cancellationToken: ct);
}

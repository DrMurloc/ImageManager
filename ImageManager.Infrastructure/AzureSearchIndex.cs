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

public sealed class AzureSearchIndex : ISearchIndex
{
    private readonly AzureSearchOptions _options;

    public AzureSearchIndex(IOptions<AzureSearchOptions> options)
    {
        _options = options.Value;
    }

    private AzureKeyCredential Credential => new(_options.ApiKey);
    private SearchIndexClient IndexClient => new(new Uri(_options.Endpoint), Credential);
    private SearchClient Documents => new(new Uri(_options.Endpoint), _options.IndexName, Credential);

    public async Task EnsureIndexAsync(CancellationToken ct = default)
    {
        var index = new SearchIndex(_options.IndexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("content"),
                new SearchableField("chapterName") { IsFilterable = true },
                new SimpleField("book", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("chapterNumber", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SimpleField("chunkIndex", SearchFieldDataType.Int32) { IsSortable = true },
            }
        };

        await IndexClient.CreateOrUpdateIndexAsync(index, cancellationToken: ct);
    }

    public async Task IndexChapterAsync(string book, int chapterNumber, string chapterName, IReadOnlyList<string> chunks, CancellationToken ct = default)
    {
        var client = Documents;

        // Drop any previously-indexed chunks for this chapter so re-indexing can't leave orphans.
        var stale = await client.SearchAsync<SearchDocument>("*", new SearchOptions
        {
            Filter = $"book eq '{Escape(book)}' and chapterNumber eq {chapterNumber}",
            Select = { "id" },
            Size = 1000
        }, ct);

        var staleKeys = new List<string>();
        await foreach (var result in stale.Value.GetResultsAsync())
            staleKeys.Add((string)result.Document["id"]);
        if (staleKeys.Count > 0)
            await client.DeleteDocumentsAsync("id", staleKeys, cancellationToken: ct);

        if (chunks.Count == 0) return;

        var docs = chunks.Select((chunk, i) => new SearchDocument
        {
            ["id"] = BookSearchDocs.ChunkKey(book, chapterNumber, i),
            ["book"] = book,
            ["chapterNumber"] = chapterNumber,
            ["chapterName"] = chapterName,
            ["chunkIndex"] = i,
            ["content"] = chunk
        });
        await client.MergeOrUploadDocumentsAsync(docs, cancellationToken: ct);
    }

    // OData string literals double single quotes.
    private static string Escape(string value) => value.Replace("'", "''");
}

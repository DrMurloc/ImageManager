using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ImageManager.Application;
using ImageManager.Configuration;
using Microsoft.Extensions.Options;

namespace ImageManager.Infrastructure;

public sealed class AzureNoteSearchQuery : INoteSearchQuery
{
    private readonly AzureSearchOptions _options;

    public AzureNoteSearchQuery(IOptions<AzureSearchOptions> options)
    {
        _options = options.Value;
    }

    private SearchClient Client => new(new Uri(_options.Endpoint), _options.NotesIndexName, new AzureKeyCredential(_options.ApiKey));

    public async Task<IReadOnlyList<NoteSearchHit>> SearchAsync(string query, int top, CancellationToken ct = default)
    {
        var options = new SearchOptions
        {
            Size = top <= 0 ? 8 : top,
            Select = { "path", "title", "content" }
        };

        var response = await Client.SearchAsync<SearchDocument>(query, options, ct);

        var hits = new List<NoteSearchHit>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            var d = result.Document;
            hits.Add(new NoteSearchHit(
                (string)d["path"],
                (string)d["title"],
                (string)d["content"],
                result.Score ?? 0));
        }
        return hits;
    }
}

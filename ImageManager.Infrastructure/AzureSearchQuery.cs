using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ImageManager.Application;
using ImageManager.Configuration;
using Microsoft.Extensions.Options;

namespace ImageManager.Infrastructure;

public sealed class AzureSearchQuery : ISearchQuery
{
    private readonly AzureSearchOptions _options;

    public AzureSearchQuery(IOptions<AzureSearchOptions> options)
    {
        _options = options.Value;
    }

    private SearchClient Client => new(new Uri(_options.Endpoint), _options.IndexName, new AzureKeyCredential(_options.ApiKey));

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, string? book, int top, CancellationToken ct = default)
    {
        var options = new SearchOptions
        {
            Size = top <= 0 ? 8 : top,
            Select = { "book", "chapterNumber", "chapterName", "chunkIndex", "content" }
        };
        if (!string.IsNullOrWhiteSpace(book))
            options.Filter = $"book eq '{Escape(book)}'";

        var response = await Client.SearchAsync<SearchDocument>(query, options, ct);

        var hits = new List<SearchHit>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            var d = result.Document;
            hits.Add(new SearchHit(
                (string)d["book"],
                Convert.ToInt32(d["chapterNumber"]),
                (string)d["chapterName"],
                Convert.ToInt32(d["chunkIndex"]),
                (string)d["content"],
                result.Score ?? 0));
        }
        return hits;
    }

    public async Task<ChapterText?> GetChapterAsync(string book, int chapterNumber, CancellationToken ct = default)
    {
        var options = new SearchOptions
        {
            Filter = $"book eq '{Escape(book)}' and chapterNumber eq {chapterNumber}",
            OrderBy = { "chunkIndex" },
            Select = { "chapterName", "chunkIndex", "content" },
            Size = 1000
        };

        var response = await Client.SearchAsync<SearchDocument>("*", options, ct);

        var chunks = new List<string>();
        var chapterName = "";
        await foreach (var result in response.Value.GetResultsAsync())
        {
            chunks.Add((string)result.Document["content"]);
            chapterName = (string)result.Document["chapterName"];
        }

        return chunks.Count == 0
            ? null
            : new ChapterText(book, chapterNumber, chapterName, string.Join("\n\n", chunks));
    }

    private static string Escape(string value) => value.Replace("'", "''");
}

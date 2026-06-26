namespace ImageManager.Application;

// Orchestrates the notes store and its search index so writes stay indexed.
// This is the single seam the web app and the MCP server both use.
public interface INoteService
{
    Task EnsureIndexAsync(CancellationToken ct = default);

    Task<Note?> ReadAsync(string path, CancellationToken ct = default);

    Task SaveAsync(string path, string content, CancellationToken ct = default);

    // True when a note existed and was removed.
    Task<bool> DeleteAsync(string path, CancellationToken ct = default);

    Task<NoteListing> ListAsync(string prefix, CancellationToken ct = default);

    Task<IReadOnlyList<NoteSearchHit>> SearchAsync(string query, int top, CancellationToken ct = default);

    // Rebuilds the search index from the store; returns the number of notes indexed.
    Task<int> ReindexAllAsync(CancellationToken ct = default);
}

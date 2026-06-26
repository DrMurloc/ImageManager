using ImageManager.Domain;

namespace ImageManager.Application;

// Persistent home for note files (markdown blobs). Reachable with an app-level credential
// from both the web app and the MCP server, so notes can be edited from computer and phone.
public interface INoteStore
{
    Task<string?> ReadAsync(NotePath path, CancellationToken ct = default);

    Task WriteAsync(NotePath path, string content, CancellationToken ct = default);

    // True when a note existed and was removed.
    Task<bool> DeleteAsync(NotePath path, CancellationToken ct = default);

    Task<bool> ExistsAsync(NotePath path, CancellationToken ct = default);

    // Immediate children under a folder prefix ("" = root): child folder prefixes + note files.
    Task<NoteListing> ListAsync(string prefix, CancellationToken ct = default);

    // Every note path under a prefix, recursively (used to rebuild the search index).
    Task<IReadOnlyList<NotePath>> ListAllAsync(string prefix, CancellationToken ct = default);
}

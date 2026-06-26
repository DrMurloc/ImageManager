using ImageManager.Domain;

namespace ImageManager.Application;

// Orchestrates todo use-cases over the repository. Single seam for the web app and MCP server.
public interface ITodoService
{
    Task<IReadOnlyList<Todo>> ListAsync(TodoFilter filter, CancellationToken ct = default);

    Task<Todo> AddAsync(string book, string title, int? chapterNumber = null, string? notes = null, CancellationToken ct = default);

    // Marks a todo done (or reopens it). Returns the updated todo, or null if the id is unknown.
    Task<Todo?> SetDoneAsync(Guid id, bool done, CancellationToken ct = default);

    // Updates the provided fields only (null = leave unchanged). Returns null if the id is unknown.
    Task<Todo?> UpdateAsync(Guid id, string? title = null, string? notes = null, int? order = null, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

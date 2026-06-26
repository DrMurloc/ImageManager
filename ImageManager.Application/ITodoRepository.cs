using ImageManager.Domain;

namespace ImageManager.Application;

// Filter for listing todos. Null members are "don't care".
public sealed record TodoFilter(
    string? Book = null,
    int? ChapterNumber = null,
    TodoScope? Scope = null,
    bool IncludeDone = false);

// Persistence port for todos (backed by EF Core / SQL in Infrastructure).
public interface ITodoRepository
{
    Task<IReadOnlyList<Todo>> ListAsync(TodoFilter filter, CancellationToken ct = default);
    Task<Todo?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Todo todo, CancellationToken ct = default);
    Task UpdateAsync(Todo todo, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

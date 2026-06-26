using ImageManager.Application;
using ImageManager.Domain;

namespace ImageManager.Infrastructure;

// Registered when Todos:SqlConnectionString is absent, so the app still boots and the todos
// feature fails with a clear message instead of an opaque DI/connection error.
public sealed class UnconfiguredTodoRepository : ITodoRepository
{
    private static InvalidOperationException NotConfigured()
        => new("Todos are not configured. Set Todos:SqlConnectionString (User Secrets locally, or app settings when deployed).");

    public Task<IReadOnlyList<Todo>> ListAsync(TodoFilter filter, CancellationToken ct = default) => throw NotConfigured();
    public Task<Todo?> GetAsync(Guid id, CancellationToken ct = default) => throw NotConfigured();
    public Task AddAsync(Todo todo, CancellationToken ct = default) => throw NotConfigured();
    public Task UpdateAsync(Todo todo, CancellationToken ct = default) => throw NotConfigured();
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => throw NotConfigured();
}

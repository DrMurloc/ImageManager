using ImageManager.Domain;

namespace ImageManager.Application;

public sealed class TodoService : ITodoService
{
    private readonly ITodoRepository _repo;

    public TodoService(ITodoRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<Todo>> ListAsync(TodoFilter filter, CancellationToken ct = default)
        => _repo.ListAsync(filter, ct);

    public async Task<Todo> AddAsync(string book, string title, int? chapterNumber = null, string? notes = null, CancellationToken ct = default)
    {
        // Append after the existing todos in the same list (book-level vs this chapter).
        var siblings = await _repo.ListAsync(new TodoFilter(
            Book: book,
            ChapterNumber: chapterNumber,
            Scope: chapterNumber is null ? TodoScope.Book : TodoScope.Chapter,
            IncludeDone: true), ct);
        var order = siblings.Count == 0 ? 0 : siblings.Max(t => t.Order) + 1;

        var todo = Todo.Create(book, title, chapterNumber, notes, order, DateTimeOffset.UtcNow);
        await _repo.AddAsync(todo, ct);
        return todo;
    }

    public async Task<Todo?> SetDoneAsync(Guid id, bool done, CancellationToken ct = default)
    {
        var todo = await _repo.GetAsync(id, ct);
        if (todo is null)
            return null;

        if (done)
            todo.Complete(DateTimeOffset.UtcNow);
        else
            todo.Reopen();

        await _repo.UpdateAsync(todo, ct);
        return todo;
    }

    public async Task<Todo?> UpdateAsync(Guid id, string? title = null, string? notes = null, int? order = null, CancellationToken ct = default)
    {
        var todo = await _repo.GetAsync(id, ct);
        if (todo is null)
            return null;

        if (!string.IsNullOrWhiteSpace(title))
            todo.Title = title.Trim();
        if (notes is not null)
            todo.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (order is not null)
            todo.Order = order.Value;

        await _repo.UpdateAsync(todo, ct);
        return todo;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        => _repo.DeleteAsync(id, ct);
}

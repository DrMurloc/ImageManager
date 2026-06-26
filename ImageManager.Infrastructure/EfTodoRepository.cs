using ImageManager.Application;
using ImageManager.Domain;
using Microsoft.EntityFrameworkCore;

namespace ImageManager.Infrastructure;

public sealed class EfTodoRepository : ITodoRepository
{
    private readonly BooksDbContext _db;

    public EfTodoRepository(BooksDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Todo>> ListAsync(TodoFilter filter, CancellationToken ct = default)
    {
        IQueryable<Todo> q = _db.Todos.AsNoTracking();

        if (filter.Book is not null)
            q = q.Where(t => t.Book == filter.Book);
        if (filter.ChapterNumber is not null)
            q = q.Where(t => t.ChapterNumber == filter.ChapterNumber);
        if (filter.Scope == TodoScope.Book)
            q = q.Where(t => t.ChapterNumber == null);
        else if (filter.Scope == TodoScope.Chapter)
            q = q.Where(t => t.ChapterNumber != null);
        if (!filter.IncludeDone)
            q = q.Where(t => !t.Done);

        return await q.OrderBy(t => t.Order).ThenBy(t => t.CreatedUtc).ToListAsync(ct);
    }

    public async Task<Todo?> GetAsync(Guid id, CancellationToken ct = default)
        => await _db.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(Todo todo, CancellationToken ct = default)
    {
        _db.Todos.Add(todo);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Todo todo, CancellationToken ct = default)
    {
        _db.Todos.Update(todo);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _db.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (existing is null)
            return false;

        _db.Todos.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

using ImageManager.Domain;

namespace ImageManager.Application;

public sealed class NoteService : INoteService
{
    private readonly INoteStore _store;
    private readonly INoteSearchIndex _index;
    private readonly INoteSearchQuery _search;

    public NoteService(INoteStore store, INoteSearchIndex index, INoteSearchQuery search)
    {
        _store = store;
        _index = index;
        _search = search;
    }

    public Task EnsureIndexAsync(CancellationToken ct = default) => _index.EnsureIndexAsync(ct);

    public async Task<Note?> ReadAsync(string path, CancellationToken ct = default)
    {
        var notePath = NotePath.Parse(path);
        var content = await _store.ReadAsync(notePath, ct);
        return content is null ? null : new Note(notePath.Value, content);
    }

    public async Task SaveAsync(string path, string content, CancellationToken ct = default)
    {
        var notePath = NotePath.Parse(path);
        // Write the file first, then keep the index in step (write-through).
        await _store.WriteAsync(notePath, content, ct);
        await _index.IndexAsync(notePath, content, ct);
    }

    public async Task<bool> DeleteAsync(string path, CancellationToken ct = default)
    {
        var notePath = NotePath.Parse(path);
        var existed = await _store.DeleteAsync(notePath, ct);
        if (existed)
            await _index.DeleteAsync(notePath, ct);
        return existed;
    }

    public Task<NoteListing> ListAsync(string prefix, CancellationToken ct = default)
        => _store.ListAsync(prefix ?? "", ct);

    public Task<IReadOnlyList<NoteSearchHit>> SearchAsync(string query, int top, CancellationToken ct = default)
        => _search.SearchAsync(query, top <= 0 ? 8 : top, ct);

    public async Task<int> ReindexAllAsync(CancellationToken ct = default)
    {
        await _index.EnsureIndexAsync(ct);
        var paths = await _store.ListAllAsync("", ct);
        foreach (var path in paths)
        {
            var content = await _store.ReadAsync(path, ct);
            if (content is not null)
                await _index.IndexAsync(path, content, ct);
        }
        return paths.Count;
    }
}

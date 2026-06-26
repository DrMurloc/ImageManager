namespace ImageManager.Application;

// Read side of the notes search index.
public interface INoteSearchQuery
{
    Task<IReadOnlyList<NoteSearchHit>> SearchAsync(string query, int top, CancellationToken ct = default);
}

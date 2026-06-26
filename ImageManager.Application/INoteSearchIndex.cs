using ImageManager.Domain;

namespace ImageManager.Application;

// Write side of the dedicated notes search index (kept separate from book chapters).
public interface INoteSearchIndex
{
    Task EnsureIndexAsync(CancellationToken ct = default);

    Task IndexAsync(NotePath path, string content, CancellationToken ct = default);

    Task DeleteAsync(NotePath path, CancellationToken ct = default);
}

using ImageManager.Domain;

namespace ImageManager.Application;

public interface IMetadataStore
{
    Task<CommissionDatabase> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(CommissionDatabase db, CancellationToken ct = default);
}

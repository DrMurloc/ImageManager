namespace ImageManager.Application;

// Orchestrates creating a commission group: makes the Drive subfolder, uploads the images,
// resolves-or-creates the artist, and persists the group with the artist assigned.
public interface ICommissionGroupService
{
    Task<CreateGroupResult> CreateGroupAsync(CreateGroupRequest request, CancellationToken ct = default);
}

// One image to upload. Content is buffered so the Application layer never sees a browser/HTTP type.
public sealed record UploadFile(string Name, string ContentType, byte[] Content);

public sealed record CreateGroupRequest(
    string AccessToken,
    string CollectionId,
    string CollectionName,
    string Description,
    string ArtistName,
    string? NewArtistCreditUrl,
    IReadOnlyList<UploadFile> Files);

public sealed record CreateGroupResult(string FolderId, string Subfolder, int Uploaded);

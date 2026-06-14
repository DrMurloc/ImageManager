using ImageManager.Domain;

namespace ImageManager.Application;

public sealed class CommissionGroupService : ICommissionGroupService
{
    private readonly IDriveUploader _uploader;
    private readonly IMetadataStore _store;

    public CommissionGroupService(IDriveUploader uploader, IMetadataStore store)
    {
        _uploader = uploader;
        _store = store;
    }

    public async Task<CreateGroupResult> CreateGroupAsync(CreateGroupRequest request, CancellationToken ct = default)
    {
        // Load fresh and save here so the write isn't racing a stale copy held by the UI.
        var db = await _store.LoadAsync(ct);

        var artist = db.Artists.FirstOrDefault(a =>
            string.Equals(a.Name.Trim(), request.ArtistName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (artist is null)
        {
            artist = new Artist { Name = request.ArtistName.Trim(), CreditUrl = request.NewArtistCreditUrl?.Trim() ?? "" };
            db.Artists.Add(artist);
        }

        var subfolder = CommissionNaming.SubfolderName(request.Description, request.ArtistName);
        var folderId = await _uploader.CreateFolderAsync(request.AccessToken, request.CollectionId, subfolder, ct);

        var uploaded = 0;
        foreach (var file in request.Files)
        {
            using var content = new MemoryStream(file.Content);
            await _uploader.UploadAsync(request.AccessToken, folderId, file.Name, file.ContentType, content, ct);
            uploaded++;
        }

        db.Groups.Add(new CommissionGroup
        {
            DriveFolderId = folderId,
            CharacterNames = request.CollectionName,
            GroupName = subfolder,
            ArtistId = artist.Id
        });
        await _store.SaveAsync(db, ct);

        return new CreateGroupResult(folderId, subfolder, uploaded);
    }
}

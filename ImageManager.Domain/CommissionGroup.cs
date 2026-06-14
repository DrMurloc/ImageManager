namespace ImageManager.Domain;

public sealed class CommissionGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DriveFolderId { get; set; } = "";
    public string CharacterNames { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string? ArtistId { get; set; }
    public List<ImageAsset> Images { get; set; } = new();
}

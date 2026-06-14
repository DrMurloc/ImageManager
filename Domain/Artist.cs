namespace ImageManager.Domain;

public sealed class Artist
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string CreditUrl { get; set; } = "";
}

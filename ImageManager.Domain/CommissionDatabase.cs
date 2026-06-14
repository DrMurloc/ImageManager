namespace ImageManager.Domain;

public sealed class CommissionDatabase
{
    public List<Artist> Artists { get; set; } = new();
    public List<CommissionGroup> Groups { get; set; } = new();
    public List<Ao3Chapter> Chapters { get; set; } = new();
}

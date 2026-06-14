namespace ImageManager.Domain;

public sealed class Ao3Chapter
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";

    // Box that gallery images are scaled to fit within (px), preserving aspect ratio.
    public int GalleryMaxWidth { get; set; } = 250;
    public int GalleryMaxHeight { get; set; } = 250;
}

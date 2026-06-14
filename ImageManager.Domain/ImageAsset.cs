namespace ImageManager.Domain;

public enum ImageSize { Small, Medium, Large, ExtraLarge }

public enum GallerySection { Header, Midsection, Footer }

public sealed class ImageAsset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DriveFileId { get; set; } = "";
    public string SourceFileName { get; set; } = "";
    public string? BlobName { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? AltText { get; set; }
    public bool Synced { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }

    // Drive md5 captured at last sync; compared with the live md5 to flag source drift.
    public string? SyncedMd5 { get; set; }

    public List<string> Ao3ChapterIds { get; set; } = new();

    // Chapters the image has actually been embedded on in AO3 (confirmed by the user after the fact).
    public List<string> AddedToChapterIds { get; set; } = new();

    // Scales how much gallery space the image gets, relative to the chapter's configured box.
    public ImageSize Size { get; set; } = ImageSize.Medium;

    // Which gallery section the image sits in, per chapter (keyed by chapter Id). Defaults to Midsection.
    public Dictionary<string, GallerySection> ChapterSections { get; set; } = new();

    // True when this image was synced but its Drive source has since changed (md5 mismatch).
    // Returns false unless both the synced and live checksums are known.
    public bool IsDriftedFrom(string? liveMd5)
        => Synced && SyncedMd5 is not null && liveMd5 is not null && SyncedMd5 != liveMd5;

    // Replace the linked AO3 chapters, pruning per-chapter state (placement confirmations and
    // section assignments) for chapters that are no longer linked.
    public void SetChapters(IEnumerable<string> chapterIds)
    {
        var ids = chapterIds.ToList();
        Ao3ChapterIds = new List<string>(ids);
        AddedToChapterIds.RemoveAll(id => !ids.Contains(id));
        foreach (var staleId in ChapterSections.Keys.Where(k => !ids.Contains(k)).ToList())
            ChapterSections.Remove(staleId);
    }
}

using ImageManager.Domain;

namespace ImageManager.Application;

public interface IHtmlBuilder
{
    string BuildSnippet(ImageAsset image, Artist? artist, string imageUrl);

    // Stacked, centered gallery: each image scaled to fit within maxWidth x maxHeight, followed by its artist credit.
    string BuildGallery(IReadOnlyList<GalleryItem> items, int maxWidth, int maxHeight);
}

public sealed record GalleryItem(ImageAsset Image, Artist? Artist, string ImageUrl);

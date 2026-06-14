using System.Net;
using System.Text;
using ImageManager.Domain;

namespace ImageManager.Application;

public sealed class HtmlBuilder : IHtmlBuilder
{
    public string BuildSnippet(ImageAsset image, Artist? artist, string imageUrl)
        => BuildBlock(imageUrl, image.AltText, image.Width, image.Height, artist);

    public string BuildGallery(IReadOnlyList<GalleryItem> items, int maxWidth, int maxHeight)
    {
        var blocks = new List<string>();
        foreach (var item in items)
        {
            var factor = SizeMultiplier(item.Image.Size);
            var (w, h) = Scale(item.Image.Width, item.Image.Height,
                (int)Math.Round(maxWidth * factor), (int)Math.Round(maxHeight * factor));
            blocks.Add(BuildBlock(item.ImageUrl, item.Image.AltText, w, h, item.Artist));
        }
        return string.Join("\n\n", blocks);
    }

    private static string BuildBlock(string imageUrl, string? altText, int? width, int? height, Artist? artist)
    {
        var alt = WebUtility.HtmlEncode(altText ?? "");

        var html = new StringBuilder();
        html.Append($"<p align=\"center\"><img src=\"{imageUrl}\" alt=\"{alt}\"{Dimensions(width, height)}></p>");

        if (!string.IsNullOrWhiteSpace(artist?.CreditUrl))
        {
            html.Append("\n\n");
            html.Append($"<p align=\"center\">({artist!.CreditUrl})</p>");
        }

        return html.ToString();
    }

    private static string Dimensions(int? width, int? height)
    {
        var sb = new StringBuilder();
        if (width is int w) sb.Append($" width=\"{w}\"");
        if (height is int h) sb.Append($" height=\"{h}\"");
        return sb.ToString();
    }

    private static double SizeMultiplier(ImageSize size) => size switch
    {
        ImageSize.Small => 0.5,
        ImageSize.Large => 1.5,
        ImageSize.ExtraLarge => 2.0,
        _ => 1.0
    };

    // Scale original dimensions to fit within the box, preserving aspect ratio and never upscaling.
    // With no usable original dimensions, fall back to a width-only hint so the browser keeps the ratio.
    private static (int? width, int? height) Scale(int? origW, int? origH, int maxW, int maxH)
    {
        if (origW is not int w || origH is not int h || w <= 0 || h <= 0)
            return (maxW > 0 ? maxW : null, null);

        var scale = Math.Min(1.0, Math.Min((double)maxW / w, (double)maxH / h));
        return ((int)Math.Round(w * scale), (int)Math.Round(h * scale));
    }
}

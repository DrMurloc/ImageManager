using ImageManager.Application;
using ImageManager.Domain;

namespace ImageManager.Tests;

// Characterization tests: they lock the current HTML output and gallery scaling math so the
// upcoming move of HtmlBuilder into the Application layer is provably behavior-preserving.
public class HtmlBuilderTests
{
    private readonly HtmlBuilder _sut = new();

    [Fact]
    public void BuildSnippet_WithDimensionsAndCredit_EmitsImageThenCredit()
    {
        var image = new ImageAsset { AltText = "Hello", Width = 300, Height = 200 };
        var artist = new Artist { Name = "Jane", CreditUrl = "https://ko-fi.com/jane" };

        var html = _sut.BuildSnippet(image, artist, "https://cdn/x.png");

        Assert.Equal(
            "<p align=\"center\"><img src=\"https://cdn/x.png\" alt=\"Hello\" width=\"300\" height=\"200\"></p>"
            + "\n\n<p align=\"center\">(https://ko-fi.com/jane)</p>",
            html);
    }

    [Fact]
    public void BuildSnippet_NoArtist_OmitsCreditParagraph()
    {
        var image = new ImageAsset { AltText = "Hi", Width = 10, Height = 20 };

        var html = _sut.BuildSnippet(image, null, "u");

        Assert.Equal("<p align=\"center\"><img src=\"u\" alt=\"Hi\" width=\"10\" height=\"20\"></p>", html);
    }

    [Fact]
    public void BuildSnippet_ArtistWithBlankCreditUrl_OmitsCreditParagraph()
    {
        var image = new ImageAsset { AltText = "Hi", Width = 10, Height = 20 };
        var artist = new Artist { Name = "Jane", CreditUrl = "  " };

        var html = _sut.BuildSnippet(image, artist, "u");

        Assert.DoesNotContain("<p align=\"center\">(", html);
    }

    [Fact]
    public void BuildSnippet_NullAltText_RendersEmptyAlt()
    {
        var image = new ImageAsset { AltText = null, Width = 10, Height = 20 };

        var html = _sut.BuildSnippet(image, null, "u");

        Assert.Contains("alt=\"\"", html);
    }

    [Fact]
    public void BuildSnippet_EncodesHtmlSpecialCharactersInAltText()
    {
        var image = new ImageAsset { AltText = "A & B <c>", Width = 10, Height = 20 };

        var html = _sut.BuildSnippet(image, null, "u");

        Assert.Contains("alt=\"A &amp; B &lt;c&gt;\"", html);
    }

    [Fact]
    public void BuildSnippet_WidthOnly_EmitsOnlyWidthAttribute()
    {
        var image = new ImageAsset { AltText = "x", Width = 120, Height = null };

        var html = _sut.BuildSnippet(image, null, "u");

        Assert.Equal("<p align=\"center\"><img src=\"u\" alt=\"x\" width=\"120\"></p>", html);
    }

    [Fact]
    public void BuildSnippet_NoDimensions_EmitsNoSizeAttributes()
    {
        var image = new ImageAsset { AltText = "x", Width = null, Height = null };

        var html = _sut.BuildSnippet(image, null, "u");

        Assert.Equal("<p align=\"center\"><img src=\"u\" alt=\"x\"></p>", html);
    }

    [Fact]
    public void BuildGallery_ScalesDownToFitBoxPreservingAspectRatio()
    {
        // scale = min(1, min(250/1000, 250/500)) = 0.25 -> 250x125
        var item = new GalleryItem(
            new ImageAsset { AltText = "a", Width = 1000, Height = 500, Size = ImageSize.Medium }, null, "u");

        var html = _sut.BuildGallery(new[] { item }, 250, 250);

        Assert.Equal("<p align=\"center\"><img src=\"u\" alt=\"a\" width=\"250\" height=\"125\"></p>", html);
    }

    [Fact]
    public void BuildGallery_NeverUpscalesSmallImages()
    {
        var item = new GalleryItem(
            new ImageAsset { AltText = "a", Width = 100, Height = 100, Size = ImageSize.Medium }, null, "u");

        var html = _sut.BuildGallery(new[] { item }, 250, 250);

        Assert.Equal("<p align=\"center\"><img src=\"u\" alt=\"a\" width=\"100\" height=\"100\"></p>", html);
    }

    [Theory]
    [InlineData(ImageSize.Small, 125)]
    [InlineData(ImageSize.Medium, 250)]
    [InlineData(ImageSize.Large, 375)]
    [InlineData(ImageSize.ExtraLarge, 500)]
    public void BuildGallery_AppliesSizeMultiplierToBox(ImageSize size, int expected)
    {
        // 1000x1000 source against a 250 box; source always larger, so output == scaled box.
        var item = new GalleryItem(
            new ImageAsset { AltText = "a", Width = 1000, Height = 1000, Size = size }, null, "u");

        var html = _sut.BuildGallery(new[] { item }, 250, 250);

        Assert.Equal(
            $"<p align=\"center\"><img src=\"u\" alt=\"a\" width=\"{expected}\" height=\"{expected}\"></p>",
            html);
    }

    [Fact]
    public void BuildGallery_MissingSourceDimensions_FallsBackToWidthOnlyHint()
    {
        var item = new GalleryItem(
            new ImageAsset { AltText = "a", Width = null, Height = null, Size = ImageSize.Medium }, null, "u");

        var html = _sut.BuildGallery(new[] { item }, 250, 250);

        Assert.Equal("<p align=\"center\"><img src=\"u\" alt=\"a\" width=\"250\"></p>", html);
    }

    [Fact]
    public void BuildGallery_JoinsMultipleBlocksWithBlankLine()
    {
        var items = new[]
        {
            new GalleryItem(new ImageAsset { AltText = "a", Width = 100, Height = 100, Size = ImageSize.Medium }, null, "u1"),
            new GalleryItem(new ImageAsset { AltText = "b", Width = 100, Height = 100, Size = ImageSize.Medium }, null, "u2"),
        };

        var html = _sut.BuildGallery(items, 250, 250);

        Assert.Equal(
            "<p align=\"center\"><img src=\"u1\" alt=\"a\" width=\"100\" height=\"100\"></p>"
            + "\n\n"
            + "<p align=\"center\"><img src=\"u2\" alt=\"b\" width=\"100\" height=\"100\"></p>",
            html);
    }
}

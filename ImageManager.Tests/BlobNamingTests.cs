using ImageManager.Domain;

namespace ImageManager.Tests;

// Characterization tests for the blob-name derivation extracted from GroupDetail.
public class BlobNamingTests
{
    [Fact]
    public void StripsArtistSuffixFromGroupName_AndPrefixesWithArtistFolder()
    {
        var name = BlobNaming.DefaultBlobName("BeachDay_ArtistX", "Artist X", "photo.png", "image/png", 0);

        Assert.Equal("ArtistX/BeachDay1.png", name);
    }

    [Fact]
    public void IndexIsOneBased()
    {
        var name = BlobNaming.DefaultBlobName("BeachDay_ArtistX", "ArtistX", "photo.png", "image/png", 4);

        Assert.Equal("ArtistX/BeachDay5.png", name);
    }

    [Fact]
    public void NoArtist_OmitsFolderPrefix()
    {
        var name = BlobNaming.DefaultBlobName("SomeFolder", null, "a.jpg", "image/jpeg", 2);

        Assert.Equal("SomeFolder3.jpg", name);
    }

    [Fact]
    public void MissingFileExtension_FallsBackToMimeSubtype()
    {
        var name = BlobNaming.DefaultBlobName("X_A", "A", "noext", "image/webp", 0);

        Assert.Equal("A/X1.webp", name);
    }

    [Fact]
    public void MissingExtensionAndMalformedMime_YieldsNoExtension()
    {
        var name = BlobNaming.DefaultBlobName("G", null, "noext", "octet", 0);

        Assert.Equal("G1", name);
    }

    [Fact]
    public void DescriptionSanitization_DropsDashesAndSpacesButKeepsUnderscores()
    {
        var name = BlobNaming.DefaultBlobName("Astrid-and Cale_ArtistX", "ArtistX", "x.png", "image/png", 0);

        Assert.Equal("ArtistX/AstridandCale1.png", name);
    }

    [Fact]
    public void DescriptionWithUnderscores_ArePreserved()
    {
        var name = BlobNaming.DefaultBlobName("my_desc_ArtistX", "ArtistX", "x.png", "image/png", 0);

        Assert.Equal("ArtistX/my_desc1.png", name);
    }

    [Fact]
    public void ArtistSuffixMatchIsCaseInsensitive()
    {
        var name = BlobNaming.DefaultBlobName("Beach_artistx", "ArtistX", "x.png", "image/png", 0);

        Assert.Equal("ArtistX/Beach1.png", name);
    }

    [Fact]
    public void EmptyDescriptionAfterStrippingSuffix_FallsBackToImage()
    {
        var name = BlobNaming.DefaultBlobName("_ArtistX", "ArtistX", "x.png", "image/png", 0);

        Assert.Equal("ArtistX/Image1.png", name);
    }

    [Fact]
    public void DescriptionWithNoUsableCharacters_FallsBackToImage()
    {
        var name = BlobNaming.DefaultBlobName("---", null, "x.png", "image/png", 0);

        Assert.Equal("Image1.png", name);
    }
}

using ImageManager.Domain;

namespace ImageManager.Tests;

// Characterization tests for the commission folder/blob naming rules.
public class CommissionNamingTests
{
    [Theory]
    [InlineData("BeachDay", true)]
    [InlineData("Beach_Day_2", true)]
    [InlineData("Beach Day", false)]
    [InlineData("Beach-Day", false)]
    [InlineData("", false)]
    public void IsValidDescription_AllowsAlphanumericAndUnderscoreOnly(string description, bool expected)
        => Assert.Equal(expected, CommissionNaming.IsValidDescription(description));

    [Theory]
    [InlineData("Artist X", "ArtistX")]
    [InlineData("a-b_c.d", "abcd")]
    [InlineData(null, "")]
    public void SanitizeArtist_StripsNonAlphanumerics(string? input, string expected)
        => Assert.Equal(expected, CommissionNaming.SanitizeArtist(input));

    [Fact]
    public void SubfolderName_CombinesDescriptionAndSanitizedArtist()
        => Assert.Equal("BeachDay_ArtistX", CommissionNaming.SubfolderName("BeachDay", "Artist X"));

    [Fact]
    public void StripsArtistSuffixFromGroupName_AndPrefixesWithArtistFolder()
    {
        var name = CommissionNaming.DefaultBlobName("BeachDay_ArtistX", "Artist X", "photo.png", "image/png", 0);

        Assert.Equal("ArtistX/BeachDay1.png", name);
    }

    [Fact]
    public void IndexIsOneBased()
    {
        var name = CommissionNaming.DefaultBlobName("BeachDay_ArtistX", "ArtistX", "photo.png", "image/png", 4);

        Assert.Equal("ArtistX/BeachDay5.png", name);
    }

    [Fact]
    public void NoArtist_OmitsFolderPrefix()
    {
        var name = CommissionNaming.DefaultBlobName("SomeFolder", null, "a.jpg", "image/jpeg", 2);

        Assert.Equal("SomeFolder3.jpg", name);
    }

    [Fact]
    public void MissingFileExtension_FallsBackToMimeSubtype()
    {
        var name = CommissionNaming.DefaultBlobName("X_A", "A", "noext", "image/webp", 0);

        Assert.Equal("A/X1.webp", name);
    }

    [Fact]
    public void MissingExtensionAndMalformedMime_YieldsNoExtension()
    {
        var name = CommissionNaming.DefaultBlobName("G", null, "noext", "octet", 0);

        Assert.Equal("G1", name);
    }

    [Fact]
    public void DescriptionSanitization_DropsDashesAndSpacesButKeepsUnderscores()
    {
        var name = CommissionNaming.DefaultBlobName("Astrid-and Cale_ArtistX", "ArtistX", "x.png", "image/png", 0);

        Assert.Equal("ArtistX/AstridandCale1.png", name);
    }

    [Fact]
    public void DescriptionWithUnderscores_ArePreserved()
    {
        var name = CommissionNaming.DefaultBlobName("my_desc_ArtistX", "ArtistX", "x.png", "image/png", 0);

        Assert.Equal("ArtistX/my_desc1.png", name);
    }

    [Fact]
    public void ArtistSuffixMatchIsCaseInsensitive()
    {
        var name = CommissionNaming.DefaultBlobName("Beach_artistx", "ArtistX", "x.png", "image/png", 0);

        Assert.Equal("ArtistX/Beach1.png", name);
    }

    [Fact]
    public void EmptyDescriptionAfterStrippingSuffix_FallsBackToImage()
    {
        var name = CommissionNaming.DefaultBlobName("_ArtistX", "ArtistX", "x.png", "image/png", 0);

        Assert.Equal("ArtistX/Image1.png", name);
    }

    [Fact]
    public void DescriptionWithNoUsableCharacters_FallsBackToImage()
    {
        var name = CommissionNaming.DefaultBlobName("---", null, "x.png", "image/png", 0);

        Assert.Equal("Image1.png", name);
    }
}

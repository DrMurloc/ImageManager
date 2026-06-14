using ImageManager.Domain;

namespace ImageManager.Tests;

public class ImageAssetTests
{
    [Fact]
    public void IsDriftedFrom_SyncedWithDifferingMd5_IsTrue()
    {
        var asset = new ImageAsset { Synced = true, SyncedMd5 = "aaa" };

        Assert.True(asset.IsDriftedFrom("bbb"));
    }

    [Fact]
    public void IsDriftedFrom_SyncedWithMatchingMd5_IsFalse()
    {
        var asset = new ImageAsset { Synced = true, SyncedMd5 = "aaa" };

        Assert.False(asset.IsDriftedFrom("aaa"));
    }

    [Fact]
    public void IsDriftedFrom_NotSynced_IsFalse()
    {
        var asset = new ImageAsset { Synced = false, SyncedMd5 = "aaa" };

        Assert.False(asset.IsDriftedFrom("bbb"));
    }

    [Fact]
    public void IsDriftedFrom_UnknownSyncedMd5_IsFalse()
    {
        var asset = new ImageAsset { Synced = true, SyncedMd5 = null };

        Assert.False(asset.IsDriftedFrom("bbb"));
    }

    [Fact]
    public void IsDriftedFrom_UnknownLiveMd5_IsFalse()
    {
        var asset = new ImageAsset { Synced = true, SyncedMd5 = "aaa" };

        Assert.False(asset.IsDriftedFrom(null));
    }

    [Fact]
    public void SetChapters_ReplacesChapterIds()
    {
        var asset = new ImageAsset { Ao3ChapterIds = { "old" } };

        asset.SetChapters(new[] { "a", "b" });

        Assert.Equal(new[] { "a", "b" }, asset.Ao3ChapterIds);
    }

    [Fact]
    public void SetChapters_PrunesAddedToChapterIdsNotInNewSet()
    {
        var asset = new ImageAsset();
        asset.AddedToChapterIds.AddRange(new[] { "a", "gone" });

        asset.SetChapters(new[] { "a", "b" });

        Assert.Equal(new[] { "a" }, asset.AddedToChapterIds);
    }

    [Fact]
    public void SetChapters_PrunesChapterSectionsNotInNewSet()
    {
        var asset = new ImageAsset();
        asset.ChapterSections["a"] = GallerySection.Header;
        asset.ChapterSections["gone"] = GallerySection.Footer;

        asset.SetChapters(new[] { "a", "b" });

        Assert.True(asset.ChapterSections.ContainsKey("a"));
        Assert.False(asset.ChapterSections.ContainsKey("gone"));
    }

    [Fact]
    public void SetChapters_EmptyList_ClearsDependentState()
    {
        var asset = new ImageAsset { Ao3ChapterIds = { "a" } };
        asset.AddedToChapterIds.Add("a");
        asset.ChapterSections["a"] = GallerySection.Midsection;

        asset.SetChapters(Array.Empty<string>());

        Assert.Empty(asset.Ao3ChapterIds);
        Assert.Empty(asset.AddedToChapterIds);
        Assert.Empty(asset.ChapterSections);
    }

    [Fact]
    public void SetChapters_DoesNotFabricatePerChapterStateForNewIds()
    {
        var asset = new ImageAsset();

        asset.SetChapters(new[] { "a", "b" });

        Assert.Empty(asset.AddedToChapterIds);
        Assert.Empty(asset.ChapterSections);
    }
}

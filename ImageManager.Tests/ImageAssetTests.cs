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
}

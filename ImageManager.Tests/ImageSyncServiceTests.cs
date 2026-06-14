using ImageManager.Application;
using ImageManager.Domain;

namespace ImageManager.Tests;

public class ImageSyncServiceTests
{
    private const string FolderId = "group-folder";

    private static (ImageSyncService Sut, FakeDriveScanner Drive, FakeBlobSyncService Blob, InMemoryMetadataStore Store) Build()
    {
        var store = new InMemoryMetadataStore();
        store.Db.Groups.Add(new CommissionGroup { DriveFolderId = FolderId });
        var drive = new FakeDriveScanner();
        var blob = new FakeBlobSyncService();
        return (new ImageSyncService(drive, blob, store), drive, blob, store);
    }

    private static SyncItem Item(string driveFileId = "file-1", string blobName = "Artist/Pic1.png")
        => new(driveFileId, "Pic.png", blobName, "alt text", 800, 600, "image/png", "md5-1");

    [Fact]
    public async Task DownloadsFromDriveAndUploadsToBlob()
    {
        var (sut, drive, blob, _) = Build();

        await sut.SyncAsync(FolderId, new[] { Item() });

        Assert.Equal(new[] { "file-1" }, drive.Downloaded);
        var upload = Assert.Single(blob.Uploads);
        Assert.Equal("Artist/Pic1.png", upload.BlobName);
        Assert.Equal("image/png", upload.ContentType);
        Assert.Equal(3, upload.Bytes);
    }

    [Fact]
    public async Task CreatesAndStampsAssetOnTheGroup()
    {
        var (sut, _, _, store) = Build();

        await sut.SyncAsync(FolderId, new[] { Item() });

        var asset = Assert.Single(store.Db.Groups[0].Images);
        Assert.Equal("file-1", asset.DriveFileId);
        Assert.Equal("Pic.png", asset.SourceFileName);
        Assert.Equal("Artist/Pic1.png", asset.BlobName);
        Assert.Equal(800, asset.Width);
        Assert.Equal(600, asset.Height);
        Assert.Equal("alt text", asset.AltText);
        Assert.True(asset.Synced);
        Assert.Equal("md5-1", asset.SyncedMd5);
        Assert.NotNull(asset.SyncedAt);
    }

    [Fact]
    public async Task UpdatesExistingAssetInsteadOfDuplicating()
    {
        var (sut, _, _, store) = Build();
        store.Db.Groups[0].Images.Add(new ImageAsset { DriveFileId = "file-1", BlobName = "old", Synced = false });

        await sut.SyncAsync(FolderId, new[] { Item(blobName: "Artist/Pic1.png") });

        var asset = Assert.Single(store.Db.Groups[0].Images);
        Assert.Equal("Artist/Pic1.png", asset.BlobName);
        Assert.True(asset.Synced);
    }

    [Fact]
    public async Task SyncsBatchAndSavesOnce()
    {
        var (sut, _, _, store) = Build();

        var results = await sut.SyncAsync(FolderId, new[]
        {
            Item("file-1", "Artist/Pic1.png"),
            Item("file-2", "Artist/Pic2.png"),
        });

        Assert.Equal(2, store.Db.Groups[0].Images.Count);
        Assert.Equal(2, results.Count);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task ReturnsResultsWithPublicUrls()
    {
        var (sut, _, _, _) = Build();

        var results = await sut.SyncAsync(FolderId, new[] { Item(blobName: "Artist/Pic1.png") });

        Assert.Equal("https://cdn/Artist/Pic1.png", Assert.Single(results).PublicUrl);
    }

    [Fact]
    public async Task ThrowsWhenGroupNotFound()
    {
        var (sut, _, _, _) = Build();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SyncAsync("missing", new[] { Item() }));
    }
}

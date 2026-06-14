using ImageManager.Application;
using ImageManager.Domain;

namespace ImageManager.Tests;

public class CommissionGroupServiceTests
{
    private static CreateGroupRequest Request(string[] files, string artistName = "Artist X", string? url = null)
        => new("token", "coll-1", "Astrid and Cale", "BeachDay", artistName, url,
               files.Select(f => new UploadFile(f, "image/png", new byte[] { 1 })).ToList());

    [Fact]
    public async Task CreatesFolderNamedFromDescriptionAndArtistUnderCollection()
    {
        var drive = new FakeDriveUploader();
        var sut = new CommissionGroupService(drive, new InMemoryMetadataStore());

        await sut.CreateGroupAsync(Request(new[] { "a.png" }));

        Assert.Equal("coll-1", drive.CreatedUnderParentId);
        Assert.Equal("BeachDay_ArtistX", drive.CreatedFolderName);
    }

    [Fact]
    public async Task UploadsAllFilesAndReportsCount()
    {
        var drive = new FakeDriveUploader();
        var sut = new CommissionGroupService(drive, new InMemoryMetadataStore());

        var result = await sut.CreateGroupAsync(Request(new[] { "a.png", "b.png" }));

        Assert.Equal(2, result.Uploaded);
        Assert.Equal(new[] { "a.png", "b.png" }, drive.UploadedFileNames);
    }

    [Fact]
    public async Task PersistsGroupWithCollectionMetadataAndLinkedArtist()
    {
        var drive = new FakeDriveUploader { FolderId = "new-folder" };
        var store = new InMemoryMetadataStore();
        var sut = new CommissionGroupService(drive, store);

        var result = await sut.CreateGroupAsync(Request(new[] { "a.png" }));

        var group = Assert.Single(store.Db.Groups);
        Assert.Equal("new-folder", group.DriveFolderId);
        Assert.Equal("Astrid and Cale", group.CharacterNames);
        Assert.Equal("BeachDay_ArtistX", group.GroupName);
        var artist = Assert.Single(store.Db.Artists);
        Assert.Equal(artist.Id, group.ArtistId);
        Assert.Equal("new-folder", result.FolderId);
        Assert.Equal("BeachDay_ArtistX", result.Subfolder);
    }

    [Fact]
    public async Task ReusesExistingArtistByNameCaseInsensitively()
    {
        var store = new InMemoryMetadataStore();
        store.Db.Artists.Add(new Artist { Name = "Artist X", CreditUrl = "https://existing" });
        var sut = new CommissionGroupService(new FakeDriveUploader(), store);

        await sut.CreateGroupAsync(Request(new[] { "a.png" }, artistName: "artist x", url: "https://ignored"));

        var artist = Assert.Single(store.Db.Artists);
        Assert.Equal("https://existing", artist.CreditUrl);
    }

    [Fact]
    public async Task CreatesNewArtistWithCreditUrlWhenNotFound()
    {
        var store = new InMemoryMetadataStore();
        var sut = new CommissionGroupService(new FakeDriveUploader(), store);

        await sut.CreateGroupAsync(Request(new[] { "a.png" }, artistName: "New Artist", url: "https://ko-fi.com/new"));

        var artist = Assert.Single(store.Db.Artists);
        Assert.Equal("New Artist", artist.Name);
        Assert.Equal("https://ko-fi.com/new", artist.CreditUrl);
    }

    [Fact]
    public async Task PersistsExactlyOnce()
    {
        var store = new InMemoryMetadataStore();
        var sut = new CommissionGroupService(new FakeDriveUploader(), store);

        await sut.CreateGroupAsync(Request(new[] { "a.png" }));

        Assert.Equal(1, store.SaveCount);
    }
}

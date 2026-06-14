using ImageManager.Application;

namespace ImageManager.Tests;

public class DocumentMigrationServiceTests
{
    private static MigrateDocument Doc(string book, int number, string name)
        => new(book, number, name, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", new byte[] { 1 });

    private static MigrateRequest Request(params MigrateDocument[] docs)
        => new("token", "books-root", docs);

    [Fact]
    public async Task EnsuresBookFolderUnderBooksRootAndUploadsNamedFile()
    {
        var drive = new FakeDriveUploader();
        var sut = new DocumentMigrationService(drive);

        var result = await sut.MigrateAsync(Request(Doc("Connected", 1, "Armor")));

        Assert.Equal("books-root", drive.EnsuredUnderParentId);
        Assert.Equal(new[] { "Connected" }, drive.EnsuredFolderNames);
        Assert.Equal(new[] { "Chapter 1 - Armor.docx" }, drive.UploadedFileNames);
        Assert.Equal(1, result.Uploaded);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public async Task SkipsFilesThatAlreadyExist()
    {
        var drive = new FakeDriveUploader();
        drive.ExistingFiles.Add("Chapter 1 - Armor.docx");
        var sut = new DocumentMigrationService(drive);

        var result = await sut.MigrateAsync(Request(
            Doc("Connected", 1, "Armor"),
            Doc("Connected", 2, "Shield")));

        Assert.Equal(new[] { "Chapter 2 - Shield.docx" }, drive.UploadedFileNames);
        Assert.Equal(1, result.Uploaded);
        Assert.Equal(new[] { "Chapter 1 - Armor.docx" }, result.Skipped);
    }

    [Fact]
    public async Task EnsuresEachBookFolderOnlyOnce()
    {
        var drive = new FakeDriveUploader();
        var sut = new DocumentMigrationService(drive);

        await sut.MigrateAsync(Request(
            Doc("Connected", 0, "Prologue"),
            Doc("Connected", 1, "Armor"),
            Doc("Sequel", 0, "Prologue")));

        Assert.Equal(new[] { "Connected", "Sequel" }, drive.EnsuredFolderNames);
        Assert.Equal(3, drive.UploadedFileNames.Count);
    }
}

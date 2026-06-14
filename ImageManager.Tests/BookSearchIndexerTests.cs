using ImageManager.Application;

namespace ImageManager.Tests;

public class BookSearchIndexerTests
{
    private const string Root = "books-root";

    private static (BookSearchIndexer Sut, FakeDriveUploader Drive, FakeDocxTextExtractor Extractor, FakeSearchIndex Index) Build()
    {
        var drive = new FakeDriveUploader();
        var extractor = new FakeDocxTextExtractor();
        var index = new FakeSearchIndex();
        return (new BookSearchIndexer(drive, extractor, index), drive, extractor, index);
    }

    [Fact]
    public async Task ListChapters_ParsesConventionFilesAndSkipsOthers()
    {
        var (sut, drive, _, _) = Build();
        drive.FoldersByParent[Root] = new() { new("book-connected", "Connected") };
        drive.FilesByFolder["book-connected"] = new()
        {
            new("f0", "Chapter 0 - Prologue.docx"),
            new("f1", "Chapter 1 - Armor.docx"),
            new("fx", "notes.txt"),
        };

        var chapters = await sut.ListChaptersAsync("token", Root);

        Assert.Equal(2, chapters.Count);
        Assert.Contains(chapters, c => c is { Book: "Connected", Number: 0, Name: "Prologue", DriveFileId: "f0" });
        Assert.Contains(chapters, c => c is { Book: "Connected", Number: 1, Name: "Armor", DriveFileId: "f1" });
    }

    [Fact]
    public async Task IndexChapter_DownloadsExtractsChunksAndIndexes()
    {
        var (sut, drive, extractor, index) = Build();
        drive.ContentByFileId["f1"] = new byte[] { 1, 2, 3 };
        extractor.Text = "Para one.\n\nPara two.";

        await sut.IndexChapterAsync("token", new BookChapterRef("Connected", 1, "Armor", "f1"));

        var indexed = Assert.Single(index.Indexed);
        Assert.Equal("Connected", indexed.Book);
        Assert.Equal(1, indexed.Number);
        Assert.Equal("Armor", indexed.Name);
        Assert.Equal(new[] { "Para one.\n\nPara two." }, indexed.Chunks);
    }

    [Fact]
    public async Task IndexAll_IndexesEveryChapterAndReturnsCount()
    {
        var (sut, drive, _, index) = Build();
        drive.FoldersByParent[Root] = new() { new("book-connected", "Connected") };
        drive.FilesByFolder["book-connected"] = new()
        {
            new("f0", "Chapter 0 - Prologue.docx"),
            new("f1", "Chapter 1 - Armor.docx"),
        };
        drive.ContentByFileId["f0"] = new byte[] { 1 };
        drive.ContentByFileId["f1"] = new byte[] { 1 };

        var count = await sut.IndexAllAsync("token", Root);

        Assert.Equal(2, count);
        Assert.Equal(2, index.Indexed.Count);
    }

    [Fact]
    public async Task EnsureIndex_DelegatesToTheIndex()
    {
        var (sut, _, _, index) = Build();

        await sut.EnsureIndexAsync();

        Assert.Equal(1, index.EnsureCalls);
    }
}

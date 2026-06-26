using ImageManager.Application;
using ImageManager.Domain;

namespace ImageManager.Tests;

public class NoteServiceTests
{
    private static NoteService Build(out FakeNoteStore store, out FakeNoteSearchIndex index, out FakeNoteSearchQuery search)
    {
        store = new FakeNoteStore();
        index = new FakeNoteSearchIndex();
        search = new FakeNoteSearchQuery();
        return new NoteService(store, index, search);
    }

    [Fact]
    public async Task SaveAsync_WritesToStoreAndIndexes()
    {
        var svc = Build(out var store, out var index, out _);

        await svc.SaveAsync("books/connected/index", "# Chapters");

        Assert.Equal("# Chapters", store.Files["books/connected/index.md"]);
        Assert.Equal("# Chapters", index.Indexed["books/connected/index.md"]);
    }

    [Fact]
    public async Task SaveThenRead_NormalizesPathConsistently()
    {
        var svc = Build(out _, out _, out _);

        await svc.SaveAsync("books\\connected\\index.md", "x");
        var note = await svc.ReadAsync("/books/connected/index");

        Assert.NotNull(note);
        Assert.Equal("books/connected/index.md", note!.Path);
        Assert.Equal("x", note.Content);
    }

    [Fact]
    public async Task ReadAsync_ReturnsNullWhenMissing()
    {
        var svc = Build(out _, out _, out _);
        Assert.Null(await svc.ReadAsync("nope.md"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesFromStoreAndIndex()
    {
        var svc = Build(out var store, out var index, out _);
        await svc.SaveAsync("a.md", "x");

        var deleted = await svc.DeleteAsync("a.md");

        Assert.True(deleted);
        Assert.False(store.Files.ContainsKey("a.md"));
        Assert.Contains("a.md", index.Deleted);
    }

    [Fact]
    public async Task DeleteAsync_WhenAbsent_DoesNotTouchIndex()
    {
        var svc = Build(out _, out var index, out _);

        var deleted = await svc.DeleteAsync("ghost.md");

        Assert.False(deleted);
        Assert.Empty(index.Deleted);
    }

    [Fact]
    public async Task SearchAsync_DefaultsTopWhenNonPositive()
    {
        var svc = Build(out _, out _, out var search);

        await svc.SearchAsync("astrid", 0);

        Assert.Equal(8, search.LastTop);
    }

    [Fact]
    public async Task ReindexAll_EnsuresIndexAndIndexesEveryNote()
    {
        var svc = Build(out var store, out var index, out _);
        await store.WriteAsync(NotePath.Parse("a.md"), "one");
        await store.WriteAsync(NotePath.Parse("books/b.md"), "two");

        var count = await svc.ReindexAllAsync();

        Assert.Equal(2, count);
        Assert.Equal(1, index.EnsureCalls);
        Assert.Equal("one", index.Indexed["a.md"]);
        Assert.Equal("two", index.Indexed["books/b.md"]);
    }

    [Fact]
    public async Task ListAsync_ReturnsImmediateChildren()
    {
        var svc = Build(out var store, out _, out _);
        await store.WriteAsync(NotePath.Parse("books/connected/index.md"), "x");
        await store.WriteAsync(NotePath.Parse("books/connected/character/astrid.md"), "y");
        await store.WriteAsync(NotePath.Parse("scratch.md"), "z");

        var root = await svc.ListAsync("");
        Assert.Contains("books", root.Folders);
        Assert.Contains(root.Notes, n => n.Path == "scratch.md");

        var connected = await svc.ListAsync("books/connected");
        Assert.Contains("books/connected/character", connected.Folders);
        Assert.Contains(connected.Notes, n => n.Path == "books/connected/index.md");
    }
}

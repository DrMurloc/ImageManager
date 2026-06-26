using ImageManager.Application;
using ImageManager.Domain;

namespace ImageManager.Tests;

public class TodoServiceTests
{
    private static TodoService Build(out FakeTodoRepository repo)
    {
        repo = new FakeTodoRepository();
        return new TodoService(repo);
    }

    [Fact]
    public async Task AddAsync_StoresAndReturns()
    {
        var svc = Build(out var repo);

        var todo = await svc.AddAsync("Connected", "Outline", null, "note");

        Assert.Single(repo.Items);
        Assert.Equal("Connected", todo.Book);
        Assert.Equal(TodoScope.Book, todo.Scope);
        Assert.Equal("note", todo.Notes);
    }

    [Fact]
    public async Task AddAsync_AssignsIncrementingOrderPerList()
    {
        var svc = Build(out _);

        var a = await svc.AddAsync("Connected", "one");
        var b = await svc.AddAsync("Connected", "two");
        var chapter = await svc.AddAsync("Connected", "chap", 1);

        Assert.Equal(0, a.Order);
        Assert.Equal(1, b.Order);
        Assert.Equal(0, chapter.Order); // chapter 1 is a separate list, starts at 0
    }

    [Fact]
    public async Task SetDone_TogglesAndStamps()
    {
        var svc = Build(out _);
        var t = await svc.AddAsync("Connected", "x");

        var done = await svc.SetDoneAsync(t.Id, true);
        Assert.True(done!.Done);
        Assert.NotNull(done.CompletedUtc);

        var reopened = await svc.SetDoneAsync(t.Id, false);
        Assert.False(reopened!.Done);
        Assert.Null(reopened.CompletedUtc);
    }

    [Fact]
    public async Task SetDone_UnknownId_ReturnsNull()
        => Assert.Null(await Build(out _).SetDoneAsync(Guid.NewGuid(), true));

    [Fact]
    public async Task Update_ChangesProvidedFieldsOnly()
    {
        var svc = Build(out _);
        var t = await svc.AddAsync("Connected", "old", null, "oldnote");

        var updated = await svc.UpdateAsync(t.Id, title: "new", notes: null, order: 9);

        Assert.Equal("new", updated!.Title);
        Assert.Equal("oldnote", updated.Notes); // null notes => unchanged
        Assert.Equal(9, updated.Order);
    }

    [Fact]
    public async Task Update_CanClearNotesWithEmptyString()
    {
        var svc = Build(out _);
        var t = await svc.AddAsync("Connected", "x", null, "hasnote");

        var updated = await svc.UpdateAsync(t.Id, notes: "");

        Assert.Null(updated!.Notes);
    }

    [Fact]
    public async Task List_FiltersByScopeAndExcludesDoneByDefault()
    {
        var svc = Build(out _);
        var book = await svc.AddAsync("Connected", "book-level");
        await svc.AddAsync("Connected", "chapter-level", 2);
        await svc.SetDoneAsync(book.Id, true);

        var openBook = await svc.ListAsync(new TodoFilter(Scope: TodoScope.Book));
        Assert.Empty(openBook); // the only book-level todo is done

        var allBook = await svc.ListAsync(new TodoFilter(Scope: TodoScope.Book, IncludeDone: true));
        Assert.Single(allBook);

        var chapters = await svc.ListAsync(new TodoFilter(Scope: TodoScope.Chapter));
        Assert.Single(chapters);
        Assert.Equal("chapter-level", chapters[0].Title);
    }

    [Fact]
    public async Task Delete_RemovesAndReportsExistence()
    {
        var svc = Build(out var repo);
        var t = await svc.AddAsync("Connected", "x");

        Assert.True(await svc.DeleteAsync(t.Id));
        Assert.Empty(repo.Items);
        Assert.False(await svc.DeleteAsync(t.Id));
    }
}

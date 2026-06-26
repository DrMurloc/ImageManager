using ImageManager.Domain;

namespace ImageManager.Tests;

public class TodoTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_TrimsAndInitializes()
    {
        var todo = Todo.Create("  Connected  ", "  Outline act two  ", null, "  later  ", 3, Now);

        Assert.NotEqual(Guid.Empty, todo.Id);
        Assert.Equal("Connected", todo.Book);
        Assert.Equal("Outline act two", todo.Title);
        Assert.Equal("later", todo.Notes);
        Assert.Equal(3, todo.Order);
        Assert.False(todo.Done);
        Assert.Null(todo.CompletedUtc);
        Assert.Equal(Now, todo.CreatedUtc);
    }

    [Fact]
    public void Create_BlankNotesBecomeNull()
        => Assert.Null(Todo.Create("Connected", "x", null, "   ", 0, Now).Notes);

    [Fact]
    public void Scope_IsBookWhenChapterNull_ChapterOtherwise()
    {
        Assert.Equal(TodoScope.Book, Todo.Create("Connected", "x", null, null, 0, Now).Scope);
        Assert.Equal(TodoScope.Chapter, Todo.Create("Connected", "x", 5, null, 0, Now).Scope);
    }

    [Theory]
    [InlineData("", "title")]
    [InlineData("   ", "title")]
    [InlineData("Connected", "")]
    [InlineData("Connected", "   ")]
    public void Create_RequiresBookAndTitle(string book, string title)
        => Assert.Throws<ArgumentException>(() => Todo.Create(book, title, null, null, 0, Now));

    [Fact]
    public void Create_RejectsNegativeChapter()
        => Assert.Throws<ArgumentException>(() => Todo.Create("Connected", "x", -1, null, 0, Now));

    [Fact]
    public void Complete_SetsDoneAndTimestamp_ReopenClears()
    {
        var todo = Todo.Create("Connected", "x", 1, null, 0, Now);

        todo.Complete(Now.AddHours(1));
        Assert.True(todo.Done);
        Assert.Equal(Now.AddHours(1), todo.CompletedUtc);

        todo.Reopen();
        Assert.False(todo.Done);
        Assert.Null(todo.CompletedUtc);
    }
}

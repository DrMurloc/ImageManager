using System.ComponentModel;
using ImageManager.Application;
using ImageManager.Domain;
using ModelContextProtocol.Server;

namespace ImageManager.Mcp;

[McpServerToolType]
public static class TodoTools
{
    [McpServerTool, Description("List todos. Scope 'book' = high-level book todos; 'chapter' = todos for a specific chapter; 'all' = both. Optionally filter by book title and chapter number. Completed todos are hidden unless includeDone is true.")]
    public static async Task<string> ListTodos(
        ITodoService todos,
        [Description("'book', 'chapter', or 'all' (default)")] string scope = "all",
        [Description("Optional book title to filter by, e.g. 'Connected'")] string? book = null,
        [Description("Optional chapter number to filter by")] int? chapterNumber = null,
        [Description("Include completed todos (default false)")] bool includeDone = false)
    {
        TodoScope? scopeFilter = scope?.Trim().ToLowerInvariant() switch
        {
            "book" => TodoScope.Book,
            "chapter" => TodoScope.Chapter,
            _ => null
        };

        var items = await todos.ListAsync(new TodoFilter(book, chapterNumber, scopeFilter, includeDone));
        return items.Count == 0
            ? "No todos found."
            : string.Join("\n", items.Select(Format));
    }

    [McpServerTool, Description("Add a todo. Omit chapterNumber for a book-level todo; include it for a chapter-level todo. Returns the new todo with its id.")]
    public static async Task<string> AddTodo(
        ITodoService todos,
        [Description("Book title, e.g. 'Connected'")] string book,
        [Description("What needs doing")] string title,
        [Description("Chapter number for a chapter-level todo; omit for book-level")] int? chapterNumber = null,
        [Description("Optional extra detail")] string? notes = null)
    {
        var todo = await todos.AddAsync(book, title, chapterNumber, notes);
        return "Added:\n" + Format(todo);
    }

    [McpServerTool, Description("Mark a todo done, or reopen it. Pass the todo id from list_todos.")]
    public static async Task<string> CompleteTodo(
        ITodoService todos,
        [Description("The todo id")] string id,
        [Description("true to complete, false to reopen (default true)")] bool done = true)
    {
        if (!Guid.TryParse(id, out var guid))
            return $"'{id}' is not a valid todo id.";

        var todo = await todos.SetDoneAsync(guid, done);
        if (todo is null)
            return $"No todo with id {id}.";
        return (done ? "Completed:\n" : "Reopened:\n") + Format(todo);
    }

    [McpServerTool, Description("Update a todo's title, notes, or order. Only the fields you pass change; pass an empty string for notes to clear them.")]
    public static async Task<string> UpdateTodo(
        ITodoService todos,
        [Description("The todo id")] string id,
        [Description("New title (optional)")] string? title = null,
        [Description("New notes; empty string clears them (optional)")] string? notes = null,
        [Description("New sort order (optional)")] int? order = null)
    {
        if (!Guid.TryParse(id, out var guid))
            return $"'{id}' is not a valid todo id.";

        var todo = await todos.UpdateAsync(guid, title, notes, order);
        return todo is null ? $"No todo with id {id}." : "Updated:\n" + Format(todo);
    }

    [McpServerTool, Description("Delete a todo by id. This cannot be undone.")]
    public static async Task<string> DeleteTodo(
        ITodoService todos,
        [Description("The todo id")] string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return $"'{id}' is not a valid todo id.";

        var deleted = await todos.DeleteAsync(guid);
        return deleted ? $"Deleted todo {id}." : $"No todo with id {id}.";
    }

    private static string Format(Todo t)
    {
        var scope = t.ChapterNumber is null ? "book" : $"ch.{t.ChapterNumber}";
        var status = t.Done ? "[x]" : "[ ]";
        var notes = string.IsNullOrWhiteSpace(t.Notes) ? "" : $" — {t.Notes}";
        return $"{status} {t.Book} ({scope}) {t.Title}{notes}  #{t.Id}";
    }
}

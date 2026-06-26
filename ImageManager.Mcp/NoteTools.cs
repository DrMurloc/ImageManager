using System.ComponentModel;
using ImageManager.Application;
using ModelContextProtocol.Server;

namespace ImageManager.Mcp;

[McpServerToolType]
public static class NoteTools
{
    [McpServerTool, Description("Search your notes (markdown files) for a query and return the most relevant ones with their path and content. Notes are your own knowledge base — character journeys, chapter indexes, worldbuilding, and so on.")]
    public static async Task<string> SearchNotes(
        INoteService notes,
        [Description("What to search for")] string query,
        [Description("Maximum number of notes to return (default 8)")] int top = 8)
    {
        var hits = await notes.SearchAsync(query, top);
        if (hits.Count == 0)
            return "No matching notes found.";

        return string.Join("\n\n---\n\n", hits.Select(h => $"[{h.Path}]\n{h.Content}"));
    }

    [McpServerTool, Description("Read the full content of one note by its exact path, e.g. 'books/connected/character/astrid.md'.")]
    public static async Task<string> ReadNote(
        INoteService notes,
        [Description("Exact note path, e.g. 'books/connected/index.md'")] string path)
    {
        var note = await notes.ReadAsync(path);
        return note is null ? $"No note found at '{path}'." : note.Content;
    }

    [McpServerTool, Description("Create or overwrite a note at the given path with markdown content. Folders are created implicitly from the path. Use this to capture or update notes from anywhere.")]
    public static async Task<string> WriteNote(
        INoteService notes,
        [Description("Note path, e.g. 'books/connected/character/astrid.md'. A '.md' extension is added if you omit one.")] string path,
        [Description("The full markdown content of the note")] string content)
    {
        await notes.SaveAsync(path, content);
        return $"Saved note '{path}'.";
    }

    [McpServerTool, Description("List the notes and sub-folders directly under a folder path. Pass an empty string for the top level. Use this to browse the note tree.")]
    public static async Task<string> ListNotes(
        INoteService notes,
        [Description("Folder path to list, e.g. 'books/connected'. Empty for the root.")] string prefix = "")
    {
        var listing = await notes.ListAsync(prefix);
        if (listing.Folders.Count == 0 && listing.Notes.Count == 0)
            return $"Nothing found under '{(string.IsNullOrEmpty(prefix) ? "(root)" : prefix)}'.";

        var lines = new List<string>();
        foreach (var folder in listing.Folders)
            lines.Add($"[folder] {folder}/");
        foreach (var note in listing.Notes)
            lines.Add($"[note]   {note.Path}");
        return string.Join("\n", lines);
    }

    [McpServerTool, Description("Delete a note by its exact path. This cannot be undone from here.")]
    public static async Task<string> DeleteNote(
        INoteService notes,
        [Description("Exact note path to delete")] string path)
    {
        var deleted = await notes.DeleteAsync(path);
        return deleted ? $"Deleted note '{path}'." : $"No note found at '{path}'.";
    }
}

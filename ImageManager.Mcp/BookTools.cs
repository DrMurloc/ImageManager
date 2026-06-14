using System.ComponentModel;
using ImageManager.Application;
using ModelContextProtocol.Server;

namespace ImageManager.Mcp;

[McpServerToolType]
public static class BookTools
{
    [McpServerTool, Description("Search the indexed book(s) for passages matching a query. Returns the most relevant excerpts with their book and chapter so you can answer questions or quote text. Optionally restrict to a single book by exact title.")]
    public static async Task<string> SearchBook(
        ISearchQuery search,
        [Description("What to search for, e.g. a character, event, place, or phrase")] string query,
        [Description("Optional exact book title to restrict the search to")] string? book = null,
        [Description("Maximum number of passages to return (default 8)")] int top = 8)
    {
        var hits = await search.SearchAsync(query, book, top);
        if (hits.Count == 0)
            return "No matching passages found.";

        return string.Join("\n\n---\n\n", hits.Select(h =>
            $"[{h.Book} — Chapter {h.ChapterNumber}: {h.ChapterName}]\n{h.Content}"));
    }

    [McpServerTool, Description("Return the full text of one chapter, by exact book title and chapter number. The prologue is chapter 0.")]
    public static async Task<string> GetChapter(
        ISearchQuery search,
        [Description("Exact book title, e.g. 'Connected'")] string book,
        [Description("Chapter number; the prologue is 0")] int chapterNumber)
    {
        var chapter = await search.GetChapterAsync(book, chapterNumber);
        return chapter is null
            ? $"No indexed chapter {chapterNumber} found for book '{book}'."
            : $"# {chapter.Book} — Chapter {chapter.ChapterNumber}: {chapter.ChapterName}\n\n{chapter.Content}";
    }
}

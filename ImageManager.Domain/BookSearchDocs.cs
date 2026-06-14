using System.Text.RegularExpressions;

namespace ImageManager.Domain;

// Builds stable, Azure-Search-safe document keys for chapter chunks.
// Keys may only contain letters, digits, dash, underscore, and equals.
public static class BookSearchDocs
{
    public static string ChunkKey(string book, int chapterNumber, int chunkIndex)
        => $"{Slug(book)}_{chapterNumber}_{chunkIndex}";

    private static string Slug(string value)
    {
        var slug = Regex.Replace(value, "[^A-Za-z0-9]", "-").Trim('-');
        return slug.Length == 0 ? "book" : slug;
    }
}

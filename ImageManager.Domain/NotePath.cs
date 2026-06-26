using System.Text;

namespace ImageManager.Domain;

// A validated, normalized path to a note in the notes store. Paths are relative,
// forward-slash separated, carry a file extension (defaulting to .md), and never
// contain "." or ".." segments — so they're safe to use as blob names directly.
public sealed record NotePath
{
    public string Value { get; }      // e.g. "books/connected/character/astrid.md"
    public string Folder { get; }     // e.g. "books/connected/character" ("" at the root)
    public string FileName { get; }   // e.g. "astrid.md"
    public string Title { get; }      // FileName without its extension, e.g. "astrid"
    public string Key { get; }        // stable, Azure-Search-safe document key for this path

    private NotePath(string value, string folder, string fileName, string title, string key)
    {
        Value = value;
        Folder = folder;
        FileName = fileName;
        Title = title;
        Key = key;
    }

    public static NotePath Parse(string? raw)
        => TryParse(raw, out var path, out var error) ? path! : throw new ArgumentException(error, nameof(raw));

    public static bool TryParse(string? raw, out NotePath? path, out string error)
    {
        path = null;
        error = "";

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Note path is required.";
            return false;
        }

        // Accept either slash, then split into clean segments (drops leading/trailing/duplicate slashes).
        var segments = raw.Trim()
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
        {
            error = "Note path is required.";
            return false;
        }

        if (segments.Any(s => s is "." or ".."))
        {
            error = "Note path may not contain '.' or '..' segments.";
            return false;
        }

        // Notes are markdown by default; only add an extension when the file has none.
        if (!segments[^1].Contains('.'))
            segments[^1] += ".md";

        var value = string.Join('/', segments);
        var fileName = segments[^1];
        var folder = segments.Length > 1 ? string.Join('/', segments[..^1]) : "";
        var dot = fileName.LastIndexOf('.');
        var title = dot > 0 ? fileName[..dot] : fileName;

        path = new NotePath(value, folder, fileName, title, ToKey(value));
        return true;
    }

    // URL-safe Base64 of the path: stable, collision-free, and within Azure Search's
    // allowed key alphabet (letters, digits, dash, underscore, equals).
    private static string ToKey(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).Replace('+', '-').Replace('/', '_');
}

using System.Text;
using System.Text.RegularExpressions;

namespace ImageManager.Domain;

// Splits chapter text into paragraph-aligned passages for indexing. Each chunk stays at or
// under maxChars; a paragraph longer than maxChars is hard-wrapped.
public static class ChapterChunker
{
    public static IReadOnlyList<string> Chunk(string text, int maxChars = 1800)
    {
        var chunks = new List<string>();

        var normalized = Regex.Replace(text.Replace("\r\n", "\n"), "\n{2,}", "\n\n").Trim();
        if (normalized.Length == 0) return chunks;

        var buffer = new StringBuilder();
        void Flush()
        {
            if (buffer.Length == 0) return;
            chunks.Add(buffer.ToString().Trim());
            buffer.Clear();
        }

        foreach (var paragraph in normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var para = paragraph.Trim();
            if (para.Length == 0) continue;

            if (para.Length > maxChars)
            {
                Flush();
                for (var i = 0; i < para.Length; i += maxChars)
                    chunks.Add(para.Substring(i, Math.Min(maxChars, para.Length - i)));
                continue;
            }

            if (buffer.Length > 0 && buffer.Length + 2 + para.Length > maxChars)
                Flush();

            if (buffer.Length > 0) buffer.Append("\n\n");
            buffer.Append(para);
        }

        Flush();
        return chunks;
    }
}

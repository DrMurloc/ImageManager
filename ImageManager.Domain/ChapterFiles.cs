using System.Text.RegularExpressions;

namespace ImageManager.Domain;

// A Grammarly export filename split into its book and chapter-name parts.
public sealed record ChapterFileName(string Book, string ChapterName);

public static class ChapterFiles
{
    // Parses "<Book> - <Chapter name>" from a downloaded filename, stripping the ".docx"
    // extension plus Grammarly's ".edited" and re-download " (N)" suffixes (in any order).
    // Book is empty when the " - " separator is absent.
    public static ChapterFileName Parse(string fileName)
    {
        var name = fileName;
        if (name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            name = name[..^".docx".Length];
        name = StripNoise(name);

        var sep = name.IndexOf(" - ", StringComparison.Ordinal);
        return sep < 0
            ? new ChapterFileName("", name.Trim())
            : new ChapterFileName(name[..sep].Trim(), name[(sep + 3)..].Trim());
    }

    // The Drive filename a chapter migrates to: "Chapter <number> - <name>.docx".
    public static string TargetFileName(int chapterNumber, string chapterName)
        => $"Chapter {chapterNumber} - {chapterName}.docx";

    // Parses a migrated "Chapter <number> - <name>.docx" filename back into its parts;
    // returns null for files that don't follow the convention.
    public static (int Number, string Name)? ParseTarget(string fileName)
    {
        var match = Regex.Match(fileName, @"^Chapter (\d+) - (.+)\.docx$", RegexOptions.IgnoreCase);
        return match.Success
            ? (int.Parse(match.Groups[1].Value), match.Groups[2].Value)
            : null;
    }

    private static string StripNoise(string s)
    {
        string previous;
        do
        {
            previous = s;
            s = s.TrimEnd();
            var trailingCopy = Regex.Match(s, @"\s*\(\d+\)$");
            if (trailingCopy.Success)
                s = s[..trailingCopy.Index];
            if (s.EndsWith(".edited", StringComparison.OrdinalIgnoreCase))
                s = s[..^".edited".Length];
        }
        while (s != previous);

        return s;
    }
}

using System.Text.RegularExpressions;

namespace ImageManager.Domain;

// Maps AO3 chapter titles (imported as "N. Title") to manuscript chapter numbers.
// AO3 counts the prologue as chapter 1, so the manuscript number is the AO3 number minus one.
public static class Ao3Numbering
{
    // Splits "2. Armor" into (2, "Armor"); returns null when there's no leading "N." prefix.
    public static (int Number, string Name)? ParseTitle(string title)
    {
        var match = Regex.Match(title.Trim(), @"^(\d+)\.\s*(.*)$");
        return match.Success
            ? (int.Parse(match.Groups[1].Value), match.Groups[2].Value.Trim())
            : null;
    }

    // The manuscript chapter number for a chapter name, or null if it can't be determined.
    // A chapter named "Prologue" is always 0; otherwise an AO3 title match gives (AO3 number - 1).
    public static int? Resolve(IEnumerable<string> ao3Titles, string chapterName)
    {
        var target = chapterName.Trim();

        foreach (var title in ao3Titles)
        {
            if (ParseTitle(title) is { } parsed
                && string.Equals(parsed.Name, target, StringComparison.OrdinalIgnoreCase))
                return parsed.Number - 1;
        }

        return string.Equals(target, "Prologue", StringComparison.OrdinalIgnoreCase) ? 0 : null;
    }
}

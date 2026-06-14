using System.Text.RegularExpressions;

namespace ImageManager.Application;

public sealed record ParsedArtist(string Name, string CreditUrl);

// Parses the free-text "Sources" Google Doc into artist name / credit-URL pairs.
// Each line is "Name - <text that may contain a URL>"; the first http(s) token in the
// remainder becomes the credit URL. Parsing stops at an "END ARTISTS" marker line.
public static class SourcesParser
{
    public static IReadOnlyList<ParsedArtist> Parse(string text)
    {
        var result = new List<ParsedArtist>();
        if (string.IsNullOrEmpty(text)) return result;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (Regex.Replace(line, @"\s+", "").Equals("ENDARTISTS", StringComparison.OrdinalIgnoreCase))
                break;

            var match = Regex.Match(line, @"^(.+?)\s+-\s+(.+)$");
            if (!match.Success) continue;

            var name = match.Groups[1].Value.Trim();
            if (name.Length == 0) continue;

            var urlMatch = Regex.Match(match.Groups[2].Value, @"https?://\S+");
            var creditUrl = urlMatch.Success ? urlMatch.Value : "";

            result.Add(new ParsedArtist(name, creditUrl));
        }

        return result;
    }
}

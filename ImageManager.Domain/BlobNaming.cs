using System.Text.RegularExpressions;

namespace ImageManager.Domain;

// Derives the default Azure blob name for a synced image:
//   <Artist>/<Description><n><ext>   (or <Description><n><ext> when there is no artist)
// where Description is recovered from the group's "<Description>_<Artist>" folder name and
// <n> is the image's 1-based position in the group.
public static class BlobNaming
{
    public static string DefaultBlobName(string groupName, string? artistName, string fileName, string mimeType, int index)
    {
        var ext = ResolveExtension(fileName, mimeType);
        var artist = SanitizeArtist(artistName);
        var description = DescriptionFromGroupName(groupName, artist);

        var name = $"{description}{index + 1}{ext}";
        return artist.Length > 0 ? $"{artist}/{name}" : name;
    }

    // The original file extension, falling back to the MIME subtype (e.g. "image/png" -> ".png").
    private static string ResolveExtension(string fileName, string mimeType)
    {
        var ext = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(ext)) return ext;
        var slash = mimeType.IndexOf('/');
        return slash >= 0 ? "." + mimeType[(slash + 1)..] : "";
    }

    private static string SanitizeArtist(string? artistName)
        => artistName is null ? "" : Regex.Replace(artistName, "[^A-Za-z0-9]", "");

    // GroupName is "<description>_<artist>"; drop the trailing artist suffix, then keep only
    // alphanumerics and underscores. Falls back to "Image" when nothing usable remains.
    private static string DescriptionFromGroupName(string groupName, string sanitizedArtist)
    {
        var description = groupName;
        if (sanitizedArtist.Length > 0 && description.EndsWith("_" + sanitizedArtist, StringComparison.OrdinalIgnoreCase))
            description = description[..^(sanitizedArtist.Length + 1)];
        description = Regex.Replace(description, "[^A-Za-z0-9_]", "");
        return description.Length == 0 ? "Image" : description;
    }
}

namespace ImageManager.Configuration;

public sealed class AzureSearchOptions
{
    public const string Section = "AzureSearch";

    // e.g. https://<service-name>.search.windows.net
    public string Endpoint { get; set; } = "";

    // Admin API key (needed to create the index and push documents).
    public string ApiKey { get; set; } = "";

    public string IndexName { get; set; } = "book-chapters";

    // Separate index for notes, so note search never conflates with chapter search.
    public string NotesIndexName { get; set; } = "notes";
}

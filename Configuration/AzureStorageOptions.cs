namespace ImageManager.Configuration;

public sealed class AzureStorageOptions
{
    public const string Section = "AzureStorage";

    public string ConnectionString { get; set; } = "";

    // Static-website container that serves public images.
    public string WebContainer { get; set; } = "$web";

    // Private container holding commissions.json and its backups.
    public string DataContainer { get; set; } = "appdata";

    // Path prefix under the web container that images are uploaded beneath.
    public string HolisticPrefix { get; set; } = "Holistic/";

    // Custom-domain/CDN base for public URLs, e.g. https://piuimages.arroweclip.se
    public string PublicBaseUrl { get; set; } = "";
}

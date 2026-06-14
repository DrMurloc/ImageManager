namespace ImageManager.Configuration;

public sealed class GoogleDriveOptions
{
    public const string Section = "GoogleDrive";

    // Path to the service-account JSON key file, kept outside the repo. Preferred over inline.
    public string? ServiceAccountKeyPath { get; set; }

    // Inline service-account JSON, as an alternative to ServiceAccountKeyPath.
    public string? ServiceAccountJson { get; set; }

    // Drive folder ID of the /Commissions root, taken from the folder's URL.
    public string CommissionsRootFolderId { get; set; } = "";
}

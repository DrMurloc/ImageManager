namespace ImageManager.Application;

public sealed record ScannedGroup(string DriveFolderId, string CharacterNames, string GroupName);

public sealed record DriveFolderRef(string Id, string Name);

public sealed record DriveImageFile(
    string Id,
    string Name,
    string Path,
    string MimeType,
    long? Size,
    int? Width,
    int? Height,
    string? Md5);

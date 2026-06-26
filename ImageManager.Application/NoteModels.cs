namespace ImageManager.Application;

// Full content of a note plus its normalized path.
public sealed record Note(string Path, string Content);

// A note that matched a search, with its relevance score.
public sealed record NoteSearchHit(string Path, string Title, string Content, double Score);

// One note file directly under a folder.
public sealed record NoteEntry(string Path, string Title);

// One level of the notes tree: child folder prefixes and note files directly under a prefix.
public sealed record NoteListing(string Prefix, IReadOnlyList<string> Folders, IReadOnlyList<NoteEntry> Notes);

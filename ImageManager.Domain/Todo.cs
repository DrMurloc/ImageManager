namespace ImageManager.Domain;

public enum TodoScope
{
    Book,
    Chapter
}

// A todo for a book (high-level) or a specific chapter. ChapterNumber == null means book-level.
// Plain mutable entity (no ORM attributes) so EF Core can map it via the Infrastructure context
// while Domain stays persistence-ignorant.
public class Todo
{
    public Guid Id { get; set; }
    public string Book { get; set; } = "";
    public int? ChapterNumber { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public bool Done { get; set; }
    public int Order { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? CompletedUtc { get; set; }

    public TodoScope Scope => ChapterNumber is null ? TodoScope.Book : TodoScope.Chapter;

    public static Todo Create(string book, string title, int? chapterNumber, string? notes, int order, DateTimeOffset createdUtc)
    {
        var cleanBook = (book ?? "").Trim();
        var cleanTitle = (title ?? "").Trim();

        if (cleanBook.Length == 0)
            throw new ArgumentException("Book is required.", nameof(book));
        if (cleanTitle.Length == 0)
            throw new ArgumentException("Title is required.", nameof(title));
        if (chapterNumber is < 0)
            throw new ArgumentException("Chapter number cannot be negative.", nameof(chapterNumber));

        return new Todo
        {
            Id = Guid.NewGuid(),
            Book = cleanBook,
            ChapterNumber = chapterNumber,
            Title = cleanTitle,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Order = order,
            Done = false,
            CreatedUtc = createdUtc
        };
    }

    public void Complete(DateTimeOffset completedUtc)
    {
        Done = true;
        CompletedUtc = completedUtc;
    }

    public void Reopen()
    {
        Done = false;
        CompletedUtc = null;
    }
}

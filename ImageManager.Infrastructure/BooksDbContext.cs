using ImageManager.Domain;
using Microsoft.EntityFrameworkCore;

namespace ImageManager.Infrastructure;

// EF Core context for the "books" SQL schema. Mapping lives here (Fluent API) so the Domain
// Todo entity stays free of any ORM attributes.
public sealed class BooksDbContext : DbContext
{
    public BooksDbContext(DbContextOptions<BooksDbContext> options) : base(options)
    {
    }

    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var todo = modelBuilder.Entity<Todo>();
        todo.ToTable("Todo", "books");
        todo.HasKey(t => t.Id);
        todo.Property(t => t.Book).HasMaxLength(200).IsRequired();
        todo.Property(t => t.Title).HasMaxLength(500).IsRequired();
        // "Order" is a SQL keyword; EF quotes it. Kept explicit for clarity.
        todo.Property(t => t.Order).HasColumnName("Order");
        todo.HasIndex(t => new { t.Book, t.ChapterNumber });
    }
}

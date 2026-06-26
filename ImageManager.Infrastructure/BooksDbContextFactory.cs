using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ImageManager.Infrastructure;

// Lets `dotnet ef migrations add ...` construct the context without running the app or a real
// database. Migrations only need the model, so the connection string here is just a placeholder
// (override via the TODOS_SQL_CONNECTION env var if you ever run `database update` from the CLI).
public sealed class BooksDbContextFactory : IDesignTimeDbContextFactory<BooksDbContext>
{
    public BooksDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TODOS_SQL_CONNECTION")
            ?? "Server=(localdb)\\design;Database=ImageManagerDesign;Trusted_Connection=True;";

        var options = new DbContextOptionsBuilder<BooksDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new BooksDbContext(options);
    }
}

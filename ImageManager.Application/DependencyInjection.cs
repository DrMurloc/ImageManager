using Microsoft.Extensions.DependencyInjection;

namespace ImageManager.Application;

// Composition for the Application layer: registers pure, dependency-free use-case services.
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IHtmlBuilder, HtmlBuilder>();
        services.AddScoped<ICommissionGroupService, CommissionGroupService>();
        services.AddScoped<IImageSyncService, ImageSyncService>();
        services.AddScoped<IDocumentMigrationService, DocumentMigrationService>();
        services.AddScoped<IBookSearchIndexer, BookSearchIndexer>();
        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<ITodoService, TodoService>();
        return services;
    }
}

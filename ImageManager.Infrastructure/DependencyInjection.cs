using Azure.Storage.Blobs;
using ImageManager.Application;
using ImageManager.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ImageManager.Infrastructure;

// Composition root for the Infrastructure layer: binds options and registers the concrete
// adapters behind their Application-layer ports. This is the only Infrastructure type the Web
// project needs to reference.
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GoogleDriveOptions>(configuration.GetSection(GoogleDriveOptions.Section));
        services.Configure<AzureStorageOptions>(configuration.GetSection(AzureStorageOptions.Section));

        services.AddSingleton(sp =>
        {
            var connectionString = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "AzureStorage:ConnectionString is not set. Add it via User Secrets.");
            return new BlobServiceClient(connectionString);
        });

        services.AddHttpClient();
        services.AddSingleton<IDriveScanner, GoogleDriveScanner>();
        services.AddSingleton<IBlobSyncService, AzureBlobSyncService>();
        services.AddSingleton<IMetadataStore, JsonBlobMetadataStore>();
        services.AddSingleton<IHtmlBuilder, HtmlBuilder>();
        services.AddSingleton<IDriveUploader, DriveUploader>();
        services.AddSingleton<IGoogleUserTokens, GoogleUserTokenStore>();

        return services;
    }
}

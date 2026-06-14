using ImageManager.Application;
using ImageManager.Configuration;
using ImageManager.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ModelContextProtocol.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Search read-side only: this service never touches Drive/Blob/Google, just the index.
builder.Services.Configure<AzureSearchOptions>(builder.Configuration.GetSection(AzureSearchOptions.Section));
builder.Services.AddSingleton<ISearchQuery, AzureSearchQuery>();

// Entra OAuth is enabled only when configured, so the server can run no-auth locally for testing.
var tenantId = builder.Configuration["AzureAd:TenantId"];
var audience = builder.Configuration["AzureAd:Audience"];
var serverUrl = builder.Configuration["HttpMcpServerUrl"];
var authEnabled = !string.IsNullOrWhiteSpace(tenantId)
    && !string.IsNullOrWhiteSpace(audience)
    && !string.IsNullOrWhiteSpace(serverUrl);

if (authEnabled)
{
    var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";

    builder.Services.AddAuthentication(options =>
        {
            // Resource-server: validate Entra JWTs, but emit MCP's resource_metadata challenge on 401.
            options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.TokenValidationParameters.ValidAudiences =
                new[] { audience!, audience!.Replace("api://", "") };
        })
        .AddMcp(options =>
        {
            options.ResourceMetadata = new()
            {
                Resource = serverUrl!,
                AuthorizationServers = { authority },
                ScopesSupported = { "mcp:tools" }
            };
        });

    builder.Services.AddAuthorization();
}

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapMcp().RequireAuthorization();
}
else
{
    // No auth — local/dev only. Do not expose publicly without AzureAd configured.
    app.MapMcp();
}

app.Run();

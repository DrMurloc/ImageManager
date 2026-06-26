using ImageManager.Application;
using ImageManager.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ModelContextProtocol.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Same composition as the web app. The tools only resolve the search/notes/todos ports, so the
// Drive/Google/blob-sync singletons stay unconstructed — but reusing this keeps wiring in one place.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Entra OAuth is enabled only when configured, so the server can run no-auth locally for testing.
// Provider-agnostic OIDC: works with any authorization server that serves standard OAuth
// metadata (Auth0, etc.). Authority is the issuer; Audience is the API identifier.
var authority = builder.Configuration["Oauth:Authority"];
var audience = builder.Configuration["Oauth:Audience"];
var authEnabled = !string.IsNullOrWhiteSpace(authority) && !string.IsNullOrWhiteSpace(audience);

if (authEnabled)
{
    builder.Services.AddAuthentication(options =>
        {
            // Resource-server: validate JWTs, but emit MCP's resource_metadata challenge on 401.
            options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
        })
        .AddMcp(options =>
        {
            options.ResourceMetadata = new()
            {
                Resource = audience!,
                AuthorizationServers = { authority! },
                ScopesSupported = { "mcp:tools" }
            };
        });

    // Require the mcp:tools permission. With Auth0 RBAC, only users assigned that permission get
    // it in their token, so this is the access gate. Auth0 ("Add Permissions in the Access Token")
    // puts granted permissions in a "permissions" array claim; granted scopes are in "scope".
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("mcp", policy => policy.RequireAssertion(ctx =>
        {
            var permissions = ctx.User.FindAll("permissions").Select(c => c.Value);
            var scopes = (ctx.User.FindFirst("scope")?.Value ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return permissions.Contains("mcp:tools") || scopes.Contains("mcp:tools");
        }));
}

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapMcp().RequireAuthorization("mcp");
}
else
{
    // No auth — local/dev only. Do not expose publicly without Oauth configured.
    app.MapMcp();
}

app.Run();

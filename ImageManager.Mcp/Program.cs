using ImageManager.Application;
using ImageManager.Configuration;
using ImageManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Search read-side only: this service never touches Drive/Blob/Google, just the index.
builder.Services.Configure<AzureSearchOptions>(builder.Configuration.GetSection(AzureSearchOptions.Section));
builder.Services.AddSingleton<ISearchQuery, AzureSearchQuery>();

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();

// NOTE: no auth yet. Claude's custom connector requires OAuth 2.1 (static bearer tokens and
// URL tokens are not supported), so an OAuth layer must be added before this is exposed publicly.
app.MapMcp();

app.Run();

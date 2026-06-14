using System.Security.Claims;
using ImageManager.Application;
using ImageManager.Components;
using ImageManager.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Behind App Service's TLS-terminating proxy, trust the forwarded scheme so OAuth builds https redirect URIs.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (string.IsNullOrWhiteSpace(googleClientId) || string.IsNullOrWhiteSpace(googleClientSecret))
    throw new InvalidOperationException(
        "Authentication:Google:ClientId/ClientSecret are not set. Add them via User Secrets (local) or app settings (deployed).");

var allowedEmails = (builder.Configuration["Authentication:AllowedEmails"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "ImageManager.Auth";
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(365);
    options.SlidingExpiration = true;
})
.AddGoogle(options =>
{
    options.ClientId = googleClientId;
    options.ClientSecret = googleClientSecret;
    // Drive write access so the app can create folders / upload files as the signed-in user.
    options.Scope.Add("https://www.googleapis.com/auth/drive");
    options.AccessType = "offline";
    options.SaveTokens = true;
    options.Events.OnCreatingTicket = context =>
    {
        var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(context.AccessToken))
        {
            var tokens = context.HttpContext.RequestServices.GetRequiredService<IGoogleUserTokens>();
            var expiresAt = DateTimeOffset.UtcNow.Add(context.ExpiresIn ?? TimeSpan.FromHours(1));
            tokens.Capture(email, context.AccessToken, context.RefreshToken, expiresAt);
        }
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    // Hard block: must be signed in AND the Google email must be on the allowlist.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireAssertion(context =>
            context.User.FindFirst(ClaimTypes.Email)?.Value is string email &&
            allowedEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
        .Build();
});

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Streams a Drive image through the app so the UI can preview it before it's synced to $web.
app.MapGet("/preview/{fileId}", async (string fileId, string? type, IDriveScanner drive, CancellationToken ct) =>
{
    var bytes = await drive.DownloadAsync(fileId, ct);
    return Results.File(bytes, string.IsNullOrWhiteSpace(type) ? "application/octet-stream" : type);
});

app.MapGet("/login", (string? returnUrl) =>
    Results.Challenge(
        new AuthenticationProperties { RedirectUri = returnUrl ?? "/", IsPersistent = true },
        new[] { GoogleDefaults.AuthenticationScheme }))
    .AllowAnonymous();

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).AllowAnonymous();

app.MapGet("/access-denied", (HttpContext ctx) =>
{
    var email = System.Net.WebUtility.HtmlEncode(ctx.User.FindFirst(ClaimTypes.Email)?.Value ?? "that account");
    return Results.Content(
        $"<!doctype html><html><head><meta charset=\"utf-8\"><title>Access denied</title></head>" +
        $"<body style=\"font-family:sans-serif;text-align:center;padding:3rem;\">" +
        $"<h1>Access denied</h1><p>{email} isn't authorized to use this tool.</p>" +
        $"<p><a href=\"/logout\">Sign out and try another account</a></p></body></html>",
        "text/html");
}).AllowAnonymous();

app.Run();

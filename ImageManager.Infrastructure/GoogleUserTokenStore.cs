using System.Collections.Concurrent;
using System.Text.Json;
using ImageManager.Application;
using Microsoft.Extensions.Configuration;

namespace ImageManager.Infrastructure;

// In-memory store of the signed-in user's Google OAuth tokens, refreshed on demand via Google's token endpoint.
public sealed class GoogleUserTokenStore : IGoogleUserTokens
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

    private sealed record Tokens(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt);

    private readonly ConcurrentDictionary<string, Tokens> _byEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public GoogleUserTokenStore(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _clientId = config["Authentication:Google:ClientId"] ?? "";
        _clientSecret = config["Authentication:Google:ClientSecret"] ?? "";
    }

    public void Capture(string email, string accessToken, string? refreshToken, DateTimeOffset expiresAt)
        => _byEmail.AddOrUpdate(email,
            _ => new Tokens(accessToken, refreshToken, expiresAt),
            // Google only returns a refresh token on first consent; keep the existing one if this login omitted it.
            (_, existing) => new Tokens(accessToken, refreshToken ?? existing.RefreshToken, expiresAt));

    public async Task<string?> GetValidAccessTokenAsync(string email, CancellationToken ct = default)
    {
        if (!_byEmail.TryGetValue(email, out var tokens))
            return null;

        if (DateTimeOffset.UtcNow < tokens.ExpiresAt.AddMinutes(-2))
            return tokens.AccessToken;

        if (string.IsNullOrEmpty(tokens.RefreshToken))
            return null;

        var http = _httpClientFactory.CreateClient();
        var response = await http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = tokens.RefreshToken,
            ["grant_type"] = "refresh_token"
        }), ct);

        if (!response.IsSuccessStatusCode)
        {
            _byEmail.TryRemove(email, out _);
            return null;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
        var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;
        _byEmail[email] = tokens with
        {
            AccessToken = accessToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn)
        };
        return accessToken;
    }
}

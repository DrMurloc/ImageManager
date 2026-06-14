namespace ImageManager.Application;

// Holds the signed-in user's Google OAuth tokens (captured at login) for downstream Drive writes.
public interface IGoogleUserTokens
{
    void Capture(string email, string accessToken, string? refreshToken, DateTimeOffset expiresAt);

    // Returns a non-expired access token (refreshing if needed), or null if the user must reconnect.
    Task<string?> GetValidAccessTokenAsync(string email, CancellationToken ct = default);
}

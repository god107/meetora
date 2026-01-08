namespace Meetora.Api.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public required string JwtSigningKey { get; init; }
    public string JwtIssuer { get; init; } = "meetora";
    public string JwtAudience { get; init; } = "meetora";
    public int JwtExpiresMinutes { get; init; } = 60 * 24 * 7;

    // Google OAuth Client ID (used to validate Google ID tokens)
    public required string GoogleClientId { get; init; }

    // Pepper for HMAC hashing public tokens (separate from JWT key)
    public required string PublicTokenPepper { get; init; }
}

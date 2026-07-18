using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Services.Interfaces;

/// <summary>Result of minting an access token.</summary>
public sealed record AccessToken(string Token, string JwtId, DateTime ExpiresAtUtc);

/// <summary>Identity extracted from a (possibly expired) access token during refresh.</summary>
public sealed record TokenIdentity(int UserId, string JwtId);

public interface IJwtTokenService
{
    /// <summary>Creates a signed JWT access token for the given user and role.</summary>
    AccessToken CreateAccessToken(User user, string roleName);

    /// <summary>Creates a cryptographically-random opaque refresh token string.</summary>
    string CreateRefreshToken();

    /// <summary>
    /// Validates the token's signature (ignoring expiry) and returns the embedded user id and
    /// token id. Returns null when the signature is invalid or the required claims are missing.
    /// </summary>
    TokenIdentity? ReadExpiredToken(string accessToken);

    /// <summary>Refresh-token lifetime in days (from configuration).</summary>
    int RefreshTokenValidityDays { get; }
}

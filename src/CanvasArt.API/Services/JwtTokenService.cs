using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Models.Entities;
using CanvasArt.API.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CanvasArt.API.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
        if (string.IsNullOrWhiteSpace(_settings.SecretKey) || _settings.SecretKey.Length < 32)
            throw new InvalidOperationException("JWT SecretKey must be configured and at least 32 characters long.");
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
    }

    public int RefreshTokenValidityDays => _settings.RefreshTokenDays;

    public AccessToken CreateAccessToken(User user, string roleName)
    {
        var jwtId = Guid.NewGuid().ToString("N");
        var expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(ClaimTypes.Role, roleName)
        };

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(encoded, jwtId, expires);
    }

    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public TokenIdentity? ReadExpiredToken(string accessToken)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateLifetime = false, // expired access tokens are expected during refresh
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(accessToken, validationParameters, out var validated);

            if (validated is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!int.TryParse(sub, out var userId) || string.IsNullOrEmpty(jti))
                return null;

            return new TokenIdentity(userId, jti);
        }
        catch
        {
            return null;
        }
    }
}

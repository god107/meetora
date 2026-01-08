using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Meetora.Api.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Meetora.Api.Auth;

public interface IJwtTokenService
{
    (string accessToken, DateTimeOffset expiresAtUtc) CreateAccessToken(AppUser user);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly AuthOptions _options;

    public JwtTokenService(IOptions<AuthOptions> options)
    {
        _options = options.Value;
    }

    public (string accessToken, DateTimeOffset expiresAtUtc) CreateAccessToken(AppUser user)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_options.JwtExpiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("google_sub", user.GoogleSubject),
        };

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            claims.Add(new("name", user.DisplayName));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.JwtIssuer,
            audience: _options.JwtAudience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return (accessToken, expires);
    }
}

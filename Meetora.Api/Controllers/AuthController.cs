using Google.Apis.Auth;
using Meetora.Api.Auth;
using Meetora.Api.Contracts.Auth;
using Meetora.Api.Data;
using Meetora.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Meetora.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AuthOptions _authOptions;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        ApplicationDbContext db,
        IOptions<AuthOptions> authOptions,
        IJwtTokenService jwtTokenService)
    {
        _db = db;
        _authOptions = authOptions.Value;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("google")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return BadRequest(new { error = "id_token_required" });
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _authOptions.GoogleClientId },
                });
        }
        catch
        {
            return BadRequest(new { error = "invalid_google_id_token" });
        }

        if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
        {
            return BadRequest(new { error = "google_token_missing_claims" });
        }

        var now = DateTimeOffset.UtcNow;

        var user = await _db.Users.SingleOrDefaultAsync(u => u.GoogleSubject == payload.Subject, cancellationToken);
        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                GoogleSubject = payload.Subject,
                Email = payload.Email,
                DisplayName = payload.Name,
                PictureUrl = payload.Picture,
                CreatedAt = now,
                LastLoginAt = now,
            };

            _db.Users.Add(user);
        }
        else
        {
            user.Email = payload.Email;
            user.DisplayName = payload.Name;
            user.PictureUrl = payload.Picture;
            user.LastLoginAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var (accessToken, expiresAtUtc) = _jwtTokenService.CreateAccessToken(user);

        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAtUtc,
            User = new LoginResponse.UserDto
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                PictureUrl = user.PictureUrl,
            },
        });
    }
}

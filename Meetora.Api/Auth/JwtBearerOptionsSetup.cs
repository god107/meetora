using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Meetora.Api.Auth;

public sealed class JwtBearerOptionsSetup : IConfigureOptions<JwtBearerOptions>
{
    private readonly IOptions<AuthOptions> _authOptions;

    public JwtBearerOptionsSetup(IOptions<AuthOptions> authOptions)
    {
        _authOptions = authOptions;
    }

    public void Configure(JwtBearerOptions options)
    {
        var auth = _authOptions.Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = auth.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = auth.JwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.JwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = "sub",
        };
    }
}

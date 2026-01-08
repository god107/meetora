using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Meetora.Api.Public;

public interface IPublicTokenService
{
    string GenerateToken();
    byte[] ComputeHash(string token);
    string ProtectToken(string token);
    string UnprotectToken(string protectedToken);
}

public sealed class PublicTokenService : IPublicTokenService
{
    private readonly byte[] _pepper;
    private readonly IDataProtector _protector;

    public PublicTokenService(string pepper, IDataProtectionProvider dataProtectionProvider)
    {
        _pepper = System.Text.Encoding.UTF8.GetBytes(pepper);
        _protector = dataProtectionProvider.CreateProtector("Meetora.PublicToken.v1");
    }

    public string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    public byte[] ComputeHash(string token)
    {
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
        using var hmac = new HMACSHA256(_pepper);
        return hmac.ComputeHash(tokenBytes);
    }

    public string ProtectToken(string token)
    {
        return _protector.Protect(token);
    }

    public string UnprotectToken(string protectedToken)
    {
        return _protector.Unprotect(protectedToken);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> data)
    {
        var base64 = Convert.ToBase64String(data);
        return base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

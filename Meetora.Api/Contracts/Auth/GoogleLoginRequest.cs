namespace Meetora.Api.Contracts.Auth;

public sealed class GoogleLoginRequest
{
    public required string IdToken { get; init; }
}

namespace Meetora.Api.Contracts.Auth;

public sealed class LoginResponse
{
    public required string AccessToken { get; init; }
    public required DateTimeOffset ExpiresAtUtc { get; init; }

    public required UserDto User { get; init; }

    public sealed class UserDto
    {
        public required Guid Id { get; init; }
        public required string Email { get; init; }
        public string? DisplayName { get; init; }
        public string? PictureUrl { get; init; }
    }
}

using System.Security.Claims;

namespace Meetora.Api.Auth;

public interface ICurrentUserAccessor
{
    Guid GetRequiredUserId();
}

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetRequiredUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var sub = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
        {
            throw new InvalidOperationException("Authenticated user id is missing or invalid.");
        }

        return userId;
    }
}

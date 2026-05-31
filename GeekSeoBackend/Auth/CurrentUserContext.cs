using System.Security.Claims;

namespace GeekSeoBackend.Auth;

public sealed class CurrentUserContext(IHttpContextAccessor accessor, WorkerUserContext worker) : ICurrentUserContext
{
    public bool IsAuthenticated => TryResolveUserId(out _);

    public string? Email =>
        accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
        ?? accessor.HttpContext?.User.FindFirstValue("email");

    public Guid UserId
    {
        get
        {
            if (TryResolveUserId(out var id))
                return id;
            throw new UnauthorizedAccessException("User is not authenticated.");
        }
    }

    private bool TryResolveUserId(out Guid userId)
    {
        userId = Guid.Empty;
        if (worker.UserId != Guid.Empty)
        {
            userId = worker.UserId;
            return true;
        }

        var sub = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? accessor.HttpContext?.User.FindFirstValue("sub");
        if (Guid.TryParse(sub, out userId) && userId != Guid.Empty)
            return true;

        var header = accessor.HttpContext?.Request.Headers["X-User-Id"].ToString();
        if (Guid.TryParse(header, out userId) && userId != Guid.Empty)
            return true;

        return false;
    }
}

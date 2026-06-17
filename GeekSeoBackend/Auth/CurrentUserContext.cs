using System.Security.Claims;

namespace GeekSeoBackend.Auth;

public sealed class CurrentUserContext(IHttpContextAccessor accessor, WorkerUserContext worker) : ICurrentUserContext
{
    public bool IsAuthenticated => TryResolveUserId(out _);

    public string? Email
    {
        get
        {
            var ctx = HttpContextAccess.TryGetLiveContext(accessor);
            return ctx?.User.FindFirstValue(ClaimTypes.Email)
                ?? ctx?.User.FindFirstValue("email");
        }
    }

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
        if (worker.UserId != Guid.Empty)
        {
            userId = worker.UserId;
            return true;
        }

        var ctx = HttpContextAccess.TryGetLiveContext(accessor);
        return UserIdResolver.TryResolve(
            Guid.Empty,
            ctx?.User,
            ctx?.Request.Headers["X-User-Id"].ToString(),
            out userId);
    }
}

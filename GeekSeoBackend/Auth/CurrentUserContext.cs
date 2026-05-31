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

    private bool TryResolveUserId(out Guid userId) =>
        UserIdResolver.TryResolve(
            worker.UserId,
            accessor.HttpContext?.User,
            accessor.HttpContext?.Request.Headers["X-User-Id"].ToString(),
            out userId);
}

using System.Security.Claims;

namespace GeekSeoBackend.Auth;

public static class UserIdResolver
{
    public static bool TryResolve(
        Guid workerUserId,
        ClaimsPrincipal? user,
        string? headerUserId,
        out Guid userId)
    {
        userId = Guid.Empty;
        if (workerUserId != Guid.Empty)
        {
            userId = workerUserId;
            return true;
        }

        var sub = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue("sub");
        if (Guid.TryParse(sub, out userId) && userId != Guid.Empty)
            return true;

        if (Guid.TryParse(headerUserId, out userId) && userId != Guid.Empty)
            return true;

        return false;
    }
}

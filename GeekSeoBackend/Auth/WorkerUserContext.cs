using GeekSeo.Application.Interfaces;

namespace GeekSeoBackend.Auth;

/// <summary>
/// User id for background workers (no HTTP request). Uses AsyncLocal so concurrent jobs do not clobber each other.
/// </summary>
public sealed class WorkerUserContext : IBackgroundUserContext
{
    private readonly AsyncLocal<Guid> _userId = new();

    public Guid UserId
    {
        get => _userId.Value;
        set => _userId.Value = value;
    }

    public void SetUserId(Guid userId) => UserId = userId;
}

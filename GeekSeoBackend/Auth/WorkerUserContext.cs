namespace GeekSeoBackend.Auth;

/// <summary>
/// User id for background workers (no HTTP request). Set per job iteration before scoped services run.
/// </summary>
public sealed class WorkerUserContext
{
    public Guid UserId { get; set; }
}

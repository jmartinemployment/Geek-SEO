using System.Collections.Concurrent;

namespace GeekSeoBackend.Services;

/// <summary>
/// Per-profile execution lock — ensures only one step runs per profile at a time.
/// Registered as singleton.
/// </summary>
public sealed class NicheStepLock
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public SemaphoreSlim Get(Guid profileId) =>
        _locks.GetOrAdd(profileId, _ => new SemaphoreSlim(1, 1));

    public bool IsLocked(Guid profileId) =>
        _locks.TryGetValue(profileId, out var s) && s.CurrentCount == 0;
}

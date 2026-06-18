using System.Collections.Concurrent;

namespace GeekSeoBackend.Services;

/// <summary>Short-lived cache for hub group ACL checks to avoid redundant DB hits on rapid navigation.</summary>
public sealed class HubGroupAccessCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(45);
    private readonly ConcurrentDictionary<string, (bool Allowed, DateTime Expires)> _entries = new();

    public bool? TryGet(string key)
    {
        if (!_entries.TryGetValue(key, out var entry)) return null;
        if (DateTime.UtcNow > entry.Expires)
        {
            _entries.TryRemove(key, out _);
            return null;
        }
        return entry.Allowed;
    }

    public void Set(string key, bool allowed)
    {
        _entries[key] = (allowed, DateTime.UtcNow.Add(Ttl));
    }

    public static string Key(Guid userId, string kind, Guid entityId) =>
        $"{userId:N}:{kind}:{entityId:N}";
}

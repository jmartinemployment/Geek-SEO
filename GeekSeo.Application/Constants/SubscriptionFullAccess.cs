namespace GeekSeo.Application.Constants.Seo;

/// <summary>
/// Operator accounts that always get full product access (no PayPal row required).
/// Set SUBSCRIPTION_FULL_ACCESS_EMAILS on GeekSeoBackend (comma-separated).
/// </summary>
public static class SubscriptionFullAccess
{
    private static HashSet<Guid>? _userIds;
    private static HashSet<string>? _emails;
    private static readonly object LoadLock = new();

    public static bool IsGranted(Guid userId, string? email)
    {
        EnsureLoaded();
        if (_userIds!.Contains(userId))
            return true;

        if (string.IsNullOrWhiteSpace(email))
            return false;

        return _emails!.Contains(email.Trim().ToLowerInvariant());
    }

    private static void EnsureLoaded()
    {
        if (_userIds is not null)
            return;

        lock (LoadLock)
        {
            if (_userIds is not null)
                return;

            _userIds = ParseGuids(Environment.GetEnvironmentVariable("SUBSCRIPTION_FULL_ACCESS_USER_IDS"));
            _emails = ParseEmails(Environment.GetEnvironmentVariable("SUBSCRIPTION_FULL_ACCESS_EMAILS"));
        }
    }

    private static HashSet<Guid> ParseGuids(string? raw)
    {
        var set = new HashSet<Guid>();
        if (string.IsNullOrWhiteSpace(raw))
            return set;

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(part, out var id) && id != Guid.Empty)
                set.Add(id);
        }

        return set;
    }

    private static HashSet<string> ParseEmails(string? raw)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(raw))
            return set;

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = part.ToLowerInvariant();
            if (normalized.Length > 0)
                set.Add(normalized);
        }

        return set;
    }
}

using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace GeekSeoBackend.Services;

public sealed class InMemoryGoogleOAuthStateStore(IMemoryCache cache) : IGoogleOAuthStateStore
{
    private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(15);
    private const string StatePrefix = "google-oauth-state:";

    public (string State, DateTimeOffset ExpiresAt) Create(GoogleOAuthStatePayload payload)
    {
        var state = Convert.ToHexString(Guid.NewGuid().ToByteArray())
            + Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var expiresAt = DateTimeOffset.UtcNow.Add(StateTtl);
        cache.Set(StatePrefix + state, payload, expiresAt);
        return (state, expiresAt);
    }

    public GoogleOAuthStatePayload Consume(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw new GoogleIntegrationException("Missing OAuth state.", StatusCodes.Status400BadRequest);

        var key = StatePrefix + state.Trim();
        if (!cache.TryGetValue<GoogleOAuthStatePayload>(key, out var payload) || payload is null)
        {
            throw new GoogleIntegrationException(
                "OAuth state is invalid or expired. Restart Google connection.",
                StatusCodes.Status400BadRequest);
        }

        cache.Remove(key);
        return payload;
    }

    public bool TryPeek(string state, out GoogleOAuthStatePayload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(state))
            return false;

        var key = StatePrefix + state.Trim();
        if (!cache.TryGetValue<GoogleOAuthStatePayload>(key, out var found) || found is null)
            return false;

        payload = found;
        return true;
    }
}

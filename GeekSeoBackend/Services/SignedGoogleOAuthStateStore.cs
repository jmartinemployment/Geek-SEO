using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeekSeoBackend.Services;

/// <summary>
/// Stateless OAuth state using HMAC-signed payloads so callbacks succeed on any backend instance.
/// </summary>
public sealed class SignedGoogleOAuthStateStore : IGoogleOAuthStateStore
{
    private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(15);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public (string State, DateTimeOffset ExpiresAt) Create(GoogleOAuthStatePayload payload)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(StateTtl);
        var envelope = new StateEnvelope
        {
            UserId = payload.UserId,
            ProjectId = payload.ProjectId,
            PropertyId = payload.PropertyId,
            SiteUrl = payload.SiteUrl,
            Exp = expiresAt.ToUnixTimeSeconds(),
            Nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
        };

        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        var signature = ComputeSignature(json);
        var state = $"{Base64UrlEncode(Encoding.UTF8.GetBytes(json))}.{Base64UrlEncode(signature)}";
        return (state, expiresAt);
    }

    public GoogleOAuthStatePayload Consume(string state) => Parse(state, requireUnexpired: true);

    public bool TryPeek(string state, out GoogleOAuthStatePayload? payload)
    {
        try
        {
            payload = Parse(state, requireUnexpired: false);
            return true;
        }
        catch
        {
            payload = null;
            return false;
        }
    }

    private static GoogleOAuthStatePayload Parse(string state, bool requireUnexpired)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw new GoogleIntegrationException("Missing OAuth state.", StatusCodes.Status400BadRequest);

        var trimmed = state.Trim();
        var separator = trimmed.LastIndexOf('.');
        if (separator <= 0 || separator >= trimmed.Length - 1)
        {
            throw new GoogleIntegrationException(
                "OAuth state is invalid or expired. Restart Google connection.",
                StatusCodes.Status400BadRequest);
        }

        byte[] payloadBytes;
        byte[] providedSignature;
        try
        {
            payloadBytes = Base64UrlDecode(trimmed[..separator]);
            providedSignature = Base64UrlDecode(trimmed[(separator + 1)..]);
        }
        catch (FormatException)
        {
            throw new GoogleIntegrationException(
                "OAuth state is invalid or expired. Restart Google connection.",
                StatusCodes.Status400BadRequest);
        }

        var json = Encoding.UTF8.GetString(payloadBytes);
        var expectedSignature = ComputeSignature(json);
        if (!CryptographicOperations.FixedTimeEquals(expectedSignature, providedSignature))
        {
            throw new GoogleIntegrationException(
                "OAuth state is invalid or expired. Restart Google connection.",
                StatusCodes.Status400BadRequest);
        }

        StateEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<StateEnvelope>(json, JsonOptions);
        }
        catch (JsonException)
        {
            envelope = null;
        }

        if (envelope is null || envelope.UserId == Guid.Empty || envelope.ProjectId == Guid.Empty)
        {
            throw new GoogleIntegrationException(
                "OAuth state is invalid or expired. Restart Google connection.",
                StatusCodes.Status400BadRequest);
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(envelope.Exp);
        if (requireUnexpired && expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new GoogleIntegrationException(
                "OAuth state is invalid or expired. Restart Google connection.",
                StatusCodes.Status400BadRequest);
        }

        return new GoogleOAuthStatePayload
        {
            UserId = envelope.UserId,
            ProjectId = envelope.ProjectId,
            PropertyId = envelope.PropertyId,
            SiteUrl = envelope.SiteUrl,
        };
    }

    private static byte[] ComputeSignature(string json)
    {
        var key = GetSigningKey();
        return HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(json));
    }

    private static byte[] GetSigningKey()
    {
        var raw = Environment.GetEnvironmentVariable("GEEK_SEO_ENCRYPTION_KEY");
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "GEEK_SEO_ENCRYPTION_KEY must be set (base64-encoded 32-byte key) for Google OAuth state signing.");
        }

        var key = Convert.FromBase64String(raw.Trim());
        if (key.Length != 32)
            throw new InvalidOperationException("GEEK_SEO_ENCRYPTION_KEY must decode to exactly 32 bytes.");
        return key;
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }

    private sealed class StateEnvelope
    {
        public Guid UserId { get; init; }
        public Guid ProjectId { get; init; }
        public string? PropertyId { get; init; }
        public string? SiteUrl { get; init; }
        public long Exp { get; init; }
        public string Nonce { get; init; } = string.Empty;
    }
}

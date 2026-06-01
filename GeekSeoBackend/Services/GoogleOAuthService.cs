using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeoBackend.Models;

namespace GeekSeoBackend.Services;

public sealed class GoogleOAuthService(
    IHttpClientFactory httpClientFactory,
    IGoogleOAuthStateStore stateStore,
    IGoogleIntegrationRepository integrations,
    IProjectRepository projects,
    GoogleOAuthOptions options) : IGoogleOAuthService
{
    private static readonly string[] Scopes =
    [
        "https://www.googleapis.com/auth/webmasters.readonly",
        "https://www.googleapis.com/auth/analytics.readonly"
    ];

    public async Task<GoogleConnectUrlResponse> GetConnectUrlAsync(
        Guid userId,
        Guid projectId,
        string? propertyId,
        string? siteUrl,
        CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureProjectOwnershipAsync(userId, projectId, ct);

        var (state, expiresAt) = stateStore.Create(new GoogleOAuthStatePayload
        {
            UserId = userId,
            ProjectId = projectId,
            PropertyId = string.IsNullOrWhiteSpace(propertyId) ? null : propertyId.Trim(),
            SiteUrl = string.IsNullOrWhiteSpace(siteUrl) ? null : siteUrl.Trim(),
        });

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = options.RedirectUri,
            ["response_type"] = "code",
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["state"] = state,
            ["scope"] = string.Join(' ', Scopes),
        };

        var consentUri = BuildUri("https://accounts.google.com/o/oauth2/v2/auth", query);
        return new GoogleConnectUrlResponse
        {
            Url = consentUri.ToString(),
            ExpiresAt = expiresAt,
        };
    }

    public async Task<GoogleCallbackOutcome> HandleCallbackAsync(string code, string state, CancellationToken ct = default)
    {
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(code))
            throw new GoogleIntegrationException("Missing authorization code.", StatusCodes.Status400BadRequest);

        var payload = stateStore.Consume(state);
        await EnsureProjectOwnershipAsync(payload.UserId, payload.ProjectId, ct);

        var token = await ExchangeCodeAsync(code.Trim(), ct);
        if (string.IsNullOrWhiteSpace(token.RefreshToken))
            throw new GoogleIntegrationException(
                "Google did not provide a refresh token. Reconnect with consent.",
                StatusCodes.Status502BadGateway);

        var refreshToken = token.RefreshToken!;
        var (tokenCipher, tokenIv, tokenTag) = SeoCredentialProtector.Encrypt(refreshToken);

        var accessibleSites = await ListAccessibleSiteUrlsAsync(token.AccessToken, ct);
        var siteUrl = GscSiteUrlMatcher.Match(accessibleSites, payload.SiteUrl);

        if (string.IsNullOrWhiteSpace(siteUrl))
        {
            throw new GoogleIntegrationException(
                accessibleSites.Count == 0
                    ? "No Search Console property was returned for this Google account. Verify the site in Search Console, then reconnect."
                    : "No Search Console property matches this project URL. Add the site in Search Console, then reconnect.",
                StatusCodes.Status400BadRequest);
        }

        var gscConnection = new SeoGscConnection
        {
            Id = Guid.NewGuid(),
            ProjectId = payload.ProjectId,
            UserId = payload.UserId,
            SiteUrl = siteUrl,
            EncryptedRefreshToken = tokenCipher,
            EncryptionIv = tokenIv,
            EncryptionTag = tokenTag,
            ConnectedAt = DateTimeOffset.UtcNow,
        };

        var gscUpsert = await integrations.UpsertGscConnectionAsync(gscConnection, ct);
        if (!gscUpsert.IsSuccess)
            throw new GoogleIntegrationException(gscUpsert.Error ?? "Failed to persist Google Search Console connection.");

        var candidates = await ListGa4PropertyCandidatesAsync(token.AccessToken, ct);
        var propertyId = Ga4PropertyMatcher.Match(candidates, siteUrl, payload.PropertyId);
        propertyId = string.IsNullOrWhiteSpace(propertyId) ? null : Ga4PropertyMatcher.NormalizePropertyId(propertyId);

        var ga4Connected = false;
        if (!string.IsNullOrWhiteSpace(propertyId))
        {
            var ga4 = new SeoGa4Connection
            {
                Id = Guid.NewGuid(),
                ProjectId = payload.ProjectId,
                PropertyId = propertyId,
                EncryptedRefreshToken = tokenCipher,
                EncryptionIv = tokenIv,
                EncryptionTag = tokenTag,
                ConnectedAt = DateTimeOffset.UtcNow,
            };
            var ga4Upsert = await integrations.UpsertGa4ConnectionAsync(ga4, payload.UserId, ct);
            if (!ga4Upsert.IsSuccess)
                throw new GoogleIntegrationException(ga4Upsert.Error ?? "Failed to persist Google Analytics 4 connection.");
            ga4Connected = true;
        }

        return new GoogleCallbackOutcome
        {
            ProjectId = payload.ProjectId,
            GscConnected = true,
            Ga4Connected = ga4Connected,
            SiteUrl = siteUrl,
            PropertyId = propertyId,
        };
    }

    public async Task<GoogleIntegrationStatusResponse> GetStatusAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        await EnsureProjectOwnershipAsync(userId, projectId, ct);

        var gsc = await integrations.GetGscConnectionAsync(projectId, userId, ct);
        if (!gsc.IsSuccess)
            throw new GoogleIntegrationException(gsc.Error ?? "Failed to query GSC connection.");

        var ga4 = await integrations.GetGa4ConnectionAsync(projectId, userId, ct);
        if (!ga4.IsSuccess)
            throw new GoogleIntegrationException(ga4.Error ?? "Failed to query GA4 connection.");

        var connectedAt = new[] { gsc.Value?.ConnectedAt, ga4.Value?.ConnectedAt }
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty()
            .Max();

        return new GoogleIntegrationStatusResponse
        {
            Connected = gsc.Value is not null || ga4.Value is not null,
            GscConnected = gsc.Value is not null,
            Ga4Connected = ga4.Value is not null,
            SiteUrl = gsc.Value?.SiteUrl,
            PropertyId = ga4.Value?.PropertyId,
            ConnectedAt = connectedAt == default ? null : connectedAt,
        };
    }

    public async Task DisconnectAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        await EnsureProjectOwnershipAsync(userId, projectId, ct);
        var deleted = await integrations.DeleteConnectionsAsync(projectId, userId, ct);
        if (!deleted.IsSuccess)
            throw new GoogleIntegrationException(deleted.Error ?? "Failed to disconnect Google integrations.");
    }

    public async Task<string> GetGscAccessTokenAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureProjectOwnershipAsync(userId, projectId, ct);
        var gsc = await integrations.GetGscConnectionAsync(projectId, userId, ct);
        if (!gsc.IsSuccess)
            throw new GoogleIntegrationException(gsc.Error ?? "Failed to load GSC connection.");
        if (gsc.Value is null)
            throw new GoogleIntegrationException("Google Search Console is not connected.", StatusCodes.Status400BadRequest);

        var refreshToken = SeoCredentialProtector.Decrypt(
            gsc.Value.EncryptedRefreshToken,
            gsc.Value.EncryptionIv,
            gsc.Value.EncryptionTag);
        return await RefreshAccessTokenAsync(refreshToken, ct);
    }

    public async Task<string?> SyncGscSiteUrlAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureProjectOwnershipAsync(userId, projectId, ct);
        var gsc = await integrations.GetGscConnectionAsync(projectId, userId, ct);
        if (!gsc.IsSuccess)
            throw new GoogleIntegrationException(gsc.Error ?? "Failed to load GSC connection.");
        if (gsc.Value is null)
            return null;

        var refreshToken = SeoCredentialProtector.Decrypt(
            gsc.Value.EncryptedRefreshToken,
            gsc.Value.EncryptionIv,
            gsc.Value.EncryptionTag);
        var accessToken = await RefreshAccessTokenAsync(refreshToken, ct);
        var accessibleSites = await ListAccessibleSiteUrlsAsync(accessToken, ct);
        var resolved = GscSiteUrlMatcher.Match(accessibleSites, gsc.Value.SiteUrl);
        if (string.IsNullOrWhiteSpace(resolved) || string.Equals(resolved, gsc.Value.SiteUrl, StringComparison.Ordinal))
            return gsc.Value.SiteUrl;

        gsc.Value.SiteUrl = resolved;
        var upsert = await integrations.UpsertGscConnectionAsync(gsc.Value, ct);
        if (!upsert.IsSuccess)
            throw new GoogleIntegrationException(upsert.Error ?? "Failed to update GSC site URL.");

        return resolved;
    }

    public async Task<string?> SyncGa4PropertyAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureProjectOwnershipAsync(userId, projectId, ct);

        var project = await projects.GetByIdAsync(projectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            return null;

        var ga4 = await integrations.GetGa4ConnectionAsync(projectId, userId, ct);
        if (!ga4.IsSuccess || ga4.Value is null)
            return null;

        var refreshToken = SeoCredentialProtector.Decrypt(
            ga4.Value.EncryptedRefreshToken,
            ga4.Value.EncryptionIv,
            ga4.Value.EncryptionTag);
        var accessToken = await RefreshAccessTokenAsync(refreshToken, ct);
        var candidates = await ListGa4PropertyCandidatesAsync(accessToken, ct);
        var resolved = Ga4PropertyMatcher.Match(candidates, project.Value.Url, ga4.Value.PropertyId);
        resolved = string.IsNullOrWhiteSpace(resolved)
            ? null
            : Ga4PropertyMatcher.NormalizePropertyId(resolved);

        if (string.IsNullOrWhiteSpace(resolved)
            || string.Equals(resolved, Ga4PropertyMatcher.NormalizePropertyId(ga4.Value.PropertyId), StringComparison.Ordinal))
        {
            return ga4.Value.PropertyId;
        }

        ga4.Value.PropertyId = resolved;
        var upsert = await integrations.UpsertGa4ConnectionAsync(ga4.Value, userId, ct);
        if (!upsert.IsSuccess)
            throw new GoogleIntegrationException(upsert.Error ?? "Failed to update GA4 property ID.");

        return resolved;
    }

    public async Task<(string AccessToken, string PropertyId)> GetGa4AccessTokenAsync(
        Guid userId,
        Guid projectId,
        CancellationToken ct = default)
    {
        EnsureConfigured();
        await EnsureProjectOwnershipAsync(userId, projectId, ct);
        var ga4 = await integrations.GetGa4ConnectionAsync(projectId, userId, ct);
        if (!ga4.IsSuccess)
            throw new GoogleIntegrationException(ga4.Error ?? "Failed to load GA4 connection.");
        if (ga4.Value is null)
            throw new GoogleIntegrationException("Google Analytics 4 is not connected.", StatusCodes.Status400BadRequest);

        var refreshToken = SeoCredentialProtector.Decrypt(
            ga4.Value.EncryptedRefreshToken,
            ga4.Value.EncryptionIv,
            ga4.Value.EncryptionTag);
        var accessToken = await RefreshAccessTokenAsync(refreshToken, ct);
        return (accessToken, Ga4PropertyMatcher.NormalizePropertyId(ga4.Value.PropertyId));
    }

    private async Task EnsureProjectOwnershipAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(projectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            throw new GoogleIntegrationException("Project not found or access denied.", StatusCodes.Status404NotFound);
    }

    private async Task<GoogleTokenResponse> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        var form = new Dictionary<string, string?>
        {
            ["code"] = code,
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
            ["redirect_uri"] = options.RedirectUri,
            ["grant_type"] = "authorization_code",
        };
        return await TokenRequestAsync(form, ct);
    }

    private async Task<string> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct)
    {
        var form = new Dictionary<string, string?>
        {
            ["refresh_token"] = refreshToken,
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
            ["grant_type"] = "refresh_token",
        };
        var token = await TokenRequestAsync(form, ct);
        return token.AccessToken;
    }

    private async Task<GoogleTokenResponse> TokenRequestAsync(
        IReadOnlyDictionary<string, string?> formFields,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("GoogleOAuth");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(formFields!),
        };

        using var response = await client.SendAsync(request, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = FormatGoogleTokenError(raw);
            throw new GoogleIntegrationException(
                $"Google token endpoint failed ({(int)response.StatusCode}){detail}.",
                StatusCodes.Status502BadGateway);
        }

        GoogleTokenResponse? token;
        try
        {
            token = JsonSerializer.Deserialize<GoogleTokenResponse>(raw);
        }
        catch
        {
            throw new GoogleIntegrationException("Google token response was invalid JSON.", StatusCodes.Status502BadGateway);
        }

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
            throw new GoogleIntegrationException("Google token response missing access token.", StatusCodes.Status502BadGateway);

        return token;
    }

    private async Task<IReadOnlyList<string>> ListAccessibleSiteUrlsAsync(string accessToken, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("GoogleApis");
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/webmasters/v3/sites");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return [];

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!doc.RootElement.TryGetProperty("siteEntry", out var entries) || entries.ValueKind != JsonValueKind.Array)
            return [];

        var sites = new List<string>();
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("siteUrl", out var siteUrl))
                continue;
            var value = siteUrl.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                sites.Add(value);
        }

        return sites;
    }

    private async Task<IReadOnlyList<Ga4PropertyCandidate>> ListGa4PropertyCandidatesAsync(
        string accessToken,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("GoogleApis");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://analyticsadmin.googleapis.com/v1beta/accountSummaries?pageSize=200");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return [];

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!doc.RootElement.TryGetProperty("accountSummaries", out var summaries)
            || summaries.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var candidates = new List<Ga4PropertyCandidate>();
        foreach (var summary in summaries.EnumerateArray())
        {
            if (!summary.TryGetProperty("propertySummaries", out var properties)
                || properties.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var property in properties.EnumerateArray())
            {
                if (!property.TryGetProperty("property", out var propertyPath))
                    continue;

                var full = propertyPath.GetString();
                var id = Ga4PropertyMatcher.NormalizePropertyId(full);
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var displayName = property.TryGetProperty("displayName", out var nameEl)
                    ? nameEl.GetString() ?? id
                    : id;
                var webUris = await ListWebStreamUrisAsync(accessToken, id, ct);
                candidates.Add(new Ga4PropertyCandidate(id, displayName, webUris));
            }
        }

        return candidates;
    }

    private async Task<IReadOnlyList<string>> ListWebStreamUrisAsync(
        string accessToken,
        string propertyId,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("GoogleApis");
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://analyticsadmin.googleapis.com/v1beta/properties/{propertyId}/dataStreams");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return [];

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!doc.RootElement.TryGetProperty("dataStreams", out var streams)
            || streams.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var uris = new List<string>();
        foreach (var dataStream in streams.EnumerateArray())
        {
            if (!dataStream.TryGetProperty("webStreamData", out var webStream))
                continue;
            if (!webStream.TryGetProperty("defaultUri", out var uriEl))
                continue;
            var uri = uriEl.GetString();
            if (!string.IsNullOrWhiteSpace(uri))
                uris.Add(uri);
        }

        return uris;
    }

    private static Uri BuildUri(string baseUrl, IReadOnlyDictionary<string, string?> query)
    {
        var sb = new StringBuilder(baseUrl);
        sb.Append('?');
        var first = true;
        foreach (var pair in query)
        {
            if (pair.Value is null)
                continue;
            if (!first)
                sb.Append('&');
            first = false;
            sb.Append(Uri.EscapeDataString(pair.Key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(pair.Value));
        }

        return new Uri(sb.ToString(), UriKind.Absolute);
    }

    private static string FormatGoogleTokenError(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (!root.TryGetProperty("error", out var errorProp))
                return $": {raw[..Math.Min(raw.Length, 120)]}";

            var code = errorProp.GetString();
            var description = root.TryGetProperty("error_description", out var descProp)
                ? descProp.GetString()
                : null;
            return description is not null
                ? $": {code} — {description}"
                : $": {code}";
        }
        catch
        {
            return $": {raw[..Math.Min(raw.Length, 120)]}";
        }
    }

    private void EnsureConfigured()
    {
        if (!string.IsNullOrWhiteSpace(options.ClientId)
            && !string.IsNullOrWhiteSpace(options.ClientSecret)
            && !string.IsNullOrWhiteSpace(options.RedirectUri))
        {
            return;
        }

        throw new GoogleIntegrationException(
            "Google OAuth is not configured. Set GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, and GOOGLE_REDIRECT_URI.",
            StatusCodes.Status503ServiceUnavailable);
    }
}

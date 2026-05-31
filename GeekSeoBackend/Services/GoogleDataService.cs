using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GeekSeoBackend.Models;

namespace GeekSeoBackend.Services;

public sealed class GoogleDataService(
    IGoogleOAuthService oauth,
    IGoogleIntegrationRepository integrations,
    IHttpClientFactory httpClientFactory) : IGoogleDataService
{
    public async Task<GoogleRankingsResponse> GetRankingsAsync(
        Guid userId,
        Guid projectId,
        DateOnly? startDate,
        DateOnly? endDate,
        int? rowLimit,
        CancellationToken ct = default)
    {
        var (from, to) = NormalizeRange(startDate, endDate);
        var gscConnection = await integrations.GetGscConnectionAsync(projectId, userId, ct);
        if (!gscConnection.IsSuccess)
            throw new GoogleIntegrationException(gscConnection.Error ?? "Failed to load GSC connection.");
        if (gscConnection.Value is null)
            throw new GoogleIntegrationException("Google Search Console is not connected for this project.");

        var token = await oauth.GetGscAccessTokenAsync(userId, projectId, ct);
        var siteUrl = gscConnection.Value.SiteUrl;
        IReadOnlyList<GoogleRankingRow> rows;
        try
        {
            rows = await QueryGscRowsAsync(
                token,
                siteUrl,
                from,
                to,
                Math.Clamp(rowLimit ?? 250, 1, 1000),
                ct);
        }
        catch (GoogleIntegrationException ex) when (ex.Message.Contains("(403)", StringComparison.Ordinal))
        {
            var syncedSiteUrl = await oauth.SyncGscSiteUrlAsync(userId, projectId, ct);
            if (string.IsNullOrWhiteSpace(syncedSiteUrl)
                || string.Equals(syncedSiteUrl, siteUrl, StringComparison.Ordinal))
            {
                throw;
            }

            siteUrl = syncedSiteUrl;
            token = await oauth.GetGscAccessTokenAsync(userId, projectId, ct);
            rows = await QueryGscRowsAsync(
                token,
                siteUrl,
                from,
                to,
                Math.Clamp(rowLimit ?? 250, 1, 1000),
                ct);
        }

        return new GoogleRankingsResponse
        {
            ProjectId = projectId,
            SiteUrl = siteUrl,
            StartDate = from.ToString("yyyy-MM-dd"),
            EndDate = to.ToString("yyyy-MM-dd"),
            Rows = rows,
        };
    }

    public async Task<Ga4LandingPagesResponse> GetGa4LandingPagesAsync(
        Guid userId,
        Guid projectId,
        DateOnly? startDate,
        DateOnly? endDate,
        int? limit,
        CancellationToken ct = default)
    {
        var (from, to) = NormalizeRange(startDate, endDate);
        var (token, propertyId) = await oauth.GetGa4AccessTokenAsync(userId, projectId, ct);
        var rows = await QueryGa4LandingPagesAsync(token, propertyId, from, to, Math.Clamp(limit ?? 100, 1, 500), ct);

        return new Ga4LandingPagesResponse
        {
            ProjectId = projectId,
            PropertyId = propertyId,
            StartDate = from.ToString("yyyy-MM-dd"),
            EndDate = to.ToString("yyyy-MM-dd"),
            Rows = rows,
        };
    }

    private async Task<IReadOnlyList<GoogleRankingRow>> QueryGscRowsAsync(
        string accessToken,
        string siteUrl,
        DateOnly startDate,
        DateOnly endDate,
        int rowLimit,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("GoogleApis");
        var encodedSite = Uri.EscapeDataString(siteUrl);
        var endpoint = $"https://www.googleapis.com/webmasters/v3/sites/{encodedSite}/searchAnalytics/query";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                startDate = startDate.ToString("yyyy-MM-dd"),
                endDate = endDate.ToString("yyyy-MM-dd"),
                dimensions = new[] { "query", "page" },
                rowLimit,
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            // #region agent log
            WriteDebugLog(new
            {
                sessionId = "c1ee28",
                runId = "gsc-403",
                hypothesisId = "H1-site-url-format",
                location = "GoogleDataService.cs:QueryGscRowsAsync",
                message = "GSC searchAnalytics non-success",
                data = new
                {
                    siteUrl,
                    statusCode = (int)response.StatusCode,
                    endpoint,
                    rawPreview = raw.Length > 240 ? raw[..240] : raw,
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });
            // #endregion
            var detail = FormatGoogleApiError(raw);
            throw new GoogleIntegrationException(
                $"GSC rankings request failed ({(int)response.StatusCode}) for site {siteUrl}.{detail}",
                StatusCodes.Status502BadGateway);
        }

        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
            return [];

        var results = new List<GoogleRankingRow>();
        foreach (var row in rows.EnumerateArray())
        {
            var keys = row.TryGetProperty("keys", out var rowKeys) && rowKeys.ValueKind == JsonValueKind.Array
                ? rowKeys.EnumerateArray().Select(k => k.GetString() ?? string.Empty).ToArray()
                : [];
            results.Add(new GoogleRankingRow
            {
                Query = keys.ElementAtOrDefault(0) ?? string.Empty,
                Page = keys.ElementAtOrDefault(1) ?? string.Empty,
                Impressions = row.TryGetProperty("impressions", out var imp) ? (long)imp.GetDouble() : 0,
                Clicks = row.TryGetProperty("clicks", out var clk) ? (long)clk.GetDouble() : 0,
                Ctr = row.TryGetProperty("ctr", out var ctr) ? ctr.GetDouble() : 0,
                Position = row.TryGetProperty("position", out var pos) ? pos.GetDouble() : 0,
            });
        }

        return results;
    }

    private async Task<IReadOnlyList<Ga4LandingPageRow>> QueryGa4LandingPagesAsync(
        string accessToken,
        string propertyId,
        DateOnly startDate,
        DateOnly endDate,
        int limit,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("GoogleApis");
        var endpoint = $"https://analyticsdata.googleapis.com/v1beta/properties/{propertyId}:runReport";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                dateRanges = new[]
                {
                    new
                    {
                        startDate = startDate.ToString("yyyy-MM-dd"),
                        endDate = endDate.ToString("yyyy-MM-dd"),
                    }
                },
                dimensions = new[] { new { name = "landingPage" } },
                metrics = new[]
                {
                    new { name = "sessions" },
                    new { name = "totalUsers" },
                    new { name = "conversions" }
                },
                limit = limit.ToString(),
                orderBys = new[]
                {
                    new
                    {
                        metric = new { metricName = "sessions" },
                        desc = true
                    }
                }
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await client.SendAsync(request, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new GoogleIntegrationException($"GA4 landing pages request failed ({(int)response.StatusCode}).", StatusCodes.Status502BadGateway);

        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<Ga4LandingPageRow>();
        foreach (var row in rows.EnumerateArray())
        {
            var dimension = row.GetProperty("dimensionValues")[0].GetProperty("value").GetString() ?? "/";
            var metricValues = row.GetProperty("metricValues").EnumerateArray().ToArray();
            result.Add(new Ga4LandingPageRow
            {
                LandingPage = dimension,
                Sessions = ParseLong(metricValues, 0),
                Users = ParseLong(metricValues, 1),
                Conversions = ParseDouble(metricValues, 2),
            });
        }

        return result;
    }

    private static (DateOnly Start, DateOnly End) NormalizeRange(DateOnly? startDate, DateOnly? endDate)
    {
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = startDate ?? end.AddDays(-28);
        if (start > end)
            throw new GoogleIntegrationException("startDate cannot be after endDate.");
        return (start, end);
    }

    private static long ParseLong(IReadOnlyList<JsonElement> values, int index)
    {
        if (index < 0 || index >= values.Count)
            return 0;
        var value = values[index];
        if (!value.TryGetProperty("value", out var rawValue))
            return 0;
        var raw = rawValue.GetString();
        return long.TryParse(raw, out var parsed) ? parsed : 0;
    }

    private static double ParseDouble(IReadOnlyList<JsonElement> values, int index)
    {
        if (index < 0 || index >= values.Count)
            return 0;
        var value = values[index];
        if (!value.TryGetProperty("value", out var rawValue))
            return 0;
        var raw = rawValue.GetString();
        return double.TryParse(raw, out var parsed) ? parsed : 0;
    }

    private static string FormatGoogleApiError(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message))
            {
                var text = message.GetString();
                return string.IsNullOrWhiteSpace(text) ? string.Empty : $" Google says: {text}";
            }
        }
        catch
        {
            // Ignore malformed error payloads.
        }

        return $" Body: {raw[..Math.Min(raw.Length, 160)]}";
    }

    private static void WriteDebugLog(object payload)
    {
        try
        {
            var line = JsonSerializer.Serialize(payload) + Environment.NewLine;
            File.AppendAllText(
                "/Users/jeffmartin/Library/Mobile Documents/com~apple~CloudDocs/development-new/Geek-SEO/.cursor/debug-c1ee28.log",
                line);
        }
        catch
        {
            // Debug logging must never break API responses.
        }
    }
}

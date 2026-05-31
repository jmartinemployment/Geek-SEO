using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Services;

public sealed class GeoVisibilityService(
    ISerpProvider serp,
    IProjectRepository projects,
    IGeoTrackingRepository geoTracking)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public GeoPlatformsResponse GetPlatformStatus()
    {
        var dataForSeoConfigured = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DATAFORSEO_LOGIN"))
            && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DATAFORSEO_PASSWORD"));

        var anthropicConfigured = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));

        return new GeoPlatformsResponse
        {
            Platforms =
            [
                new GeoPlatformStatus
                {
                    Id = "google_aio",
                    Name = "Google AI Overview",
                    Configured = dataForSeoConfigured,
                    Provider = dataForSeoConfigured ? "dataforseo" : null,
                    Note = dataForSeoConfigured
                        ? "Daily probe via DataForSEO when queries are tracked"
                        : "Set DATAFORSEO_LOGIN and DATAFORSEO_PASSWORD",
                },
                new GeoPlatformStatus
                {
                    Id = "google_organic",
                    Name = "Google Organic",
                    Configured = dataForSeoConfigured,
                    Provider = dataForSeoConfigured ? "dataforseo" : null,
                },
                new GeoPlatformStatus
                {
                    Id = "chatgpt",
                    Name = "ChatGPT",
                    Configured = false,
                    Note = "Provider API key not configured",
                },
                new GeoPlatformStatus
                {
                    Id = "gemini",
                    Name = "Gemini",
                    Configured = false,
                    Note = "Provider API key not configured",
                },
                new GeoPlatformStatus
                {
                    Id = "perplexity",
                    Name = "Perplexity",
                    Configured = false,
                    Note = "Provider API key not configured",
                },
                new GeoPlatformStatus
                {
                    Id = "claude",
                    Name = "Claude",
                    Configured = anthropicConfigured,
                    Provider = anthropicConfigured ? "anthropic" : null,
                    Note = anthropicConfigured
                        ? "Scoring uses Anthropic; mention probe uses organic/AIO only today"
                        : "Set ANTHROPIC_API_KEY",
                },
            ],
        };
    }

    public async Task<IReadOnlyList<GeoTrackingQueryDto>> ListQueriesAsync(
        Guid userId,
        Guid projectId,
        CancellationToken ct = default)
    {
        await EnsureProjectAsync(userId, projectId, ct);
        var result = await geoTracking.ListByProjectAsync(projectId, ct);
        if (!result.IsSuccess || result.Value is null)
            return [];

        return result.Value.Select(q => new GeoTrackingQueryDto
        {
            Id = q.Id,
            ProjectId = q.ProjectId,
            QueryText = q.QueryText,
            Platforms = DeserializePlatforms(q.PlatformsJson),
            Enabled = q.Enabled,
        }).ToList();
    }

    public async Task<GeoTrackingQueryDto> CreateQueryAsync(
        Guid userId,
        CreateGeoTrackingQueryRequest request,
        CancellationToken ct = default)
    {
        await EnsureProjectAsync(userId, request.ProjectId, ct);
        var entity = new SeoGeoTrackingQuery
        {
            ProjectId = request.ProjectId,
            QueryText = request.QueryText.Trim(),
            PlatformsJson = JsonSerializer.Serialize(request.Platforms, JsonOptions),
            Enabled = true,
        };

        var saved = await geoTracking.CreateAsync(entity, ct);
        if (!saved.IsSuccess || saved.Value is null)
            throw new InvalidOperationException(saved.Error ?? "Failed to create query");

        return new GeoTrackingQueryDto
        {
            Id = saved.Value.Id,
            ProjectId = saved.Value.ProjectId,
            QueryText = saved.Value.QueryText,
            Platforms = request.Platforms,
            Enabled = saved.Value.Enabled,
        };
    }

    public async Task DeleteQueryAsync(Guid userId, Guid queryId, CancellationToken ct = default)
    {
        _ = userId;
        var result = await geoTracking.DeleteAsync(queryId, ct);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Failed to delete query");
    }

    public async Task<GeoTrendsResponse> GetTrendsAsync(Guid userId, Guid queryId, CancellationToken ct = default)
    {
        _ = userId;
        var queryResult = await geoTracking.GetQueryAsync(queryId, ct);
        if (!queryResult.IsSuccess || queryResult.Value is null)
            throw new InvalidOperationException("Query not found");

        var snapshotsResult = await geoTracking.ListSnapshotsAsync(queryId, 30, ct);
        if (!snapshotsResult.IsSuccess || snapshotsResult.Value is null)
            throw new InvalidOperationException(snapshotsResult.Error ?? "Failed to load snapshots");

        var snapshots = snapshotsResult.Value;
        var points = snapshots.Select(s => new GeoTrendPoint
        {
            Date = s.CheckedAt.ToString("yyyy-MM-dd"),
            Platform = s.Platform,
            Mentioned = s.Mentioned,
        }).ToList();

        var mentionRate = snapshots.Count > 0
            ? snapshots.Count(s => s.Mentioned) / (double)snapshots.Count
            : 0;

        var queryText = queryResult.Value.QueryText;

        return new GeoTrendsResponse
        {
            QueryId = queryId,
            QueryText = queryText,
            Points = points,
            MentionRate30d = Math.Round(mentionRate * 100, 1),
        };
    }

    public async Task<GeoProbeResult> ProbeGoogleAioAsync(
        Guid userId,
        GeoProbeRequest request,
        CancellationToken ct = default)
    {
        var result = await ProbeInternalAsync(userId, request, ct);
        await PersistProbeAsync(request.ProjectId, request.Query, "google_aio", result.Mentioned, result.Snippet, ct);
        return result;
    }

    public async Task ProbeTrackedQueryAsync(GeoProbeCandidate candidate, CancellationToken ct = default)
    {
        var request = new GeoProbeRequest
        {
            ProjectId = candidate.ProjectId,
            Query = candidate.QueryText,
        };

        var result = await ProbeInternalAsync(candidate.UserId, request, ct);
        await PersistProbeAsync(candidate.ProjectId, candidate.QueryText, "google_aio", result.Mentioned, result.Snippet, ct);
    }

    private async Task<GeoProbeResult> ProbeInternalAsync(
        Guid userId,
        GeoProbeRequest request,
        CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(request.ProjectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            throw new InvalidOperationException("Project not found");

        var domain = ExtractDomain(project.Value.Url);
        var serpResult = await serp.GetSerpResultsAsync(new SerpRequest
        {
            Keyword = request.Query.Trim(),
            Location = request.Location,
            ResultCount = 20,
        }, ct);

        if (!serpResult.IsSuccess || serpResult.Value is null)
        {
            return new GeoProbeResult
            {
                ProjectId = request.ProjectId,
                Query = request.Query,
                Platform = "google_aio",
                Mentioned = false,
                HasAiOverview = false,
                CheckedAt = DateTimeOffset.UtcNow.ToString("O"),
                Note = serpResult.Error ?? "SERP probe failed",
            };
        }

        var serpData = serpResult.Value;
        var organicMatch = serpData.OrganicResults
            .FirstOrDefault(o => DomainMatches(domain, o.Domain) || DomainMatches(domain, o.Url));

        var mentionedInOrganic = organicMatch is not null;
        var hasAio = serpData.Features.HasAiOverview;

        return new GeoProbeResult
        {
            ProjectId = request.ProjectId,
            Query = request.Query,
            Platform = "google_aio",
            Mentioned = mentionedInOrganic || hasAio,
            HasAiOverview = hasAio,
            OrganicPosition = organicMatch?.Position,
            Snippet = organicMatch?.Snippet,
            CheckedAt = DateTimeOffset.UtcNow.ToString("O"),
            Note = hasAio
                ? mentionedInOrganic
                    ? "Your domain ranks organically; AI Overview is present for this query."
                    : "AI Overview present — verify brand mentions manually in live SERP."
                : mentionedInOrganic
                    ? "Ranked organically; no AI Overview detected for this query."
                    : "Not visible in top organic results; no AI Overview detected.",
        };
    }

    private async Task PersistProbeAsync(
        Guid projectId,
        string queryText,
        string platform,
        bool mentioned,
        string? snippet,
        CancellationToken ct)
    {
        var queries = await geoTracking.ListByProjectAsync(projectId, ct);
        if (!queries.IsSuccess || queries.Value is null)
            return;

        var match = queries.Value.FirstOrDefault(q =>
            string.Equals(q.QueryText, queryText, StringComparison.OrdinalIgnoreCase));
        if (match is null)
            return;

        await geoTracking.AddSnapshotAsync(new SeoGeoMentionSnapshot
        {
            QueryId = match.Id,
            Platform = platform,
            Mentioned = mentioned,
            Snippet = snippet,
            CheckedAt = DateTimeOffset.UtcNow,
        }, ct);
    }

    private async Task EnsureProjectAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(projectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            throw new InvalidOperationException("Project not found");
    }

    private static IReadOnlyList<string> DeserializePlatforms(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? ["google_aio"];
        }
        catch (JsonException)
        {
            return ["google_aio"];
        }
    }

    private static string ExtractDomain(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.Trim().ToLowerInvariant();

        return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? uri.Host[4..].ToLowerInvariant()
            : uri.Host.ToLowerInvariant();
    }

    private static bool DomainMatches(string projectDomain, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        return candidate.Contains(projectDomain, StringComparison.OrdinalIgnoreCase);
    }
}

using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services;

public sealed class GeoVisibilityService(ISerpProvider serp, IProjectRepository projects)
{
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
                        ? "On-demand SERP probe via DataForSEO"
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
                    Note = "Daily probe worker not configured — API key required",
                },
                new GeoPlatformStatus
                {
                    Id = "gemini",
                    Name = "Gemini",
                    Configured = false,
                    Note = "Daily probe worker not configured",
                },
                new GeoPlatformStatus
                {
                    Id = "perplexity",
                    Name = "Perplexity",
                    Configured = false,
                    Note = "Daily probe worker not configured",
                },
                new GeoPlatformStatus
                {
                    Id = "claude",
                    Name = "Claude",
                    Configured = anthropicConfigured,
                    Provider = anthropicConfigured ? "anthropic" : null,
                    Note = anthropicConfigured
                        ? "Scoring uses Anthropic; GEO mention probe worker not yet wired"
                        : "Set ANTHROPIC_API_KEY",
                },
            ],
        };
    }

    public async Task<GeoProbeResult> ProbeGoogleAioAsync(
        Guid userId,
        GeoProbeRequest request,
        CancellationToken ct = default)
    {
        _ = userId;
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

        var normalized = candidate.ToLowerInvariant();
        return normalized.Contains(projectDomain, StringComparison.OrdinalIgnoreCase);
    }
}

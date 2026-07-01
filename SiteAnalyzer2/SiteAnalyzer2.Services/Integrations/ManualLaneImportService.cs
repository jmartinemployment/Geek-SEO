using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Serp;
using SiteAnalyzer2.Serp.Models;
using SiteAnalyzer2.Services.Pipeline;

namespace SiteAnalyzer2.Services.Integrations;

public sealed class ManualLaneImportService(
    AppDbContext db,
    SerpHtmlImportService htmlImport)
{
    public async Task<ManualLaneImportResultDto> ImportLaneAsync(
        Guid runId,
        string html,
        string? lane,
        string topicSlug,
        CancellationToken ct = default)
    {
        if (runId == Guid.Empty)
            throw new InvalidOperationException("runId is required.");

        if (string.IsNullOrWhiteSpace(html))
            throw new InvalidOperationException("Request body must contain saved Google SERP HTML.");

        if (string.IsNullOrWhiteSpace(topicSlug))
            throw new InvalidOperationException("topic query parameter is required.");

        var normalizedLane = SerpResearchLanes.Normalize(lane);
        var normalizedTopic = topicSlug.Trim().ToLowerInvariant();

        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException("Analysis run not found.");

        await EnforceTopicInvariantAsync(run, normalizedTopic, ct);

        if (!GoogleSerpHtmlParser.LooksLikeSerpPage(html))
        {
            throw new InvalidOperationException(
                "Uploaded HTML does not look like a Google SERP page. Save as 'Webpage, HTML only' from Chrome.");
        }

        var parsed = GoogleSerpHtmlParser.ParseLivePage(html, keywordOverride: run.Keyword);
        ValidateParsedLane(normalizedLane, parsed);

        run.TopicSlug = normalizedTopic;
        run.ResearchMode = ResearchModes.Manual;
        if (string.IsNullOrWhiteSpace(run.Keyword) && !string.IsNullOrWhiteSpace(parsed.Keyword))
            run.Keyword = parsed.Keyword.Trim();

        await htmlImport.ClearSerpDataForLaneAsync(run.Id, normalizedLane, ct);
        var outcome = await htmlImport.PersistParsedPageForLaneAsync(run, parsed, normalizedLane, ct);

        return new ManualLaneImportResultDto
        {
            RunId = run.Id,
            Lane = normalizedLane,
            TopicSlug = normalizedTopic,
            OrganicCount = outcome.Counts.OrganicCount,
            CitationEligibleCount = CountCitationEligible(parsed, normalizedLane),
            ResearchMode = run.ResearchMode,
        };
    }

    private async Task EnforceTopicInvariantAsync(AnalysisRun run, string topicSlug, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(run.TopicSlug)
            && !string.Equals(run.TopicSlug, topicSlug, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"topic_slug '{topicSlug}' does not match existing topic '{run.TopicSlug}' for this run.");
        }

        var storedTopic = await db.SerpItems.AsNoTracking()
            .Where(i => i.RunId == run.Id && i.ResearchLane != null)
            .Select(i => run.TopicSlug)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrWhiteSpace(storedTopic)
            && !string.Equals(storedTopic, topicSlug, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"topic_slug '{topicSlug}' does not match existing topic '{storedTopic}' for this run.");
        }
    }

    internal static void ValidateParsedLane(string normalizedLane, SerpLivePageParseResult parsed)
    {
        var organicCount = parsed.Items.Count(i =>
            string.Equals(i.Type, SerpItemTypes.Organic, StringComparison.OrdinalIgnoreCase) && !i.Ads);

        if (string.Equals(normalizedLane, SerpResearchLanes.Keyword, StringComparison.OrdinalIgnoreCase))
        {
            if (organicCount == 0)
            {
                throw new InvalidOperationException(
                    "Keyword import produced 0 organic results — fix HTML or parser before retrying.");
            }

            return;
        }

        if (string.Equals(normalizedLane, SerpResearchLanes.Local, StringComparison.OrdinalIgnoreCase))
        {
            var localSignals = organicCount + (parsed.LocalPackPresent ? 1 : 0);
            if (localSignals == 0)
            {
                throw new InvalidOperationException(
                    "Local lane import produced 0 usable local-pack or organic results.");
            }

            return;
        }

        var eligible = CountCitationEligible(parsed, normalizedLane);
        if (eligible == 0)
        {
            throw new InvalidOperationException(
                $"Lane '{normalizedLane}' produced 0 citation-eligible URLs after domain validation.");
        }
    }

    internal static int CountCitationEligible(SerpLivePageParseResult parsed, string normalizedLane)
    {
        var count = 0;
        foreach (var item in parsed.Items)
        {
            if (!string.Equals(item.Type, SerpItemTypes.Organic, StringComparison.OrdinalIgnoreCase) || item.Ads)
                continue;

            if (string.IsNullOrWhiteSpace(item.Url))
                continue;

            if (ManualCitationDomainRules.IsEligibleForLane(item.Url, normalizedLane))
                count++;
        }

        return count;
    }
}

public sealed record ManualLaneImportResultDto
{
    public required Guid RunId { get; init; }
    public required string Lane { get; init; }
    public required string TopicSlug { get; init; }
    public int OrganicCount { get; init; }
    public int CitationEligibleCount { get; init; }
    public string ResearchMode { get; init; } = ResearchModes.Manual;
}

internal static class ManualCitationDomainRules
{
    internal static bool IsEligibleForLane(string url, string lane)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.ToLowerInvariant();
        return lane.ToLowerInvariant() switch
        {
            SerpResearchLanes.Gov => host.EndsWith(".gov", StringComparison.Ordinal),
            SerpResearchLanes.Edu => host.EndsWith(".edu", StringComparison.Ordinal),
            SerpResearchLanes.Wiki => host.Contains("wikipedia.org", StringComparison.Ordinal),
            _ => true,
        };
    }
}

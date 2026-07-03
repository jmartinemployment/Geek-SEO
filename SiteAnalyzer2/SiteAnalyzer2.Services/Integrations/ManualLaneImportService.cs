using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
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
        var normalizedTopic = ResearchTopicSlug.Normalize(topicSlug);

        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException("Analysis run not found.");

        EnforceTopicInvariant(run, normalizedTopic);

        var parsed = ParseLaneContent(html, normalizedLane, run.Keyword);
        ValidateParsedLane(normalizedLane, parsed, run.Keyword);

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
            PaaQuestionCount = string.Equals(normalizedLane, SerpResearchLanes.Paa, StringComparison.OrdinalIgnoreCase)
                ? CountPaaQuestions(parsed)
                : 0,
        };
    }

    private static SerpLivePageParseResult ParseLaneContent(string content, string normalizedLane, string keyword)
    {
        if (string.Equals(normalizedLane, SerpResearchLanes.Paa, StringComparison.OrdinalIgnoreCase))
        {
            var paaParsed = PaaLaneContentParser.Parse(content, keyword);
            return PaaLaneImportComposer.ApplyKeywordRelevance(paaParsed, keyword);
        }

        if (!GoogleSerpHtmlParser.LooksLikeSerpPage(content))
        {
            throw new InvalidOperationException(
                "Uploaded file does not look like a Google SERP page. Save as 'Webpage, HTML only' from Chrome.");
        }

        var parsed = GoogleSerpHtmlParser.ParseLivePage(content, keywordOverride: keyword);
        return CitationLaneHtmlFallback.Enrich(parsed, content, normalizedLane);
    }

    public async Task<ManualLaneImportResultDto> ImportPaaBatchAsync(
        Guid runId,
        IReadOnlyList<PaaLaneImportFile> files,
        string topicSlug,
        CancellationToken ct = default)
    {
        if (runId == Guid.Empty)
            throw new InvalidOperationException("runId is required.");

        if (files is not { Count: > 0 })
            throw new InvalidOperationException("At least one PAA file is required.");

        if (string.IsNullOrWhiteSpace(topicSlug))
            throw new InvalidOperationException("topic query parameter is required.");

        var normalizedTopic = ResearchTopicSlug.Normalize(topicSlug);

        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException("Analysis run not found.");

        EnforceTopicInvariant(run, normalizedTopic);

        var merged = PaaLaneImportComposer.MergeContents(files, run.Keyword);
        ValidateParsedLane(SerpResearchLanes.Paa, merged, run.Keyword);

        run.TopicSlug = normalizedTopic;
        run.ResearchMode = ResearchModes.Manual;
        if (string.IsNullOrWhiteSpace(run.Keyword) && !string.IsNullOrWhiteSpace(merged.Keyword))
            run.Keyword = merged.Keyword.Trim();

        await htmlImport.ClearSerpDataForLaneAsync(run.Id, SerpResearchLanes.Paa, ct);
        var outcome = await htmlImport.PersistParsedPageForLaneAsync(run, merged, SerpResearchLanes.Paa, ct);

        return new ManualLaneImportResultDto
        {
            RunId = run.Id,
            Lane = SerpResearchLanes.Paa,
            TopicSlug = normalizedTopic,
            OrganicCount = outcome.Counts.OrganicCount,
            CitationEligibleCount = CountCitationEligible(merged, SerpResearchLanes.Paa),
            ResearchMode = run.ResearchMode,
            FileCount = files.Count,
            PaaQuestionCount = CountPaaQuestions(merged),
        };
    }

    private static void EnforceTopicInvariant(AnalysisRun run, string topicSlug)
    {
        if (string.IsNullOrWhiteSpace(run.TopicSlug))
            return;

        if (string.Equals(run.TopicSlug, topicSlug, StringComparison.OrdinalIgnoreCase))
            return;

        var hint = topicSlug.Contains(' ', StringComparison.Ordinal)
            ? $" Use the research folder slug stored on this run ('{run.TopicSlug}'), not the pillar keyword phrase."
            : string.Empty;

        throw new InvalidOperationException(
            $"topic_slug '{topicSlug}' does not match existing topic '{run.TopicSlug}' for this run.{hint}");
    }

    internal static void ValidateParsedLane(string normalizedLane, SerpLivePageParseResult parsed, string? keyword = null)
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

        if (string.Equals(normalizedLane, SerpResearchLanes.Paa, StringComparison.OrdinalIgnoreCase))
        {
            if (CountPaaQuestions(parsed) == 0)
            {
                throw new InvalidOperationException(
                    "PAA lane import produced 0 People Also Ask questions — save a SERP with PAA expanded.");
            }

            return;
        }

        var eligible = CountCitationEligible(parsed, normalizedLane);
        if (eligible == 0)
        {
            var domains = parsed.Items
                .Where(i => string.Equals(i.Type, SerpItemTypes.Organic, StringComparison.OrdinalIgnoreCase) && !i.Ads)
                .Select(i => i.Domain)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList();

            var domainHint = domains.Count > 0
                ? $" Parsed {organicCount} organic result(s); domains seen: {string.Join(", ", domains)}."
                : $" Parsed {organicCount} organic result(s).";

            var wrongDomainHint = CitationLaneValidationMessages.WrongDomainHint(normalizedLane, domains) ?? "";
            var queryHint = CitationLaneQueryHints.ForLane(normalizedLane, keyword);
            throw new InvalidOperationException(
                $"Lane '{normalizedLane}' produced 0 citation-eligible URLs after domain validation.{domainHint}{wrongDomainHint}{queryHint}");
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

            if (CitationLaneDomainRules.IsEligibleUrl(item.Url, normalizedLane))
                count++;
        }

        return count;
    }

    public async Task<string> UpdateTopicSlugAsync(Guid runId, string topicSlug, CancellationToken ct = default)
    {
        if (runId == Guid.Empty)
            throw new InvalidOperationException("runId is required.");

        if (string.IsNullOrWhiteSpace(topicSlug))
            throw new InvalidOperationException("topic slug is required.");

        var normalized = ResearchTopicSlug.Normalize(topicSlug);

        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException("Analysis run not found.");

        if (await HasSupplementalLaneImportsAsync(runId, ct))
        {
            throw new InvalidOperationException(
                "Topic slug cannot change after supplemental lanes (gov, wiki, paa, etc.) are imported. Use Start new keyword.");
        }

        run.TopicSlug = normalized;
        await db.SaveChangesAsync(ct);
        return normalized;
    }

    private async Task<bool> HasSupplementalLaneImportsAsync(Guid runId, CancellationToken ct) =>
        await db.SerpItems.AsNoTracking()
            .AnyAsync(
                i => i.RunId == runId && !string.IsNullOrWhiteSpace(i.ResearchLane),
                ct);

    internal static int CountPaaQuestions(SerpLivePageParseResult parsed) =>
        parsed.Items
            .Where(i => string.Equals(i.Type, SerpItemTypes.PeopleAlsoAsk, StringComparison.OrdinalIgnoreCase)
                || i.RelatedQueries?.Count > 0)
            .SelectMany(i => i.RelatedQueries ?? [])
            .Count(q => !string.IsNullOrWhiteSpace(q.QueryText)
                && (q.QueryType == SerpRelatedQueryType.PeopleAlsoAsk
                    || q.QueryText.Contains('?', StringComparison.Ordinal)));
}

public sealed record ManualLaneImportResultDto
{
    public required Guid RunId { get; init; }
    public required string Lane { get; init; }
    public required string TopicSlug { get; init; }
    public int OrganicCount { get; init; }
    public int CitationEligibleCount { get; init; }
    public string ResearchMode { get; init; } = ResearchModes.Manual;
    public int FileCount { get; init; } = 1;
    public int PaaQuestionCount { get; init; }
}

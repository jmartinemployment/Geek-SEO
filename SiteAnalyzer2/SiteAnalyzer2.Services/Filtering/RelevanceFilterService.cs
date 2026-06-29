using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.Parsing;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.Filtering;

public class RelevanceFilterService(AppDbContext db)
{
    public async Task<int> RunFilterStageAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        var serpItems = await db.SerpItems
            .Where(i => i.RunId == runId && i.Type == SerpItemTypes.Organic && !i.Ads)
            .ToListAsync(ct);

        var referenceDomains = await db.ReferenceExcludeDomains.ToListAsync(ct);
        var knownPlatforms = await db.KnownPlatformDomains.ToListAsync(ct);
        var ownedDomains = await db.ProjectOwnedDomains.Where(d => d.ProjectId == run.ProjectId).ToListAsync(ct);
        var competitorSeeds = await db.CompetitorSeedDomains.Where(d => d.ProjectId == run.ProjectId).ToListAsync(ct);

        await ApplyFilterAsync(run, serpItems, referenceDomains, knownPlatforms, ownedDomains, competitorSeeds, ct);
        await db.SaveChangesAsync(ct);
        return serpItems.Count;
    }

    public Task ApplyFilterAsync(
        AnalysisRun run,
        IReadOnlyList<SerpItem> serpItems,
        IReadOnlyList<ReferenceExcludeDomain> referenceDomains,
        IReadOnlyList<KnownPlatformDomain> knownPlatforms,
        IReadOnlyList<ProjectOwnedDomain> ownedDomains,
        IReadOnlyList<CompetitorSeedDomain> competitorSeeds,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var targetDomain = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(run.TargetSiteUrl));
        var referenceSet = referenceDomains.Select(d => d.Domain.ToLowerInvariant()).ToHashSet();
        var platformSet = knownPlatforms.Select(d => d.Domain.ToLowerInvariant()).ToHashSet();
        var ownedSet = ownedDomains.Select(d => d.Domain.ToLowerInvariant()).ToHashSet();
        var seedSet = competitorSeeds.Select(d => d.Domain.ToLowerInvariant()).ToHashSet();

        var includedByDomain = new Dictionary<string, (IncludeReason Reason, string TriggerUrl)>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in serpItems)
        {
            var host = DomainHelper.GetHostFromUrl(item.Url ?? "");
            var registrable = DomainHelper.GetRegistrableDomain(host).ToLowerInvariant();

            var disposition = TryBucket1(run, item, host, registrable, referenceSet, ownedSet, targetDomain)
                              ?? TryBucket2(item, registrable, platformSet)
                              ?? TryBucket3Initial(item, host, registrable, seedSet, targetDomain, ownedSet)
                              ?? (FilterStatus.PendingReview, null, "Ambiguous relevance; requires manual review.");

            ApplyDisposition(item, disposition.Status, disposition.IncludeReason, disposition.ExcludeReason);

            if (item.FilterStatus == FilterStatus.Included && item.IncludeReason.HasValue)
                includedByDomain.TryAdd(registrable, (item.IncludeReason.Value, item.Url ?? ""));
        }

        ApplyCascadeRules(serpItems, run, includedByDomain, targetDomain, ownedSet);
        ApplyKeywordRelevanceRejections(serpItems, run.Keyword);
        return Task.CompletedTask;
    }

    private static void ApplyKeywordRelevanceRejections(IReadOnlyList<SerpItem> serpItems, string keyword)
    {
        foreach (var item in serpItems)
        {
            if (item.FilterStatus is FilterStatus.Excluded or FilterStatus.Rejected)
                continue;

            if (KeywordPathMatcher.ContainsAnyKeywordToken(keyword, item.Url, item.Title, item.Description))
                continue;

            ApplyDisposition(
                item,
                FilterStatus.Rejected,
                null,
                "No pillar keyword word found in URL, title, or snippet.");
        }
    }

    private static void ApplyDisposition(SerpItem item, FilterStatus status, IncludeReason? includeReason, string? excludeReason)
    {
        item.FilterStatus = status;
        item.IncludeReason = status == FilterStatus.Included ? includeReason : null;
        item.ExcludeReason = status is FilterStatus.Excluded or FilterStatus.Rejected ? excludeReason : null;
        item.Filtered = status is FilterStatus.Excluded or FilterStatus.Rejected;
    }

    private static (FilterStatus Status, IncludeReason? IncludeReason, string? ExcludeReason)? TryBucket1(
        AnalysisRun run,
        SerpItem item,
        string host,
        string registrable,
        HashSet<string> referenceSet,
        HashSet<string> ownedSet,
        string targetDomain)
    {
        if (ownedSet.Contains(registrable) || DomainHelper.HostsMatch(registrable, targetDomain))
            return (FilterStatus.Excluded, null, "Project-owned or target domain.");

        if (run.IncludeReferenceDomains)
            return null;

        if (referenceSet.Contains(registrable) || (item.Url ?? "").Contains("/wiki/", StringComparison.OrdinalIgnoreCase))
            return (FilterStatus.Excluded, null, "Reference or informational domain.");

        if (host.EndsWith(".gov", StringComparison.OrdinalIgnoreCase))
            return (FilterStatus.Excluded, null, "Informational .gov page.");

        return null;
    }

    private static (FilterStatus Status, IncludeReason? IncludeReason, string? ExcludeReason)? TryBucket2(
        SerpItem item,
        string registrable,
        HashSet<string> platformSet)
    {
        if (!platformSet.Contains(registrable))
            return null;

        return (FilterStatus.Included, IncludeReason.KnownPlatform, "Known platform domain.");
    }

    private static (FilterStatus Status, IncludeReason? IncludeReason, string? ExcludeReason)? TryBucket3Initial(
        SerpItem item,
        string host,
        string registrable,
        HashSet<string> seedSet,
        string targetDomain,
        HashSet<string> ownedSet)
    {
        if (DomainHelper.IsNoiseSubdomain(host))
            return null;

        if (seedSet.Contains(registrable))
            return (FilterStatus.Included, IncludeReason.CompetitorSeed, "Competitor seed domain.");

        var title = item.Title ?? "";
        var snippet = item.Description ?? "";
        if (PageExtractionService.HasCommercialLanguage(title, snippet))
            return (FilterStatus.Included, IncludeReason.CommercialIntent, "Commercial intent in title or snippet.");

        return null;
    }

    private static void ApplyCascadeRules(
        IReadOnlyList<SerpItem> serpItems,
        AnalysisRun run,
        Dictionary<string, (IncludeReason Reason, string TriggerUrl)> includedByDomain,
        string targetDomain,
        HashSet<string> ownedSet)
    {
        var cascadeEligibleReasons = new HashSet<IncludeReason>
        {
            IncludeReason.KnownPlatform,
            IncludeReason.CompetitorSeed,
            IncludeReason.CommercialIntent
        };

        for (var i = 0; i < serpItems.Count; i++)
        {
            var item = serpItems[i];
            if (item.FilterStatus != FilterStatus.PendingReview)
                continue;

            var host = DomainHelper.GetHostFromUrl(item.Url ?? "");
            var registrable = DomainHelper.GetRegistrableDomain(host).ToLowerInvariant();

            if (DomainHelper.IsNoiseSubdomain(host))
                continue;

            if (ownedSet.Contains(registrable) || DomainHelper.HostsMatch(registrable, targetDomain))
                continue;

            if (!includedByDomain.TryGetValue(registrable, out var trigger))
                continue;

            if (!cascadeEligibleReasons.Contains(trigger.Reason))
                continue;

            ApplyDisposition(
                item,
                FilterStatus.Included,
                IncludeReason.MultiPropertyCascade,
                $"Cascade from {trigger.TriggerUrl}.");
        }
    }
}

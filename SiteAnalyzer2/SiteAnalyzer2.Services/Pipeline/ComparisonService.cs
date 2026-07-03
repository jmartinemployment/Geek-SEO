using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using System.Text.Json;

namespace SiteAnalyzer2.Services.Pipeline;

public class ComparisonService(AppDbContext db)
{
    private const int StructuredDataThreshold = 2;
    private const int DepthThreshold = 3;

    public async Task<(int Checks, int Findings)> RunComparisonStageAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        var pages = await db.Pages.Where(p => p.RunId == runId).ToListAsync(ct);
        var targetPages = pages.Where(p => p.IsTargetSite).ToList();
        var competitorPages = pages.Where(p => !p.IsTargetSite).ToList();
        var targetPageIds = targetPages.Select(p => p.Id).ToHashSet();

        var pageIds = pages.Select(p => p.Id).ToList();

        var headings = await db.PageHeadings.Where(h => pageIds.Contains(h.PageId)).ToListAsync(ct);
        var jsonLd = await db.PageJsonLdBlocks.Where(j => pageIds.Contains(j.PageId)).ToListAsync(ct);
        var blocks = await db.PageContentBlocks.Where(b => pageIds.Contains(b.PageId)).ToListAsync(ct);
        var internalLinks = await db.InternalLinks.Where(l => l.RunId == runId).ToListAsync(ct);
        var crossLinks = await db.CrossRunLinks.Where(l => l.RunId == runId && !l.IsInternalToDomain).ToListAsync(ct);
        var pageRankScores = await db.PageRankScores.Where(s => s.RunId == runId).ToListAsync(ct);

        var checks = new List<ComparisonCheck>();
        var findings = new List<Finding>();

        EvaluateStructuredDataGap(run, targetPages, competitorPages, jsonLd, checks, findings);
        EvaluateHeadingStructureGap(run, targetPages, competitorPages, headings, checks, findings);
        EvaluateContentBlockGap(run, targetPages, competitorPages, blocks, checks, findings);
        EvaluateInternalOrphanPage(run, targetPages, internalLinks, checks, findings);
        EvaluateInternalDepthIssue(run, targetPages, checks, findings);
        EvaluateInternalAuthoritySkew(run, targetPages, pageRankScores, checks, findings);
        EvaluateOutboundLinkSignal(run, targetPages, competitorPages, crossLinks, checks, findings);

        await db.ComparisonChecks.AddRangeAsync(checks, ct);
        await db.Findings.AddRangeAsync(findings, ct);
        await db.SaveChangesAsync(ct);

        return (checks.Count, findings.Count);
    }

    /// <summary>Operator path: compare target pages vs competitor_pages (not legacy SERP pages).</summary>
    public async Task<(int Checks, int Findings)> RunOperatorComparisonAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        var existingChecks = await db.ComparisonChecks.Where(c => c.RunId == runId).ToListAsync(ct);
        var existingFindings = await db.Findings.Where(f => f.RunId == runId).ToListAsync(ct);
        if (existingChecks.Count > 0)
            db.ComparisonChecks.RemoveRange(existingChecks);
        if (existingFindings.Count > 0)
            db.Findings.RemoveRange(existingFindings);

        var targetPages = await db.Pages.Where(p => p.RunId == runId && p.IsTargetSite).ToListAsync(ct);
        var competitorPages = await db.CompetitorPages.Where(p => p.RunId == runId).ToListAsync(ct);
        var checks = new List<ComparisonCheck>();
        var findings = new List<Finding>();

        if (targetPages.Count == 0 || competitorPages.Count == 0)
        {
            checks.Add(new ComparisonCheck
            {
                Id = Guid.NewGuid(),
                ProjectId = run.ProjectId,
                RunId = run.Id,
                FindingType = FindingType.HeadingStructureGap,
                Outcome = ComparisonOutcome.NoFinding,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    skipped = true,
                    reason = targetPages.Count == 0
                        ? "Target-site pages missing during operator comparison."
                        : "Competitor pages missing during operator comparison.",
                }),
            });
            await db.ComparisonChecks.AddRangeAsync(checks, ct);
            await db.SaveChangesAsync(ct);
            return (checks.Count, 0);
        }

        var targetPageIds = targetPages.Select(p => p.Id).ToList();
        var competitorPageIds = competitorPages.Select(p => p.Id).ToList();

        var targetHeadings = await db.PageHeadings.Where(h => targetPageIds.Contains(h.PageId)).ToListAsync(ct);
        var targetJsonLd = await db.PageJsonLdBlocks.Where(j => targetPageIds.Contains(j.PageId)).ToListAsync(ct);
        var targetBlocks = await db.PageContentBlocks.Where(b => targetPageIds.Contains(b.PageId)).ToListAsync(ct);

        var competitorHeadings = await db.CompetitorPageHeadings
            .Where(h => competitorPageIds.Contains(h.CompetitorPageId))
            .ToListAsync(ct);
        var competitorJsonLd = await db.CompetitorPageJsonLdBlocks
            .Where(j => competitorPageIds.Contains(j.CompetitorPageId))
            .ToListAsync(ct);

        EvaluateOperatorStructuredDataGap(run, targetJsonLd, competitorJsonLd, checks, findings);
        EvaluateOperatorHeadingStructureGap(run, targetHeadings, competitorHeadings, checks, findings);
        EvaluateOperatorContentBlockGap(run, targetPages, targetBlocks, competitorJsonLd, checks, findings);

        if (checks.Count > 0)
            await db.ComparisonChecks.AddRangeAsync(checks, ct);
        if (findings.Count > 0)
            await db.Findings.AddRangeAsync(findings, ct);
        await db.SaveChangesAsync(ct);

        return (checks.Count, findings.Count);
    }

    private static void EvaluateOperatorStructuredDataGap(
        AnalysisRun run,
        IReadOnlyList<PageJsonLd> targetJsonLd,
        IReadOnlyList<CompetitorPageJsonLd> competitorJsonLd,
        List<ComparisonCheck> checks,
        List<Finding> findings)
    {
        var competitorTypes = competitorJsonLd
            .Where(j => !string.IsNullOrWhiteSpace(j.ParsedType))
            .GroupBy(j => j.ParsedType!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var targetTypes = targetJsonLd
            .Where(j => !string.IsNullOrWhiteSpace(j.ParsedType))
            .GroupBy(j => j.ParsedType!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var missing = competitorTypes
            .Where(kvp => kvp.Value >= StructuredDataThreshold && !targetTypes.ContainsKey(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToList();

        var payload = JsonSerializer.Serialize(new { missingTypes = missing, threshold = StructuredDataThreshold });
        AddCheck(run, FindingType.StructuredDataGap, missing.Count > 0, payload, checks, findings);
    }

    private static void EvaluateOperatorHeadingStructureGap(
        AnalysisRun run,
        IReadOnlyList<PageHeading> targetHeadings,
        IReadOnlyList<CompetitorPageHeading> competitorHeadings,
        List<ComparisonCheck> checks,
        List<Finding> findings)
    {
        var competitorCounts = competitorHeadings
            .GroupBy(h => h.CompetitorPageId)
            .Select(g => g.Count(h => h.Level >= 2))
            .Where(c => c > 0)
            .ToList();

        var targetCount = targetHeadings.Count(h => h.Level >= 2);
        var median = competitorCounts.Count == 0 ? 0 : competitorCounts.OrderBy(c => c).ElementAt(competitorCounts.Count / 2);
        var hasGap = competitorCounts.Count > 0 && targetCount < median;

        var payload = JsonSerializer.Serialize(new { targetH2ToH6 = targetCount, competitorMedianH2ToH6 = median });
        AddCheck(run, FindingType.HeadingStructureGap, hasGap, payload, checks, findings);
    }

    private static void EvaluateOperatorContentBlockGap(
        AnalysisRun run,
        IReadOnlyList<Page> targetPages,
        IReadOnlyList<PageContentBlock> targetBlocks,
        IReadOnlyList<CompetitorPageJsonLd> competitorJsonLd,
        List<ComparisonCheck> checks,
        List<Finding> findings)
    {
        var competitorHasFaq = competitorJsonLd.Any(j =>
            j.ParsedType != null && j.ParsedType.Contains("FAQ", StringComparison.OrdinalIgnoreCase));
        var targetHasFaq = targetBlocks.Any(b => b.BlockType.Equals("faq", StringComparison.OrdinalIgnoreCase));

        var missing = new List<string>();
        if (competitorHasFaq && !targetHasFaq)
            missing.Add("faq");

        var payload = JsonSerializer.Serialize(new { missingBlockTypes = missing });
        AddCheck(run, FindingType.ContentBlockGap, missing.Count > 0, payload, checks, findings);
    }

    private static void EvaluateStructuredDataGap(
        AnalysisRun run,
        IReadOnlyList<Page> targetPages,
        IReadOnlyList<Page> competitorPages,
        IReadOnlyList<PageJsonLd> jsonLd,
        List<ComparisonCheck> checks,
        List<Finding> findings)
    {
        var competitorTypes = GetSchemaTypes(competitorPages, jsonLd);
        var targetTypes = GetSchemaTypes(targetPages, jsonLd);
        var missing = competitorTypes
            .Where(kvp => kvp.Value >= StructuredDataThreshold && !targetTypes.ContainsKey(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToList();

        var payload = JsonSerializer.Serialize(new { missingTypes = missing, threshold = StructuredDataThreshold });
        AddCheck(run, FindingType.StructuredDataGap, missing.Count > 0, payload, checks, findings);
    }

    private static void EvaluateHeadingStructureGap(
        AnalysisRun run,
        IReadOnlyList<Page> targetPages,
        IReadOnlyList<Page> competitorPages,
        IReadOnlyList<PageHeading> headings,
        List<ComparisonCheck> checks,
        List<Finding> findings)
    {
        var competitorCounts = competitorPages
            .Select(p => headings.Count(h => h.PageId == p.Id && h.Level >= 2))
            .Where(c => c > 0)
            .ToList();

        var targetCount = targetPages.Sum(p => headings.Count(h => h.PageId == p.Id && h.Level >= 2));
        var median = competitorCounts.Count == 0 ? 0 : competitorCounts.OrderBy(c => c).ElementAt(competitorCounts.Count / 2);
        var hasGap = competitorCounts.Count > 0 && targetCount < median;

        var payload = JsonSerializer.Serialize(new { targetH2ToH6 = targetCount, competitorMedianH2ToH6 = median });
        AddCheck(run, FindingType.HeadingStructureGap, hasGap, payload, checks, findings);
    }

    private static void EvaluateContentBlockGap(
        AnalysisRun run,
        IReadOnlyList<Page> targetPages,
        IReadOnlyList<Page> competitorPages,
        IReadOnlyList<PageContentBlock> blocks,
        List<ComparisonCheck> checks,
        List<Finding> findings)
    {
        var signalTypes = new[] { "faq", "table" };
        var competitorHas = signalTypes.Where(t =>
            competitorPages.Any(p => blocks.Any(b => b.PageId == p.Id && b.BlockType.Equals(t, StringComparison.OrdinalIgnoreCase)))).ToList();

        var targetHas = signalTypes.Where(t =>
            targetPages.Any(p => blocks.Any(b => b.PageId == p.Id && b.BlockType.Equals(t, StringComparison.OrdinalIgnoreCase)))).ToList();

        var missing = competitorHas.Except(targetHas, StringComparer.OrdinalIgnoreCase).ToList();
        var payload = JsonSerializer.Serialize(new { missingBlockTypes = missing });
        AddCheck(run, FindingType.ContentBlockGap, missing.Count > 0, payload, checks, findings);
    }

    private static void EvaluateInternalOrphanPage(
        AnalysisRun run,
        IReadOnlyList<Page> targetPages,
        IReadOnlyList<InternalLink> internalLinks,
        List<ComparisonCheck> checks,
        List<Finding> findings)
    {
        var inbound = internalLinks.GroupBy(l => l.ToPageId).ToDictionary(g => g.Key, g => g.Count());
        var orphans = targetPages
            .Where(p => !inbound.ContainsKey(p.Id) && !string.Equals(p.Url, run.TargetSiteUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Url)
            .ToList();

        var payload = JsonSerializer.Serialize(new { orphanUrls = orphans });
        AddCheck(run, FindingType.InternalOrphanPage, orphans.Count > 0, payload, checks, findings);
    }

    private static void EvaluateInternalDepthIssue(
        AnalysisRun run,
        IReadOnlyList<Page> targetPages,
        List<ComparisonCheck> checks,
        List<Finding> findings)
    {
        var deepPages = targetPages
            .Where(p => p.DepthFromHomepage.HasValue && p.DepthFromHomepage.Value > DepthThreshold)
            .Select(p => new { p.Url, depth = p.DepthFromHomepage })
            .ToList();

        var payload = JsonSerializer.Serialize(new { threshold = DepthThreshold, pages = deepPages });
        AddCheck(run, FindingType.InternalDepthIssue, deepPages.Count > 0, payload, checks, findings);
    }

    private static void EvaluateInternalAuthoritySkew(
        AnalysisRun run,
        IReadOnlyList<Page> targetPages,
        IReadOnlyList<PageRankScore> scores,
        List<ComparisonCheck> checks,
        List<Finding> findings)
    {
        var targetScores = scores
            .Where(s => s.GraphScope == GraphScope.TargetInternal && targetPages.Any(p => p.Id == s.PageId))
            .Select(s => s.Score)
            .ToList();

        if (targetScores.Count == 0)
        {
            AddCheck(run, FindingType.InternalAuthoritySkew, false, "{}", checks, findings);
            return;
        }

        var max = targetScores.Max();
        var avg = targetScores.Average();
        var skew = avg > 0 && max / avg > 3.0;

        var payload = JsonSerializer.Serialize(new { maxScore = max, averageScore = avg, ratio = avg > 0 ? max / avg : 0 });
        AddCheck(run, FindingType.InternalAuthoritySkew, skew, payload, checks, findings);
    }

    private static void EvaluateOutboundLinkSignal(
        AnalysisRun run,
        IReadOnlyList<Page> targetPages,
        IReadOnlyList<Page> competitorPages,
        IReadOnlyList<CrossRunLink> crossLinks,
        List<ComparisonCheck> checks,
        List<Finding> findings)
    {
        var competitorIds = competitorPages.Select(p => p.Id).ToHashSet();
        var targetIds = targetPages.Select(p => p.Id).ToHashSet();

        var competitorOutboundDomains = crossLinks
            .Where(l => competitorIds.Contains(l.FromPageId))
            .Select(l => Utilities.DomainHelper.GetRegistrableDomain(Utilities.DomainHelper.GetHostFromUrl(l.Href)))
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .GroupBy(d => d, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= 2)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var targetOutboundDomains = crossLinks
            .Where(l => targetIds.Contains(l.FromPageId))
            .Select(l => Utilities.DomainHelper.GetRegistrableDomain(Utilities.DomainHelper.GetHostFromUrl(l.Href)))
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = competitorOutboundDomains.Except(targetOutboundDomains, StringComparer.OrdinalIgnoreCase).ToList();
        var payload = JsonSerializer.Serialize(new { sharedOutboundDomains = missing });
        AddCheck(run, FindingType.OutboundLinkSignal, missing.Count > 0, payload, checks, findings);
    }

    private static Dictionary<string, int> GetSchemaTypes(IReadOnlyList<Page> pages, IReadOnlyList<PageJsonLd> jsonLd)
    {
        var pageIds = pages.Select(p => p.Id).ToHashSet();
        return jsonLd
            .Where(j => pageIds.Contains(j.PageId) && !string.IsNullOrWhiteSpace(j.ParsedType))
            .GroupBy(j => j.ParsedType!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static void AddCheck(
        AnalysisRun run,
        FindingType findingType,
        bool hasFinding,
        string payload,
        List<ComparisonCheck> checks,
        List<Finding> findings)
    {
        checks.Add(new ComparisonCheck
        {
            Id = Guid.NewGuid(),
            ProjectId = run.ProjectId,
            RunId = run.Id,
            FindingType = findingType,
            Outcome = hasFinding ? ComparisonOutcome.Finding : ComparisonOutcome.NoFinding,
            PayloadJson = payload
        });

        if (hasFinding)
        {
            findings.Add(new Finding
            {
                Id = Guid.NewGuid(),
                ProjectId = run.ProjectId,
                RunId = run.Id,
                FindingType = findingType,
                Severity = "medium",
                PayloadJson = payload
            });
        }
    }
}

using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;

namespace SiteAnalyzer2.Services.Pipeline;

public class RunGateService
{
    private readonly AppDbContext _db;
    private readonly IRunProgressNotifier _progressNotifier;

    public RunGateService(AppDbContext db, IRunProgressNotifier progressNotifier)
    {
        _db = db;
        _progressNotifier = progressNotifier;
    }

    public async Task<RunGate> EvaluateAndPersistAsync(
        AnalysisRun run,
        PipelineStage stage,
        string? serpSupplement = null,
        CancellationToken ct = default)
    {
        var (passed, message, rowCounts) = stage switch
        {
            PipelineStage.Serp => await EvaluateSerpGateAsync(run.Id, serpSupplement, ct),
            PipelineStage.Filter => await EvaluateFilterGateAsync(run, ct),
            PipelineStage.Fetch => await EvaluateFetchGateAsync(run.Id, ct),
            PipelineStage.Extract => await EvaluateExtractGateAsync(run.Id, ct),
            PipelineStage.Graph => await EvaluateGraphGateAsync(run.Id, ct),
            PipelineStage.PageRank => await EvaluatePageRankGateAsync(run.Id, ct),
            PipelineStage.Comparison => await EvaluateComparisonGateAsync(run.Id, ct),
            _ => (false, $"Unknown stage {stage}.", new Dictionary<string, int>())
        };

        var gate = new RunGate
        {
            Id = Guid.NewGuid(),
            ProjectId = run.ProjectId,
            RunId = run.Id,
            Stage = stage,
            Passed = passed,
            ValidationMessage = message,
            RowCountsJson = System.Text.Json.JsonSerializer.Serialize(rowCounts),
            CheckedAt = DateTime.UtcNow
        };

        await _db.RunGates.AddAsync(gate, ct);

        run.CurrentStage = stage == PipelineStage.Serp ? null : stage;
        run.Status = passed
            ? stage == PipelineStage.Comparison
                ? RunStatus.ResearchReady
                : RunStatus.SerpReady
            : stage == PipelineStage.Serp
                ? RunStatus.SerpFailed
                : RunStatus.Failed;

        _db.AnalysisRuns.Update(run);
        await _db.SaveChangesAsync(ct);

        await _progressNotifier.NotifyStageCompleted(run.Id, stage, passed, message, run.Status, ct);

        return gate;
    }

    public async Task<RunGate> FailStageAsync(
        AnalysisRun run,
        PipelineStage stage,
        string message,
        CancellationToken ct = default)
    {
        var gate = new RunGate
        {
            Id = Guid.NewGuid(),
            ProjectId = run.ProjectId,
            RunId = run.Id,
            Stage = stage,
            Passed = false,
            ValidationMessage = message,
            RowCountsJson = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, int>()),
            CheckedAt = DateTime.UtcNow
        };

        await _db.RunGates.AddAsync(gate, ct);
        run.CurrentStage = stage;
        run.Status = RunStatus.Failed;
        _db.AnalysisRuns.Update(run);
        await _db.SaveChangesAsync(ct);
        await _progressNotifier.NotifyStageCompleted(run.Id, stage, false, message, run.Status, ct);
        return gate;
    }

    private async Task<(bool Passed, string Message, Dictionary<string, int> Counts)> EvaluateSerpGateAsync(
        Guid runId,
        string? supplement,
        CancellationToken ct)
    {
        var items = await _db.SerpItems
            .AsNoTracking()
            .Where(i => i.RunId == runId)
            .ToListAsync(ct);

        var organicCount = items.Count(i => i.Type == SerpItemTypes.Organic);
        var paidCount = items.Count(i => i.Type == SerpItemTypes.Paid || i.Ads);
        var relatedBlocks = items.Count(i => i.Type == SerpItemTypes.RelatedSearches);
        var paaCount = await _db.SerpRelatedQueries.CountAsync(
            q => q.SerpItem.RunId == runId, ct);
        var aiOverviewCount = items.Count(i => i.Type == SerpItemTypes.AiOverview);
        var totalItems = items.Count;

        var minItems = SerpGateConfiguration.ResolveMinItems();
        var run = await _db.AnalysisRuns.AsNoTracking().FirstAsync(r => r.Id == runId, ct);
        var serpPage = run.SerpPagesCount > 0 ? run.SerpPagesCount : run.SerpMaxPage;
        var passed = totalItems >= minItems;

        var breakdown =
            $"{totalItems} SERP items ({organicCount} organic, {paidCount} paid, " +
            $"{paaCount} PAA, {aiOverviewCount} ai overview; SERP page {serpPage})";

        string message;
        if (totalItems == 0)
        {
            message =
                "Import completed but no SERP items were parsed. " +
                "Re-save the Google page as HTML and import again, or stop the run.";
        }
        else if (passed)
        {
            message = $"{breakdown}. Review the SERP report — advance when you are satisfied.";
        }
        else
        {
            message = $"{breakdown}. Gate requires >= {minItems} stored item(s).";
        }

        if (!string.IsNullOrWhiteSpace(supplement))
            message = $"{message} {supplement}";

        return (passed, message, new Dictionary<string, int>
        {
            ["serp_items"] = totalItems,
            ["serp_organic"] = organicCount,
            ["serp_paid"] = paidCount,
            ["serp_related_blocks"] = relatedBlocks,
            ["serp_paa"] = paaCount,
            ["serp_ai_overview"] = aiOverviewCount,
            ["serp_page"] = serpPage
        });
    }

    private async Task<(bool Passed, string Message, Dictionary<string, int> Counts)> EvaluateFilterGateAsync(
        AnalysisRun run,
        CancellationToken ct)
    {
        var items = await _db.SerpItems
            .Where(i => i.RunId == run.Id && i.Type == SerpItemTypes.Organic && !i.Ads)
            .ToListAsync(ct);

        var included = items.Count(c => c.FilterStatus == FilterStatus.Included);
        var excluded = items.Count(c => c.FilterStatus == FilterStatus.Excluded);
        var pending = items.Count(c => c.FilterStatus == FilterStatus.PendingReview);
        var rejected = items.Count(c => c.FilterStatus == FilterStatus.Rejected);

        var serpDomains = items.Select(r => r.Domain ?? "").ToList();
        var referenceDomains = await _db.ReferenceExcludeDomains.Select(r => r.Domain).ToListAsync(ct);
        var hasReferenceInSerp = serpDomains.Any(d =>
            referenceDomains.Any(r => d.Contains(r, StringComparison.OrdinalIgnoreCase)));

        var referenceExcluded = items.Count(c =>
            c.FilterStatus == FilterStatus.Excluded
            && c.ExcludeReason != null
            && c.ExcludeReason.Contains("reference", StringComparison.OrdinalIgnoreCase));

        var referenceCheck = !hasReferenceInSerp || referenceExcluded >= 1;
        var passed = referenceCheck;

        var message = passed
            ? $"{included} included, {excluded} excluded, {rejected} rejected, {pending} pending review."
            : $"Filter gate failed: reference exclusion check passed={referenceCheck}.";

        return (passed, message, new Dictionary<string, int>
        {
            ["included"] = included,
            ["excluded"] = excluded,
            ["rejected"] = rejected,
            ["pending_review"] = pending,
            ["serp_page"] = run.SerpPagesCount > 0 ? run.SerpPagesCount : run.SerpMaxPage
        });
    }

    private async Task<(bool Passed, string Message, Dictionary<string, int> Counts)> EvaluateFetchGateAsync(
        Guid runId,
        CancellationToken ct)
    {
        var pageCount = await _db.Pages.CountAsync(p => p.RunId == runId, ct);
        var includedCount = await _db.SerpItems.CountAsync(
            i => i.RunId == runId
                && i.Type == SerpItemTypes.Organic
                && !i.Ads
                && i.FilterStatus == FilterStatus.Included, ct);
        var required = includedCount + 1;
        var passed = pageCount >= required;
        var message = passed
            ? $"{pageCount} pages fetched (required >= {required})."
            : $"{pageCount} pages fetched; gate requires >= {required}. Stage failed.";
        return (passed, message, new Dictionary<string, int> { ["pages"] = pageCount, ["required"] = required });
    }

    private async Task<(bool Passed, string Message, Dictionary<string, int> Counts)> EvaluateExtractGateAsync(
        Guid runId,
        CancellationToken ct)
    {
        var pageIds = await _db.Pages.Where(p => p.RunId == runId).Select(p => p.Id).ToListAsync(ct);
        var targetPageIds = await _db.Pages.Where(p => p.RunId == runId && p.IsTargetSite).Select(p => p.Id).ToListAsync(ct);
        var h2ToH6 = await _db.PageHeadings.CountAsync(h => pageIds.Contains(h.PageId) && h.Level >= 2, ct);
        var jsonLd = await _db.PageJsonLdBlocks.CountAsync(j => pageIds.Contains(j.PageId), ct);
        var targetContentBlocks = targetPageIds.Count == 0
            ? 0
            : await _db.PageContentBlocks.CountAsync(
                b => targetPageIds.Contains(b.PageId) && b.Content != null && b.Content != "", ct);
        var passed = h2ToH6 >= 5 && jsonLd >= 1 && targetContentBlocks >= 1;
        var message = passed
            ? $"Extraction complete: {h2ToH6} H2-H6 headings, {jsonLd} JSON-LD blocks, {targetContentBlocks} target content blocks with text."
            : $"Extraction gate failed: {h2ToH6} H2-H6 headings (need >= 5), {jsonLd} JSON-LD blocks (need >= 1), {targetContentBlocks} target content blocks with text (need >= 1).";
        return (passed, message, new Dictionary<string, int>
        {
            ["h2_h6"] = h2ToH6,
            ["json_ld"] = jsonLd,
            ["target_content_blocks"] = targetContentBlocks
        });
    }

    private async Task<(bool Passed, string Message, Dictionary<string, int> Counts)> EvaluateGraphGateAsync(
        Guid runId,
        CancellationToken ct)
    {
        var internalCount = await _db.InternalLinks.CountAsync(l => l.RunId == runId, ct);
        var crossCount = await _db.CrossRunLinks.CountAsync(l => l.RunId == runId, ct);
        var passed = internalCount >= 1;
        var message = passed
            ? $"Graph built: {internalCount} internal links, {crossCount} cross-run links."
            : $"Graph gate failed: {internalCount} internal links (need >= 1).";
        return (passed, message, new Dictionary<string, int>
        {
            ["internal_links"] = internalCount,
            ["cross_run_links"] = crossCount
        });
    }

    private async Task<(bool Passed, string Message, Dictionary<string, int> Counts)> EvaluatePageRankGateAsync(
        Guid runId,
        CancellationToken ct)
    {
        var pages = await _db.Pages.Where(p => p.RunId == runId).ToListAsync(ct);
        var targetCount = pages.Count(p => p.IsTargetSite);
        var serpCount = pages.Count(p => !p.IsTargetSite);
        var scores = await _db.PageRankScores.Where(s => s.RunId == runId).ToListAsync(ct);

        var targetScored = scores.Count(s => s.GraphScope == GraphScope.TargetInternal);
        var serpScored = scores.Count(s => s.GraphScope == GraphScope.SerpSet);

        var passed = targetScored >= targetCount && serpScored >= serpCount;
        var message = passed
            ? $"PageRank scores written for {targetScored} target and {serpScored} SERP pages."
            : $"PageRank gate failed: target scored {targetScored}/{targetCount}, SERP scored {serpScored}/{serpCount}.";
        return (passed, message, new Dictionary<string, int>
        {
            ["target_scored"] = targetScored,
            ["serp_scored"] = serpScored
        });
    }

    private async Task<(bool Passed, string Message, Dictionary<string, int> Counts)> EvaluateComparisonGateAsync(
        Guid runId,
        CancellationToken ct)
    {
        var checks = await _db.ComparisonChecks.Where(c => c.RunId == runId).ToListAsync(ct);
        var findings = await _db.Findings.CountAsync(f => f.RunId == runId, ct);
        var passed = checks.Count == 7;
        var findingCount = checks.Count(c => c.Outcome == ComparisonOutcome.Finding);
        var message = passed
            ? $"7 of 7 checks evaluated. {findingCount} findings, {7 - findingCount} no-finding."
            : $"Comparison gate failed: {checks.Count}/7 checks completed.";
        return (passed, message, new Dictionary<string, int>
        {
            ["comparison_checks"] = checks.Count,
            ["findings"] = findings
        });
    }
}

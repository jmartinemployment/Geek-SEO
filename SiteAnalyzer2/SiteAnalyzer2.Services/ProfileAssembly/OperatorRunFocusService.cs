using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.BusinessFocus;
using SiteAnalyzer2.Services.Filtering;
using SiteAnalyzer2.Services.Parsing;
using SiteAnalyzer2.Services.Pipeline;
using SiteAnalyzer2.Services.ProfileAssembly;

namespace SiteAnalyzer2.Services.ProfileAssembly;

public sealed class OperatorRunFocusService(
    AppDbContext db,
    IServiceScopeFactory scopeFactory,
    RelevanceFilterService relevanceFilter,
    ILogger<OperatorRunFocusService> logger)
{
    /// <summary>Step A — after SERP import: label organics, set pillar metadata, start target crawl.</summary>
    public async Task AfterSerpImportAsync(Guid runId, CancellationToken ct = default)
    {
        try
        {
            await relevanceFilter.RunFilterStageAsync(runId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SERP relevance filter failed for run {RunId}; continuing.", runId);
        }

        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null)
        {
            throw new InvalidOperationException(
                $"Analysis run {runId} was not found after SERP import.");
        }

        var keyword = run.Keyword.Trim();
        var serpItems = await db.SerpItems.AsNoTracking()
            .Include(i => i.RelatedQueries)
            .Where(i => i.RunId == runId)
            .ToListAsync(ct);

        run.MatchedPillarTopic = keyword;
        run.MatchedPillarIntent = SiteProfileAssemblerHelpers.InferSearchIntent(serpItems, keyword);
        run.MatchedPillarAngle = SiteProfileAssemblerHelpers.FindMatchedPillarAngle(serpItems);
        run.GapTopics = [];
        run.WritingInstructions = null;
        if (string.Equals(run.CompetitorCrawlStatus, CompetitorCrawlStatuses.Complete, StringComparison.OrdinalIgnoreCase))
        {
            run.CompetitorCrawlStatus = CompetitorCrawlStatuses.PagesSaved;
            run.CompetitorCrawlMessage =
                "SERP updated. Run competitor crawl again to refresh the research pack.";
        }

        await db.SaveChangesAsync(ct);

        _ = RunTargetCrawlInBackgroundAsync(runId);
    }

    /// <summary>Step B — after competitor crawl: target pages + comparison + gap_topics.</summary>
    public async Task TryCompleteResearchFocusAsync(Guid runId, CancellationToken ct = default)
    {
        if (!await db.CompetitorPages.AsNoTracking().AnyAsync(p => p.RunId == runId, ct))
        {
            throw new InvalidOperationException(
                "Competitor crawl data is missing. Run competitor crawl before assembling the research pack.");
        }

        if (!await db.SerpItems.AsNoTracking().AnyAsync(
                i => i.RunId == runId && i.Type == SerpItemTypes.Organic && !i.Ads,
                ct))
        {
            throw new InvalidOperationException(
                "SERP organic results are missing. Import keyword SERP before assembling the research pack.");
        }

        using var scope = scopeFactory.CreateScope();
        var pageFetch = scope.ServiceProvider.GetRequiredService<PageFetchService>();
        var extraction = scope.ServiceProvider.GetRequiredService<PageExtractionService>();
        var comparison = scope.ServiceProvider.GetRequiredService<ComparisonService>();
        var businessFocus = scope.ServiceProvider.GetRequiredService<BusinessFocusClassificationService>();
        var assembler = scope.ServiceProvider.GetRequiredService<SiteProfileAssemblerService>();
        var scopedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureTargetSiteCrawledAsync(scopedDb, pageFetch, extraction, businessFocus, runId, ct);

        await comparison.RunOperatorComparisonAsync(runId, ct);
        await assembler.AssembleOperatorRunFocusAsync(runId, ct);
        await SyncGeneratedSchemaAsync(scopedDb, runId, ct);
        await VerifyResearchPackPersistedAsync(scopedDb, runId, ct);

        var run = await scopedDb.AnalysisRuns.FirstAsync(r => r.Id == runId, ct);
        run.Status = RunStatus.ResearchReady;
        run.CompetitorCrawlStatus = CompetitorCrawlStatuses.Complete;
        await scopedDb.SaveChangesAsync(ct);
    }

    private static async Task VerifyResearchPackPersistedAsync(AppDbContext scopedDb, Guid runId, CancellationToken ct)
    {
        var run = await scopedDb.AnalysisRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null)
        {
            throw new InvalidOperationException(
                $"Analysis run {runId} was not found after research pack assembly.");
        }

        if (run.GapTopics.Count == 0)
        {
            throw new InvalidOperationException(
                "Research pack assembly did not persist gap themes.");
        }
    }

    private async Task EnsureTargetSiteCrawledAsync(
        AppDbContext scopedDb,
        PageFetchService pageFetch,
        PageExtractionService extraction,
        BusinessFocusClassificationService businessFocus,
        Guid runId,
        CancellationToken ct)
    {
        var targetPageIds = await scopedDb.Pages.AsNoTracking()
            .Where(p => p.RunId == runId && p.IsTargetSite)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var hasHeadings = targetPageIds.Count > 0
            && await scopedDb.PageHeadings.AsNoTracking()
                .AnyAsync(h => targetPageIds.Contains(h.PageId), ct);

        if (!hasHeadings)
        {
            await pageFetch.RunTargetSiteFetchAsync(runId, ct);
            await extraction.RunExtractStageAsync(runId, ct);
            try
            {
                await businessFocus.RunAfterExtractAsync(runId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Business focus extract skipped for run {RunId}.", runId);
            }

            return;
        }

        var pagesNeedingExtract = await scopedDb.Pages.AsNoTracking()
            .Where(p => p.RunId == runId && p.IsTargetSite && p.HtmlContent != null)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var extractedPageIds = await scopedDb.PageHeadings.AsNoTracking()
            .Where(h => pagesNeedingExtract.Contains(h.PageId))
            .Select(h => h.PageId)
            .Distinct()
            .ToListAsync(ct);

        var hasPagesWithoutExtract = pagesNeedingExtract.Except(extractedPageIds).Any();

        if (hasPagesWithoutExtract)
        {
            await extraction.RunExtractStageAsync(runId, ct);
            try
            {
                await businessFocus.RunAfterExtractAsync(runId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Business focus extract skipped for run {RunId}.", runId);
            }
        }
    }

    private static async Task SyncGeneratedSchemaAsync(AppDbContext scopedDb, Guid runId, CancellationToken ct)
    {
        var run = await scopedDb.AnalysisRuns.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null)
            return;

        var extractProfile = await scopedDb.TargetSiteBusinessProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.RunId == runId, ct);
        if (extractProfile is null || string.IsNullOrWhiteSpace(extractProfile.GeneratedSchemaJson))
            return;

        var siteProfile = await scopedDb.SiteProfiles
            .FirstOrDefaultAsync(p => p.SiteUrl == run.TargetSiteUrl, ct);
        if (siteProfile is null)
            return;

        siteProfile.GeneratedSchemaJson = extractProfile.GeneratedSchemaJson;
        siteProfile.BusinessProfileAt = extractProfile.GeneratedAt;
        await scopedDb.SaveChangesAsync(ct);
    }

    private async Task RunTargetCrawlInBackgroundAsync(Guid runId)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var pageFetch = scope.ServiceProvider.GetRequiredService<PageFetchService>();
            var extraction = scope.ServiceProvider.GetRequiredService<PageExtractionService>();
            var businessFocus = scope.ServiceProvider.GetRequiredService<BusinessFocusClassificationService>();
            var scopedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await pageFetch.RunTargetSiteFetchAsync(runId);
            await extraction.RunExtractStageAsync(runId);
            try
            {
                await businessFocus.RunAfterExtractAsync(runId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Business focus extract skipped for run {RunId}.", runId);
            }

            await SyncGeneratedSchemaAsync(scopedDb, runId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Background target-site crawl failed for run {RunId}.", runId);
        }
    }
}

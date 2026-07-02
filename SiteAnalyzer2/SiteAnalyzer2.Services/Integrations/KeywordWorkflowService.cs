using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.CompetitorCrawl;
using SiteAnalyzer2.Services.Filtering;
using SiteAnalyzer2.Services.Pipeline;
using SiteAnalyzer2.Services.ProfileAssembly;
using SiteAnalyzer2.Services.Rankings;

namespace SiteAnalyzer2.Services.Integrations;

public sealed class KeywordWorkflowService(
    AppDbContext db,
    SerpAutoImportService autoImport,
    CompetitorCrawlService competitorCrawl,
    SiteProfileService siteProfiles,
    OperatorRunFocusService runFocus,
    SerpRankHistoryService rankHistory)
{
    public async Task<KeywordPageImportResultDto> ImportKeywordPageAsync(
        string targetSiteUrl,
        string html,
        string? topicSlug = null,
        CancellationToken ct = default)
    {
        var normalized = TargetSiteUrlNormalizer.Normalize(targetSiteUrl);
        var projectId = await siteProfiles.ResolveProjectIdForImportAsync(normalized, ct);
        return await ImportKeywordPageAsync(projectId, normalized, html, topicSlug, ct);
    }

    public async Task<KeywordPageImportResultDto> ImportKeywordPageAsync(
        Guid projectId,
        string targetSiteUrl,
        string html,
        string? topicSlug = null,
        CancellationToken ct = default)
    {
        var normalized = TargetSiteUrlNormalizer.Normalize(targetSiteUrl);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var import = await autoImport.ImportSavedHtmlAsync(projectId, html, normalized, ct);
        var keywordSaved = await HasKeywordDataAsync(import.RunId, ct);

        if (!keywordSaved)
            return FailedKeywordImport(import);

        if (!string.IsNullOrWhiteSpace(topicSlug))
        {
            var run = await db.AnalysisRuns.FirstAsync(r => r.Id == import.RunId, ct);
            run.TopicSlug = topicSlug.Trim().ToLowerInvariant();
            await db.SaveChangesAsync(ct);
        }

        await runFocus.AfterSerpImportAsync(import.RunId, ct);
        await siteProfiles.TouchAfterRunAsync(normalized, ct);
        await transaction.CommitAsync(ct);

        return await BuildImportResultAsync(projectId, import.RunId, normalized, ct)
            ?? FailedKeywordImport(import);
    }

    public async Task<KeywordPageImportResultDto?> GetKeywordImportSummaryAsync(
        Guid keywordProjectId,
        CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == keywordProjectId, ct);
        if (run is null)
            return null;

        var items = await db.SerpItems.AsNoTracking()
            .Include(i => i.RelatedQueries)
            .Where(i => i.RunId == keywordProjectId)
            .ToListAsync(ct);

        if (items.Count == 0)
            return null;

        var counts = SerpImportCounts.FromEntities(items);
        var filter = SerpFilterCounts.FromRunItems(items, run.TargetSiteUrl, run.Keyword);
        return new KeywordPageImportResultDto
        {
            ProjectId = run.ProjectId,
            KeywordProjectId = run.Id,
            Keyword = run.Keyword,
            KeywordSaved = true,
            OrganicCount = counts.OrganicCount,
            OrganicOnlyCount = counts.OrganicOnlyCount,
            PaidCount = counts.PaidCount,
            AiOverviewCount = counts.AiOverviewCount,
            AiOverviewAvailable = counts.AiOverviewAvailable,
            PaaCount = counts.PaaCount,
            CompetitorCrawlSeedCount = filter.CompetitorCrawlSeedCount,
            FilterApplied = filter.FilterApplied,
            FilterIncludedCount = filter.Included,
            FilterExcludedCount = filter.Excluded,
            FilterRejectedCount = filter.Rejected,
            FilterPendingReviewCount = filter.PendingReview,
            FilterCrawlEligibleCount = filter.CrawlEligible,
            Message = "Keyword import summary loaded from database.",
        };
    }

    public async Task<CompetitorCrawlWorkflowResultDto> RunCompetitorCrawlAsync(
        Guid keywordProjectId,
        CancellationToken ct = default)
    {
        if (!await HasKeywordDataAsync(keywordProjectId, ct))
        {
            return new CompetitorCrawlWorkflowResultDto
            {
                CompetitorSaved = false,
                Message = "Keyword data is missing. Parse the keyword page first.",
            };
        }

        try
        {
            var outcome = await competitorCrawl.RunAsync(keywordProjectId, ct);
            var competitorSaved = outcome.TotalPages > 0 && await HasCompetitorDataAsync(keywordProjectId, ct);

            if (!competitorSaved)
            {
                return new CompetitorCrawlWorkflowResultDto
                {
                    CompetitorSaved = false,
                    TotalPages = outcome.TotalPages,
                    DomainCount = outcome.DomainCount,
                    QualityWarnings = outcome.QualityWarnings,
                    Message = outcome.TotalPages == 0
                        ? "Competitor crawl fetched zero pages. Check SERP competitor URLs and try again."
                        : "Competitor crawl data was not saved.",
                };
            }

            await runFocus.TryCompleteResearchFocusAsync(keywordProjectId, ct);

            return new CompetitorCrawlWorkflowResultDto
            {
                CompetitorSaved = true,
                TotalPages = outcome.TotalPages,
                DomainCount = outcome.DomainCount,
                QualityWarnings = outcome.QualityWarnings,
                Message =
                    $"Saved {outcome.TotalPages} pages across {outcome.DomainCount} competitor domains. Research pack ready.",
            };
        }
        catch (InvalidOperationException ex)
        {
            return new CompetitorCrawlWorkflowResultDto
            {
                CompetitorSaved = false,
                Message = ex.Message,
            };
        }
    }

    private static KeywordPageImportResultDto FailedKeywordImport(SerpHtmlImportResultDto import) =>
        ToImportResult(import.ProjectId, import, keywordSaved: false, "Keyword data was not saved.");

    private async Task<KeywordPageImportResultDto?> BuildImportResultAsync(
        Guid projectId,
        Guid runId,
        string targetSiteUrl,
        CancellationToken ct)
    {
        var run = await db.AnalysisRuns.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null)
            return null;

        var items = await db.SerpItems.AsNoTracking()
            .Include(i => i.RelatedQueries)
            .Where(i => i.RunId == runId)
            .ToListAsync(ct);

        if (items.Count == 0)
            return null;

        var counts = SerpImportCounts.FromEntities(items);
        var filter = SerpFilterCounts.FromRunItems(items, targetSiteUrl, run.Keyword);
        var rankSummary = await rankHistory.GetSummaryAsync(runId, ct);

        return new KeywordPageImportResultDto
        {
            ProjectId = projectId,
            KeywordProjectId = runId,
            Keyword = run.Keyword,
            KeywordSaved = true,
            OrganicCount = counts.OrganicCount,
            OrganicOnlyCount = counts.OrganicOnlyCount,
            PaidCount = counts.PaidCount,
            AiOverviewCount = counts.AiOverviewCount,
            AiOverviewAvailable = counts.AiOverviewAvailable,
            PaaCount = counts.PaaCount,
            CompetitorCrawlSeedCount = filter.CompetitorCrawlSeedCount,
            FilterApplied = filter.FilterApplied,
            FilterIncludedCount = filter.Included,
            FilterExcludedCount = filter.Excluded,
            FilterRejectedCount = filter.Rejected,
            FilterPendingReviewCount = filter.PendingReview,
            FilterCrawlEligibleCount = filter.CrawlEligible,
            Message = filter.Rejected > 0
                ? $"Keyword data saved. Relevance filter: {filter.Included} for crawl, {filter.Rejected} rejected."
                : "Keyword data saved.",
            TargetOrganicPosition = rankSummary.LatestDelta?.CurrentPosition ?? rankSummary.History.LastOrDefault()?.TargetPosition,
            TargetOrganicUrl = rankSummary.History.LastOrDefault()?.TargetUrl,
            RankingsDelta = rankSummary.LatestDelta,
        };
    }

    private static KeywordPageImportResultDto ToImportResult(
        Guid projectId,
        SerpHtmlImportResultDto import,
        bool keywordSaved,
        string message) =>
        new()
        {
            ProjectId = projectId,
            KeywordProjectId = import.RunId,
            Keyword = import.Keyword,
            KeywordSaved = keywordSaved,
            OrganicCount = import.OrganicCount,
            OrganicOnlyCount = import.OrganicOnlyCount,
            PaidCount = import.PaidCount,
            AiOverviewCount = import.AiOverviewCount,
            AiOverviewAvailable = import.AiOverviewAvailable,
            PaaCount = import.PaaCount,
            CompetitorCrawlSeedCount = import.CompetitorCrawlSeedCount,
            Message = message,
            TargetOrganicPosition = import.TargetOrganicPosition,
            TargetOrganicUrl = import.TargetOrganicUrl,
            RankingsDelta = import.RankingsDelta,
        };

    private async Task<bool> HasKeywordDataAsync(Guid keywordProjectId, CancellationToken ct) =>
        await db.SerpItems.AsNoTracking().AnyAsync(
            i => i.RunId == keywordProjectId
                && i.ResearchLane == null
                && i.Type == SerpItemTypes.Organic
                && !i.Ads,
            ct);

    private async Task<bool> HasCompetitorDataAsync(Guid keywordProjectId, CancellationToken ct) =>
        await db.CompetitorPages.AsNoTracking().AnyAsync(p => p.RunId == keywordProjectId, ct);
}

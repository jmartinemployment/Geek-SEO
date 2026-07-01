using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Serp;
using SiteAnalyzer2.Services.Pipeline;
using SiteAnalyzer2.Services.Rankings;

namespace SiteAnalyzer2.Services.Integrations;

public sealed class SerpAutoImportService(
    AppDbContext db,
    SerpHtmlImportService htmlImport,
    SiteProfileService siteProfiles,
    SerpRankHistoryService rankHistory)
{
    public async Task<SerpHtmlImportResultDto> ImportSavedHtmlAsync(
        Guid projectId,
        string html,
        string? targetSiteUrl = null,
        CancellationToken ct = default)
    {
        if (!GoogleSerpHtmlParser.LooksLikeSerpPage(html))
        {
            throw new InvalidOperationException(
                "Uploaded HTML does not look like a Google SERP page. Save as 'Webpage, HTML only' from Chrome.");
        }

        var normalizedUrl = TargetSiteUrlNormalizer.Normalize(targetSiteUrl);
        await EnsureGeekSeoProjectAsync(projectId, normalizedUrl, ct);

        var parsed = GoogleSerpHtmlParser.ParseLivePage(html, keywordOverride: null);
        var keyword = string.IsNullOrWhiteSpace(parsed.Keyword) ? "unknown keyword" : parsed.Keyword.Trim();

        var run = await FindOrCreateRunAsync(projectId, keyword, normalizedUrl, ct);
        await htmlImport.ClearSerpDataForRunAsync(run.Id, ct);

        var outcome = await htmlImport.ImportHtmlAsync(run, html, keyword, ct);
        var rankResult = await rankHistory.RecordAfterImportAsync(run.Id, ct);

        return new SerpHtmlImportResultDto
        {
            RunId = run.Id,
            ProjectId = projectId,
            Keyword = keyword,
            TargetSiteUrl = run.TargetSiteUrl,
            OrganicCount = outcome.OrganicCount,
            OrganicOnlyCount = outcome.OrganicOnlyCount,
            PaidCount = outcome.PaidCount,
            AiOverviewCount = outcome.AiOverviewCount,
            AiOverviewAvailable = outcome.AiOverviewAvailable,
            PaaCount = outcome.PaaCount,
            CompetitorCrawlSeedCount = outcome.CompetitorCrawlSeedCount,
            GatePassed = outcome.GatePassed,
            GateMessage = outcome.GateMessage,
            RankingsDelta = rankResult?.RankingsDelta,
            TargetOrganicPosition = rankResult?.TargetOrganicPosition,
            TargetOrganicUrl = rankResult?.TargetOrganicUrl,
        };
    }

    private async Task<AnalysisRun> FindOrCreateRunAsync(
        Guid projectId,
        string keyword,
        string normalizedUrl,
        CancellationToken ct)
    {
        var keywordLower = keyword.ToLowerInvariant();
        var existing = await db.AnalysisRuns
            .Where(r =>
                r.TargetSiteUrl == normalizedUrl
                && r.Keyword.ToLower() == keywordLower)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            existing.TargetSiteUrl = normalizedUrl;
            existing.SerpProviderKey = SerpProviderPolicy.ManualHtmlKey;
            existing.ResearchMode = ResearchModes.Manual;
            existing.Status = RunStatus.Running;
            existing.CurrentStage = PipelineStage.Serp;
            await db.SaveChangesAsync(ct);
            return existing;
        }

        var run = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Keyword = keyword,
            TargetSiteUrl = normalizedUrl,
            SerpProviderKey = SerpProviderPolicy.ManualHtmlKey,
            ResearchMode = ResearchModes.Manual,
            Status = RunStatus.Running,
            CurrentStage = PipelineStage.Serp,
        };

        db.AnalysisRuns.Add(run);
        await db.SaveChangesAsync(ct);
        return run;
    }

    private async Task EnsureGeekSeoProjectAsync(Guid projectId, string normalizedUrl, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(normalizedUrl))
            return;

        await siteProfiles.LinkGeekSeoProjectAsync(projectId, normalizedUrl, ct);
    }
}

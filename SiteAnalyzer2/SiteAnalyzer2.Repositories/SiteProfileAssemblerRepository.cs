using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;

namespace SiteAnalyzer2.Repositories;

public sealed class SiteProfileAssemblerRepository(AppDbContext db) : ISiteProfileAssemblerRepository
{
    public async Task<string?> GetRunTargetSiteUrlAsync(Guid runId, CancellationToken ct = default) =>
        await db.AnalysisRuns
            .AsNoTracking()
            .Where(r => r.Id == runId)
            .Select(r => r.TargetSiteUrl)
            .FirstOrDefaultAsync(ct);

    public async Task<Guid?> GetSiteProfileIdBySiteUrlAsync(string normalizedSiteUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(normalizedSiteUrl))
            return null;

        return await db.SiteProfiles
            .AsNoTracking()
            .Where(p => p.SiteUrl == normalizedSiteUrl)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<SiteProfile?> GetSiteProfileByIdAsync(Guid siteProfileId, CancellationToken ct = default) =>
        await db.SiteProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == siteProfileId, ct);

    public async Task<IReadOnlyList<string>> GetSiteKeywordsAsync(string normalizedSiteUrl, CancellationToken ct = default) =>
        await db.AnalysisRuns
            .AsNoTracking()
            .Where(r => r.TargetSiteUrl == normalizedSiteUrl && !string.IsNullOrWhiteSpace(r.Keyword))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.Keyword)
            .Distinct()
            .Take(20)
            .ToListAsync(ct);

    public async Task<SiteProfileAssemblySource> LoadAssemblySourceAsync(
        Guid siteProfileId,
        Guid runId,
        CancellationToken ct = default)
    {
        var profile = await db.SiteProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == siteProfileId, ct)
            ?? throw new InvalidOperationException($"Site profile {siteProfileId} not found.");

        var run = await db.AnalysisRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Analysis run {runId} not found.");

        var targetPages = await db.Pages
            .AsNoTracking()
            .Where(p => p.RunId == runId && p.IsTargetSite)
            .OrderBy(p => p.DepthFromHomepage)
            .ToListAsync(ct);

        var pageIds = targetPages.Select(p => p.Id).ToList();
        var headings = pageIds.Count == 0
            ? []
            : await db.PageHeadings.AsNoTracking().Where(h => pageIds.Contains(h.PageId)).ToListAsync(ct);
        var metaTags = pageIds.Count == 0
            ? []
            : await db.PageMetaTags.AsNoTracking().Where(m => pageIds.Contains(m.PageId)).ToListAsync(ct);
        var jsonLd = pageIds.Count == 0
            ? []
            : await db.PageJsonLdBlocks.AsNoTracking().Where(j => pageIds.Contains(j.PageId)).ToListAsync(ct);

        var snapshots = targetPages
            .Select(page => new TargetPageSnapshot
            {
                Page = page,
                Headings = headings.Where(h => h.PageId == page.Id).OrderBy(h => h.Sequence).ToList(),
                MetaTags = metaTags.Where(m => m.PageId == page.Id).ToList(),
                JsonLdBlocks = jsonLd.Where(j => j.PageId == page.Id).ToList(),
            })
            .ToList();

        var serpItems = await db.SerpItems
            .AsNoTracking()
            .Include(i => i.RelatedQueries)
            .Where(i => i.RunId == runId)
            .OrderBy(i => i.RankAbsolute)
            .ToListAsync(ct);

        var gapFindings = await db.Findings
            .AsNoTracking()
            .Where(f => f.RunId == runId
                && (f.FindingType == FindingType.ContentBlockGap
                    || f.FindingType == FindingType.HeadingStructureGap
                    || f.FindingType == FindingType.StructuredDataGap))
            .ToListAsync(ct);

        var competitorPageIds = await db.CompetitorPages.AsNoTracking()
            .Where(p => p.RunId == runId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var competitorHeadingTexts = competitorPageIds.Count == 0
            ? []
            : await db.CompetitorPageHeadings.AsNoTracking()
                .Where(h => competitorPageIds.Contains(h.CompetitorPageId) && h.Level >= 2)
                .OrderBy(h => h.Sequence)
                .Select(h => h.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToListAsync(ct);

        var siteKeywords = await GetSiteKeywordsAsync(profile.SiteUrl, ct);

        return new SiteProfileAssemblySource
        {
            SiteProfile = profile,
            Run = run,
            TargetPages = snapshots,
            SerpItems = serpItems,
            GapFindings = gapFindings,
            SiteKeywords = siteKeywords,
            CompetitorHeadingTexts = competitorHeadingTexts,
        };
    }

    public async Task PersistSiteProfileAsync(
        Guid siteProfileId,
        SiteProfileAssemblyWrite siteWrite,
        CancellationToken ct = default)
    {
        var profile = await db.SiteProfiles.FirstOrDefaultAsync(p => p.Id == siteProfileId, ct)
            ?? throw new InvalidOperationException($"Site profile {siteProfileId} not found.");

        ApplySiteProfileWrite(profile, siteWrite);
        await db.SaveChangesAsync(ct);
    }

    public async Task PersistAsync(
        Guid siteProfileId,
        Guid runId,
        SiteProfileAssemblyWrite siteWrite,
        RunWritingFocusWrite runWrite,
        CancellationToken ct = default)
    {
        var profile = await db.SiteProfiles.FirstOrDefaultAsync(p => p.Id == siteProfileId, ct)
            ?? throw new InvalidOperationException($"Site profile {siteProfileId} not found.");

        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Analysis run {runId} not found.");

        ApplySiteProfileWrite(profile, siteWrite);

        run.MatchedPillarTopic = runWrite.MatchedPillarTopic;
        run.MatchedPillarIntent = runWrite.MatchedPillarIntent;
        run.MatchedPillarAngle = runWrite.MatchedPillarAngle;
        run.GapTopics = runWrite.GapTopics.ToList();
        run.WritingInstructions = runWrite.WritingInstructions;

        await db.SaveChangesAsync(ct);
    }

    private static void ApplySiteProfileWrite(SiteProfile profile, SiteProfileAssemblyWrite siteWrite)
    {
        profile.BusinessType = siteWrite.BusinessType;
        profile.BusinessDescription = siteWrite.BusinessDescription;
        profile.BusinessSummary = siteWrite.BusinessSummary;
        profile.ServiceAreaDescription = siteWrite.ServiceAreaDescription;
        profile.GeoAnchorNodes = siteWrite.GeoAnchorNodes.ToList();
        profile.PrimaryNiche = siteWrite.PrimaryNiche;
        profile.NicheDescription = siteWrite.NicheDescription;
        profile.NicheTags = siteWrite.NicheTags.ToList();
        profile.CompetitorDomains = siteWrite.CompetitorDomains.ToList();
        profile.AuthorityPageUrls = siteWrite.AuthorityPageUrls.ToList();
        profile.WritingRecommendations = siteWrite.WritingRecommendations.ToList();
        profile.BusinessProfileAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;
    }
}

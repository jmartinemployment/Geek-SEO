using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Serp;
using SiteAnalyzer2.Serp.Models;

namespace SiteAnalyzer2.Services.Pipeline;

public class SerpHtmlImportService(AppDbContext db, RunGateService runGate)
{
    public async Task<SerpImportOutcome> ImportHtmlAsync(
        AnalysisRun run,
        string html,
        string? keywordOverride = null,
        CancellationToken ct = default)
    {
        if (!GoogleSerpHtmlParser.LooksLikeSerpPage(html))
        {
            throw new InvalidOperationException(
                "Uploaded HTML does not look like a Google SERP page. Save as 'Webpage, HTML only' from Chrome.");
        }

        var parsed = GoogleSerpHtmlParser.ParseLivePage(html, keywordOverride ?? run.Keyword);
        return await PersistParsedPageAsync(run, parsed, ct);
    }

    public async Task ClearSerpDataForRunAsync(Guid runId, CancellationToken ct = default)
    {
        var items = await db.SerpItems.Where(i => i.RunId == runId).ToListAsync(ct);
        if (items.Count == 0)
            return;

        db.SerpItems.RemoveRange(items);
        await db.SaveChangesAsync(ct);
    }

    public async Task<SerpImportOutcome> PersistParsedPageAsync(
        AnalysisRun run,
        SerpLivePageParseResult parsed,
        CancellationToken ct = default)
    {
        var existing = await db.SerpItems.AnyAsync(i => i.RunId == run.Id, ct);
        if (existing)
            throw new InvalidOperationException("SERP items already exist for this run.");

        run.Keyword = string.IsNullOrWhiteSpace(parsed.Keyword) ? run.Keyword : parsed.Keyword;
        run.SerpLocationCode = parsed.LocationCode;
        run.SerpLanguageCode = parsed.LanguageCode;
        run.SerpDevice = parsed.Device;
        run.SerpOs = parsed.Os;
        run.SerpDepth = parsed.Depth;
        run.SerpSeDomain = parsed.SeDomain;
        run.SerpCheckUrl = parsed.CheckUrl;
        run.SerpCapturedAt = parsed.CapturedAtUtc;
        run.SerpSeResultsCount = parsed.SeResultsCount;
        run.SerpPagesCount = parsed.PagesCount;
        run.SerpMaxPage = parsed.PagesCount;
        run.SerpItemsCount = parsed.Items.Count;
        run.SerpItemTypesJson = JsonSerializer.Serialize(parsed.ItemTypes);
        run.SerpLocalPackPresent = parsed.LocalPackPresent;
        run.SerpShoppingResultsPresent = parsed.ShoppingResultsPresent;

        var entities = parsed.Items.Select(item => MapItem(run, item)).ToList();
        await db.SerpItems.AddRangeAsync(entities, ct);
        db.AnalysisRuns.Update(run);
        await db.SaveChangesAsync(ct);

        var counts = SerpImportCounts.FromEntities(entities);
        var gate = await runGate.EvaluateAndPersistAsync(run, Domain.Enums.PipelineStage.Serp, null, ct);
        return new SerpImportOutcome(counts, gate.Passed, gate.ValidationMessage);
    }

    private static SerpItem MapItem(AnalysisRun run, SerpParsedItem item)
    {
        var entity = new SerpItem
        {
            Id = Guid.NewGuid(),
            ProjectId = run.ProjectId,
            RunId = run.Id,
            Type = item.Type,
            RankGroup = item.RankGroup,
            RankAbsolute = item.RankAbsolute,
            Page = item.Page,
            Position = item.Position,
            Xpath = item.Xpath,
            RectangleJson = null,
            Domain = item.Domain,
            Title = item.Title,
            Url = item.Url,
            CacheUrl = item.CacheUrl,
            RelatedSearchUrl = item.RelatedSearchUrl,
            Breadcrumb = item.Breadcrumb,
            WebsiteName = item.WebsiteName,
            IsImage = item.IsImage,
            IsVideo = item.IsVideo,
            IsFeaturedSnippet = item.IsFeaturedSnippet,
            IsMalicious = item.IsMalicious,
            IsWebStory = item.IsWebStory,
            Description = item.Description,
            PreSnippet = item.PreSnippet,
            ExtendedSnippet = item.ExtendedSnippet,
            ImagesJson = item.ImagesJson,
            AmpVersion = item.AmpVersion,
            RatingJson = item.RatingJson,
            PriceJson = item.PriceJson,
            FaqJson = item.FaqJson,
            ExtendedPeopleAlsoSearchJson = item.ExtendedPeopleAlsoSearchJson,
            AboutThisResultJson = item.AboutThisResultJson,
            RelatedResultJson = item.RelatedResultJson,
            Timestamp = item.Timestamp,
            AiOverviewAvailable = item.AiOverviewAvailable,
            AiOverviewMarkdown = item.AiOverviewMarkdown,
            AiOverviewStatusMessage = item.AiOverviewStatusMessage,
            Ads = item.Ads
        };

        if (item.Links is { Count: > 0 })
        {
            var sequence = 1;
            foreach (var link in item.Links)
            {
                entity.Links.Add(new SerpItemLink
                {
                    Id = Guid.NewGuid(),
                    SerpItemId = entity.Id,
                    Sequence = sequence++,
                    Title = link.Title,
                    Url = link.Url
                });
            }
        }

        if (item.Highlighted is { Count: > 0 })
        {
            var sequence = 1;
            foreach (var phrase in item.Highlighted)
            {
                entity.HighlightedPhrases.Add(new SerpItemHighlighted
                {
                    Id = Guid.NewGuid(),
                    SerpItemId = entity.Id,
                    Sequence = sequence++,
                    Text = phrase
                });
            }
        }

        if (item.RelatedQueries is { Count: > 0 })
        {
            foreach (var query in item.RelatedQueries)
            {
                entity.RelatedQueries.Add(new SerpRelatedQuery
                {
                    Id = Guid.NewGuid(),
                    SerpItemId = entity.Id,
                    Sequence = query.Sequence,
                    QueryText = query.QueryText,
                    QueryType = query.QueryType
                });
            }
        }

        return entity;
    }
}

public sealed record SerpImportCounts(
    int OrganicOnlyCount,
    int PaidCount,
    int OrganicCount,
    int AiOverviewCount,
    bool AiOverviewAvailable,
    int PaaCount,
    int CompetitorCrawlSeedCount)
{
    public static SerpImportCounts FromEntities(IReadOnlyList<SerpItem> entities)
    {
        var organicOnly = entities.Count(i => i.Type == Domain.SerpItemTypes.Organic && !i.Ads);
        var paid = entities.Count(i => i.Type == Domain.SerpItemTypes.Paid || i.Ads);
        var aiOverviewItems = entities.Where(i => i.Type == Domain.SerpItemTypes.AiOverview).ToList();

        return new SerpImportCounts(
            organicOnly,
            paid,
            organicOnly + paid,
            aiOverviewItems.Count,
            aiOverviewItems.Any(i => i.AiOverviewAvailable == true),
            entities.SelectMany(i => i.RelatedQueries).Count(),
            organicOnly);
    }
}

public record SerpImportOutcome(SerpImportCounts Counts, bool GatePassed, string GateMessage)
{
    public int OrganicCount => Counts.OrganicCount;
    public int PaaCount => Counts.PaaCount;
    public int OrganicOnlyCount => Counts.OrganicOnlyCount;
    public int PaidCount => Counts.PaidCount;
    public int AiOverviewCount => Counts.AiOverviewCount;
    public bool AiOverviewAvailable => Counts.AiOverviewAvailable;
    public int CompetitorCrawlSeedCount => Counts.CompetitorCrawlSeedCount;
}

using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Services.SiteAnalyzerStepRunners;

internal static partial class SiteAnalyzerStepRelationalLoader
{
    internal const string ServicePhraseKind = "service_phrase";
    internal const string VerticalTopicKind = "vertical_topic";

    internal sealed record MergingInputs(
        SchemaOrgData Schema,
        SitemapData Sitemap,
        NavMenuData Nav,
        HomepageHeadings Headings,
        PageContentData PageContent,
        SiteAnalyzerStepArtifactStore.SiteStructureArtifact Structure);

    internal static async Task<MergingInputs> LoadMergingInputsAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        string domain,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var schema = await LoadSchemaAsync(profileRepo, profileId, steps, ct);
        var sitemap = await LoadSitemapAsync(profileRepo, profileId, steps, ct);
        var nav = await LoadNavAsync(profileRepo, profileId, domain, steps, ct);
        var headings = await LoadHeadingsAsync(profileRepo, profileId, domain, steps, ct);
        var pageContent = await LoadPageContentAsync(profileRepo, profileId, steps, ct);
        var structure = await LoadSiteStructureAsync(profileRepo, profileId, steps, ct);

        return new MergingInputs(schema, sitemap, nav, headings, pageContent, structure);
    }

    internal static async Task<SiteBusinessProfile> LoadSiteBusinessProfileAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        string domain,
        CancellationToken ct)
    {
        var details = await profileRepo.GetAnalysisDetailsRowAsync(profileId, includeFusion: false, ct);
        if (!details.IsSuccess || details.Value is null)
            throw new InvalidOperationException("Step log not available.");

        var steps = SiteAnalyzerStepArtifactStore.ParseSteps(details.Value.AnalysisStepLog);
        var schema = await LoadSchemaAsync(profileRepo, profileId, steps, ct);
        var headings = await LoadHeadingsAsync(profileRepo, profileId, domain, steps, ct);
        return SiteBusinessProfileBuilder.Build(schema, headings);
    }

    internal static async Task<SchemaOrgData> LoadSchemaAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var signalsResult = await profileRepo.GetSchemaSignalsAsync(profileId, ct);
        if (signalsResult.IsSuccess && signalsResult.Value is { Count: > 0 } signals)
        {
            var fallback = SiteAnalyzerStepArtifactStore.TryGetArtifact<SchemaOrgData>(steps, "schema", "schema");
            return BuildSchemaOrgData(signals, fallback);
        }

        return SiteAnalyzerStepArtifactStore.GetRequiredArtifact<SchemaOrgData>(steps, "schema", "schema");
    }

    internal static async Task<SitemapData> LoadSitemapAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var urlsResult = await profileRepo.GetDiscoveredUrlsAsync(profileId, ct);
        if (urlsResult.IsSuccess && urlsResult.Value is { Count: > 0 } urls)
        {
            var sitemapUrls = urls
                .Where(x => string.Equals(x.SourceType, "sitemap", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Url)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (sitemapUrls.Count > 0)
            {
                var fallback = SiteAnalyzerStepArtifactStore.TryGetArtifact<SitemapData>(steps, "site_urls", "site_urls");
                return new SitemapData(
                    fallback?.Pillars ?? [],
                    fallback?.TotalUrlsScanned ?? sitemapUrls.Count,
                    sitemapUrls);
            }
        }

        return SiteAnalyzerStepArtifactStore.GetRequiredArtifact<SitemapData>(steps, "site_urls", "site_urls");
    }

    internal static async Task<NavMenuData> LoadNavAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        string domain,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var linksResult = await profileRepo.GetNavigationLinksAsync(profileId, ct);
        if (linksResult.IsSuccess && linksResult.Value is { Count: > 0 } links)
        {
            var extractMethod = links
                .Select(x => x.LinkArea)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                ?? "nav";
            var pillars = links
                .Select(link => ToNavPillar(link, domain))
                .Where(p => !string.IsNullOrWhiteSpace(p.Slug))
                .ToList();
            return new NavMenuData(pillars, extractMethod);
        }

        var artifact = SiteAnalyzerStepArtifactStore.TryGetArtifact<NavMenuData>(steps, "nav", "nav");
        if (artifact is not null)
            return artifact;

        // Nav is optional; manual runs often skip Playwright and step-log artifacts are stripped
        // after relational persist, leaving zero navigation link rows.
        return new NavMenuData([], "skipped");
    }

    internal static async Task<HomepageHeadings> LoadHeadingsAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        string domain,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var headingsResult = await profileRepo.GetHeadingsAsync(profileId, ct);
        if (headingsResult.IsSuccess && headingsResult.Value is { Count: > 0 } rows)
        {
            var fallback = SiteAnalyzerStepArtifactStore.TryGetArtifact<HomepageHeadings>(steps, "headings", "headings");
            var pageHeadings = rows
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new PageHeading { Level = x.HeadingLevel, Text = x.HeadingText })
                .ToList();
            var h2Texts = pageHeadings
                .Where(h => h.Level == 2)
                .Select(h => h.Text)
                .ToList();
            return new HomepageHeadings
            {
                Title = fallback?.Title,
                MetaDescription = fallback?.MetaDescription,
                Headings = pageHeadings,
                H2Texts = h2Texts,
            };
        }

        return SiteAnalyzerStepArtifactStore.GetRequiredArtifact<HomepageHeadings>(steps, "headings", "headings");
    }

    internal static async Task<PageContentData> LoadPageContentAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var contentResult = await profileRepo.GetPageContentAsync(profileId, ct);
        if (contentResult.IsSuccess && contentResult.Value is { Items.Count: > 0 } row)
        {
            var servicePhrases = row.Items
                .Where(x => string.Equals(x.ItemKind, ServicePhraseKind, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.ItemText)
                .ToList();
            var verticalTopics = row.Items
                .Where(x => string.Equals(x.ItemKind, VerticalTopicKind, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.ItemText)
                .ToList();
            return new PageContentData(servicePhrases, verticalTopics, row.ListItemsScanned);
        }

        return SiteAnalyzerStepArtifactStore.GetRequiredArtifact<PageContentData>(steps, "page_content", "page_content");
    }

    internal static async Task<SiteAnalyzerStepArtifactStore.SiteStructureArtifact?> TryLoadSiteStructureAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var structureResult = await profileRepo.GetSiteStructureAsync(profileId, ct);
        if (structureResult.IsSuccess && structureResult.Value is { Pages.Count: > 0 } row)
            return BuildSiteStructure(row);

        return TryGetSiteStructureFromStepArtifacts(steps);
    }

    internal static async Task<SiteAnalyzerStepArtifactStore.SiteStructureArtifact> LoadSiteStructureAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var structureResult = await profileRepo.GetSiteStructureAsync(profileId, ct);
        if (structureResult.IsSuccess && structureResult.Value is { Pages.Count: > 0 } row)
            return BuildSiteStructure(row);

        var artifact = TryGetSiteStructureFromStepArtifacts(steps);
        if (artifact is not null)
            return artifact;

        throw new InvalidOperationException("Site structure artifact is not available.");
    }

    internal static async Task<SiteAnalyzerStepArtifactStore.SiteStructureArtifact> LoadSiteCrawlAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var structureResult = await profileRepo.GetSiteStructureAsync(profileId, ct);
        if (structureResult.IsSuccess && structureResult.Value is { Pages.Count: > 0 } row)
            return BuildSiteStructure(row);

        var artifact = TryGetSiteStructureFromStepArtifacts(steps);
        if (artifact is not null)
            return artifact;

        throw new InvalidOperationException("Site crawl artifact is not available.");
    }

    private static SiteAnalyzerStepArtifactStore.SiteStructureArtifact? TryGetSiteStructureFromStepArtifacts(
        IReadOnlyList<SiteAnalysisStepLogEntry> steps)
    {
        var legacy = SiteAnalyzerStepArtifactStore.TryGetArtifact<SiteAnalyzerStepArtifactStore.SiteStructureArtifact>(
            steps,
            "site_structure",
            "site_structure");
        if (legacy is not null)
            return legacy;

        var crawlArtifact = SiteAnalyzerStepArtifactStore.TryGetArtifact<SiteAnalyzerStepArtifactStore.SiteStructureArtifact>(
            steps,
            "site_crawl",
            "site_crawl");
        var linksArtifact = SiteAnalyzerStepArtifactStore.TryGetArtifact<SiteAnalyzerStepArtifactStore.SiteStructureArtifact>(
            steps,
            "internal_links",
            "internal_links");
        var patternsArtifact = SiteAnalyzerStepArtifactStore.TryGetArtifact<SiteAnalyzerStepArtifactStore.SiteStructureArtifact>(
            steps,
            "url_patterns",
            "url_patterns");

        if (crawlArtifact is null && linksArtifact is null && patternsArtifact is null)
            return null;

        var crawl = crawlArtifact?.Crawl
            ?? linksArtifact?.Crawl
            ?? patternsArtifact?.Crawl
            ?? throw new InvalidOperationException("Site crawl data is missing from step artifacts.");
        var internalLinks = linksArtifact?.InternalLinks
            ?? patternsArtifact?.InternalLinks
            ?? crawlArtifact?.InternalLinks
            ?? EmptyInternalLinks(crawl.PagesFetched);
        var urlPatterns = patternsArtifact?.UrlPatterns
            ?? linksArtifact?.UrlPatterns
            ?? crawlArtifact?.UrlPatterns
            ?? EmptyUrlPatterns();
        var crawledUrls = crawlArtifact?.CrawledUrls
            ?? linksArtifact?.CrawledUrls
            ?? patternsArtifact?.CrawledUrls
            ?? crawl.Pages.Select(p => p.Url).ToList();

        return new SiteAnalyzerStepArtifactStore.SiteStructureArtifact(
            crawl,
            internalLinks,
            urlPatterns,
            crawledUrls);
    }

    internal static InternalLinkData EmptyInternalLinks(int pagesScanned = 0) =>
        new([], new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), pagesScanned);

    internal static UrlPatternData EmptyUrlPatterns() => new([], 0);

    private static SiteAnalyzerStepArtifactStore.SiteStructureArtifact BuildSiteStructure(
        SiteAnalysisProfileSiteStructureRow row)
    {
        var pages = row.Pages
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new CrawledPage(x.Url, x.VisibleText, x.FetchMethod))
            .ToList();
        var crawlMeta = row.CrawlMeta;
        var crawl = new SiteCrawlData(
            pages,
            crawlMeta?.PagesAttempted ?? pages.Count,
            crawlMeta?.PagesFetched ?? pages.Count);

        var links = row.Links
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new InternalLinkEdge(
                x.SourceUrl,
                x.TargetUrl,
                x.AnchorText,
                x.InferredFromUrlSlug))
            .ToList();
        var inbound = links
            .GroupBy(x => x.TargetUrl, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        var internalLinks = new InternalLinkData(links, inbound, pages.Count);

        var urlPatterns = new UrlPatternData(
            row.UrlPatterns
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new UrlPatternTopic(x.Name, x.Slug, x.Url, x.PathSegment))
                .ToList(),
            Math.Max(pages.Count, row.UrlPatterns.Count));

        var crawledUrls = pages.Select(x => x.Url).ToList();
        return new SiteAnalyzerStepArtifactStore.SiteStructureArtifact(crawl, internalLinks, urlPatterns, crawledUrls);
    }

    internal static SiteAnalysisProfilePageContentWrite ToPageContentWrite(string pageUrl, PageContentData data)
    {
        var items = new List<SiteAnalysisProfilePageContentItemWrite>();
        var order = 0;
        foreach (var phrase in data.ServicePhrases)
            items.Add(new SiteAnalysisProfilePageContentItemWrite(pageUrl, ServicePhraseKind, phrase, order++));
        foreach (var topic in data.VerticalTopics)
            items.Add(new SiteAnalysisProfilePageContentItemWrite(pageUrl, VerticalTopicKind, topic, order++));
        return new SiteAnalysisProfilePageContentWrite(pageUrl, data.ListItemsScanned, items);
    }

    internal static SiteAnalysisProfileSiteStructureWrite ToSiteStructureWrite(
        SiteCrawlData crawlData,
        InternalLinkData internalLinks,
        UrlPatternData urlPatterns)
    {
        var pages = crawlData.Pages
            .Select((page, index) =>
            {
                var visibleText = ExtractVisibleText(page.Html);
                return new SiteAnalysisProfileSitePageWrite(
                    page.Url,
                    page.FetchMethod,
                    visibleText,
                    NormalizedTopicalityCalculator.EstimateWordCount(page.Html),
                    index);
            })
            .ToList();

        var links = internalLinks.Links
            .Select((link, index) => new SiteAnalysisProfileSitePageLinkWrite(
                link.SourceUrl,
                link.TargetUrl,
                link.AnchorText,
                link.InferredFromUrlSlug,
                index))
            .ToList();

        var patterns = urlPatterns.Topics
            .Select((topic, index) => new SiteAnalysisProfileUrlPatternTopicWrite(
                topic.Name,
                topic.Slug,
                topic.Url,
                topic.PathSegment,
                index))
            .ToList();

        return new SiteAnalysisProfileSiteStructureWrite(
            pages,
            links,
            patterns,
            new SiteAnalysisProfileSiteCrawlMetaWrite(crawlData.PagesAttempted, crawlData.PagesFetched));
    }

    private static SchemaOrgData BuildSchemaOrgData(
        IReadOnlyList<SiteAnalysisProfileSchemaSignalRow> signals,
        SchemaOrgData? fallback)
    {
        static IEnumerable<string> Values(
            IEnumerable<SiteAnalysisProfileSchemaSignalRow> rows,
            string schemaType,
            string propertyName) =>
            rows
                .Where(x =>
                    x.SchemaType.Equals(schemaType, StringComparison.OrdinalIgnoreCase)
                    && x.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.PropertyValue);

        var serviceNames = Values(signals, "service", "name").ToList();
        var knowsAbout = Values(signals, "thing", "knowsAbout").ToList();
        var offerCatalog = Values(signals, "offer_catalog", "serviceType").ToList();
        var areaServed = Values(signals, "organization", "areaServed").ToList();
        var sameAs = Values(signals, "organization", "sameAs").ToList();
        var description = Values(signals, "organization", "description").FirstOrDefault();
        var brandName = Values(signals, "organization", "brandName").FirstOrDefault();

        return new SchemaOrgData(
            serviceNames,
            knowsAbout,
            offerCatalog,
            description,
            brandName,
            areaServed,
            sameAs,
            fallback?.ResolvedEntityPlatforms ?? [],
            fallback?.EntityResolved ?? sameAs.Count > 0);
    }

    private static DiscoveredPillar ToNavPillar(SiteAnalysisProfileNavigationLinkRow link, string domain)
    {
        var slug = SlugFromUrl(link.LinkUrl) ?? SiteAnalyzerService.NameToSlug(link.AnchorText ?? string.Empty);
        return new DiscoveredPillar
        {
            Name = link.AnchorText ?? slug,
            Slug = slug,
            PageUrl = link.LinkUrl,
            Source = "nav",
        };
    }

    private static string? SlugFromUrl(string url)
    {
        try
        {
            var path = new Uri(url).AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(path))
                return null;
            var segment = path.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            return string.IsNullOrWhiteSpace(segment) ? null : segment.ToLowerInvariant();
        }
        catch
        {
            return null;
        }
    }

    private const int MaxHtmlCharsForVisibleText = 512_000;
    private const int MaxVisibleTextChars = 32_768;

    private static string ExtractVisibleText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        if (html.Length > MaxHtmlCharsForVisibleText)
            html = html[..MaxHtmlCharsForVisibleText];

        var stripped = ScriptTagRegex().Replace(html, " ");
        stripped = StyleTagRegex().Replace(stripped, " ");
        stripped = TagRegex().Replace(stripped, " ");
        var decoded = WebUtility.HtmlDecode(stripped).Trim();
        return decoded.Length <= MaxVisibleTextChars
            ? decoded
            : decoded[..MaxVisibleTextChars];
    }

    [GeneratedRegex("<script\\b[^<]*(?:(?!<\\/script>)<[^<]*)*<\\/script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex("<style\\b[^<]*(?:(?!<\\/style>)<[^<]*)*<\\/style>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleTagRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}

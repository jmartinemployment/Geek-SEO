using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeoBackend.Services.NicheExtraction;

namespace GeekSeoBackend.Services.NicheStepRunners;

internal static partial class NicheStepRelationalLoader
{
    internal const string ServicePhraseKind = "service_phrase";
    internal const string VerticalTopicKind = "vertical_topic";

    internal sealed record MergingInputs(
        SchemaOrgData Schema,
        SitemapData Sitemap,
        NavMenuData Nav,
        HomepageHeadings Headings,
        PageContentData PageContent,
        NicheStepArtifactStore.SiteStructureArtifact Structure);

    internal static async Task<MergingInputs> LoadMergingInputsAsync(
        INicheProfileRepository profileRepo,
        Guid profileId,
        string domain,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
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

    internal static async Task<SchemaOrgData> LoadSchemaAsync(
        INicheProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var signalsResult = await profileRepo.GetSchemaSignalsAsync(profileId, ct);
        if (signalsResult.IsSuccess && signalsResult.Value is { Count: > 0 } signals)
        {
            var fallback = NicheStepArtifactStore.TryGetArtifact<SchemaOrgData>(steps, "schema", "schema");
            return BuildSchemaOrgData(signals, fallback);
        }

        return NicheStepArtifactStore.GetRequiredArtifact<SchemaOrgData>(steps, "schema", "schema");
    }

    internal static async Task<SitemapData> LoadSitemapAsync(
        INicheProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
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
                var fallback = NicheStepArtifactStore.TryGetArtifact<SitemapData>(steps, "site_urls", "site_urls");
                return new SitemapData(
                    fallback?.Pillars ?? [],
                    fallback?.TotalUrlsScanned ?? sitemapUrls.Count,
                    sitemapUrls);
            }
        }

        return NicheStepArtifactStore.GetRequiredArtifact<SitemapData>(steps, "site_urls", "site_urls");
    }

    internal static async Task<NavMenuData> LoadNavAsync(
        INicheProfileRepository profileRepo,
        Guid profileId,
        string domain,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
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

        return NicheStepArtifactStore.GetRequiredArtifact<NavMenuData>(steps, "nav", "nav");
    }

    internal static async Task<HomepageHeadings> LoadHeadingsAsync(
        INicheProfileRepository profileRepo,
        Guid profileId,
        string domain,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var headingsResult = await profileRepo.GetHeadingsAsync(profileId, ct);
        if (headingsResult.IsSuccess && headingsResult.Value is { Count: > 0 } rows)
        {
            var fallback = NicheStepArtifactStore.TryGetArtifact<HomepageHeadings>(steps, "headings", "headings");
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

        return NicheStepArtifactStore.GetRequiredArtifact<HomepageHeadings>(steps, "headings", "headings");
    }

    internal static async Task<PageContentData> LoadPageContentAsync(
        INicheProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
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

        return NicheStepArtifactStore.GetRequiredArtifact<PageContentData>(steps, "page_content", "page_content");
    }

    internal static async Task<NicheStepArtifactStore.SiteStructureArtifact?> TryLoadSiteStructureAsync(
        INicheProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var structureResult = await profileRepo.GetSiteStructureAsync(profileId, ct);
        if (structureResult.IsSuccess && structureResult.Value is { Pages.Count: > 0 } row)
            return BuildSiteStructure(row);

        return TryGetSiteStructureFromStepArtifacts(steps);
    }

    internal static async Task<NicheStepArtifactStore.SiteStructureArtifact> LoadSiteStructureAsync(
        INicheProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
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

    internal static async Task<NicheStepArtifactStore.SiteStructureArtifact> LoadSiteCrawlAsync(
        INicheProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
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

    private static NicheStepArtifactStore.SiteStructureArtifact? TryGetSiteStructureFromStepArtifacts(
        IReadOnlyList<NicheAnalysisStepLogEntry> steps)
    {
        var legacy = NicheStepArtifactStore.TryGetArtifact<NicheStepArtifactStore.SiteStructureArtifact>(
            steps,
            "site_structure",
            "site_structure");
        if (legacy is not null)
            return legacy;

        var crawlArtifact = NicheStepArtifactStore.TryGetArtifact<NicheStepArtifactStore.SiteStructureArtifact>(
            steps,
            "site_crawl",
            "site_crawl");
        var linksArtifact = NicheStepArtifactStore.TryGetArtifact<NicheStepArtifactStore.SiteStructureArtifact>(
            steps,
            "internal_links",
            "internal_links");
        var patternsArtifact = NicheStepArtifactStore.TryGetArtifact<NicheStepArtifactStore.SiteStructureArtifact>(
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

        return new NicheStepArtifactStore.SiteStructureArtifact(
            crawl,
            internalLinks,
            urlPatterns,
            crawledUrls);
    }

    internal static InternalLinkData EmptyInternalLinks(int pagesScanned = 0) =>
        new([], new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), pagesScanned);

    internal static UrlPatternData EmptyUrlPatterns() => new([], 0);

    private static NicheStepArtifactStore.SiteStructureArtifact BuildSiteStructure(
        NicheProfileSiteStructureRow row)
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
        return new NicheStepArtifactStore.SiteStructureArtifact(crawl, internalLinks, urlPatterns, crawledUrls);
    }

    internal static NicheProfilePageContentWrite ToPageContentWrite(string pageUrl, PageContentData data)
    {
        var items = new List<NicheProfilePageContentItemWrite>();
        var order = 0;
        foreach (var phrase in data.ServicePhrases)
            items.Add(new NicheProfilePageContentItemWrite(pageUrl, ServicePhraseKind, phrase, order++));
        foreach (var topic in data.VerticalTopics)
            items.Add(new NicheProfilePageContentItemWrite(pageUrl, VerticalTopicKind, topic, order++));
        return new NicheProfilePageContentWrite(pageUrl, data.ListItemsScanned, items);
    }

    internal static NicheProfileSiteStructureWrite ToSiteStructureWrite(
        SiteCrawlData crawlData,
        InternalLinkData internalLinks,
        UrlPatternData urlPatterns)
    {
        var pages = crawlData.Pages
            .Select((page, index) =>
            {
                var visibleText = ExtractVisibleText(page.Html);
                return new NicheProfileSitePageWrite(
                    page.Url,
                    page.FetchMethod,
                    visibleText,
                    NormalizedTopicalityCalculator.EstimateWordCount(page.Html),
                    index);
            })
            .ToList();

        var links = internalLinks.Links
            .Select((link, index) => new NicheProfileSitePageLinkWrite(
                link.SourceUrl,
                link.TargetUrl,
                link.AnchorText,
                link.InferredFromUrlSlug,
                index))
            .ToList();

        var patterns = urlPatterns.Topics
            .Select((topic, index) => new NicheProfileUrlPatternTopicWrite(
                topic.Name,
                topic.Slug,
                topic.Url,
                topic.PathSegment,
                index))
            .ToList();

        return new NicheProfileSiteStructureWrite(
            pages,
            links,
            patterns,
            new NicheProfileSiteCrawlMetaWrite(crawlData.PagesAttempted, crawlData.PagesFetched));
    }

    private static SchemaOrgData BuildSchemaOrgData(
        IReadOnlyList<NicheProfileSchemaSignalRow> signals,
        SchemaOrgData? fallback)
    {
        static IEnumerable<string> Values(
            IEnumerable<NicheProfileSchemaSignalRow> rows,
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

    private static DiscoveredPillar ToNavPillar(NicheProfileNavigationLinkRow link, string domain)
    {
        var slug = SlugFromUrl(link.LinkUrl) ?? NicheAnalyzerService.NameToSlug(link.AnchorText ?? string.Empty);
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

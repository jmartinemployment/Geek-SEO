using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Services.SiteAnalyzerStepRunners;

internal static class SiteAnalyzerStepRelationalLoader
{
    internal const string ServicePhraseKind = "service_phrase";
    internal const string VerticalTopicKind = "vertical_topic";

    internal sealed record MergingInputs(
        SchemaOrgData Schema,
        SitemapData Sitemap,
        NavMenuData Nav,
        HomepageHeadings Headings,
        PageContentData PageContent,
        SiteAnalyzerStepArtifactStore.SiteStructureArtifact Structure,
        IReadOnlySet<string> ContentBackedHeadingSlugs);

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
        var contentBackedHeadingSlugs = await LoadContentBackedHeadingSlugsAsync(profileRepo, profileId, ct);

        return new MergingInputs(schema, sitemap, nav, headings, pageContent, structure, contentBackedHeadingSlugs);
    }

    /// <summary>
    /// Slugs of every heading (any page, any level) that has real paragraph text of its own in
    /// the persisted <see cref="PageSection"/> tree — the content-backed candidacy signal that
    /// lets a heading-sourced pillar/gap candidate bypass <c>MinPillarConfidence</c> entirely
    /// (see <see cref="PillarSelector"/>), same standing as a schema/GSC-confirmed topic. No
    /// step-log gate here: an empty/missing tree (e.g. profile pre-dates this feature) simply
    /// yields no content-backed slugs, falling back to the existing confidence-gated behavior.
    /// </summary>
    internal static async Task<IReadOnlySet<string>> LoadContentBackedHeadingSlugsAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        CancellationToken ct)
    {
        var trees = await LoadPageSectionTreesAsync(profileRepo, profileId, ct);
        var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, tree) in trees)
            CollectContentBackedSlugs(tree, slugs);
        return slugs;
    }

    /// <summary>
    /// Deserialized per-page section trees for gap detection / content-backed candidacy.
    /// Malformed JSON for a single page is skipped (other pages still usable).
    /// </summary>
    internal static async Task<IReadOnlyList<(string PageUrl, IReadOnlyList<PageSection> Tree)>> LoadPageSectionTreesAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        CancellationToken ct)
    {
        var result = await profileRepo.GetPageSectionTreesAsync(profileId, ct);
        var pages = new List<(string PageUrl, IReadOnlyList<PageSection> Tree)>();
        if (!result.IsSuccess || result.Value is null)
            return pages;

        foreach (var page in result.Value)
        {
            try
            {
                var tree = System.Text.Json.JsonSerializer.Deserialize<List<PageSection>>(page.TreeJson) ?? [];
                pages.Add((page.PageUrl, tree));
            }
            catch (System.Text.Json.JsonException)
            {
                // Skip malformed page; other pages remain usable.
            }
        }

        return pages;
    }

    private static void CollectContentBackedSlugs(IReadOnlyList<PageSection> nodes, HashSet<string> slugs)
    {
        foreach (var node in nodes)
        {
            if (node.HasOwnContent)
            {
                var slug = SiteAnalyzerService.NameToSlug(node.HeadingText);
                if (!string.IsNullOrWhiteSpace(slug))
                    slugs.Add(slug);
            }

            CollectContentBackedSlugs(node.Children, slugs);
        }
    }

    private static void FlattenSectionsToHeadings(IReadOnlyList<PageSection> nodes, List<PageHeading> into)
    {
        foreach (var node in nodes)
        {
            into.Add(new PageHeading { Level = node.Level, Text = node.HeadingText });
            FlattenSectionsToHeadings(node.Children, into);
        }
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
        // A schema step that ran and genuinely found no JSON-LD is a real, common outcome (most
        // sites have none) — not a failure. Distinguish that from "the step never ran" using the
        // step-log entry's existence, not the signal count or the artifact's content.
        if (!steps.Any(s => s.Slug.Equals("schema", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Schema.org signals are not available — the schema step has not completed for this profile.");
        }

        var signalsResult = await profileRepo.GetSchemaSignalsAsync(profileId, ct);
        var signals = signalsResult.IsSuccess ? signalsResult.Value ?? [] : [];
        return BuildSchemaOrgData(signals);
    }

    /// <summary>
    /// Reads the step-1 (sitemap generation) inventory persisted by <c>site_urls</c> —
    /// <c>SourceType ∈ {{sitemap, generated}}</c>. Step 1 always runs first and always persists a
    /// non-empty inventory or throws, so an empty result here means step 1 has not actually run
    /// yet for this profile (a real ordering bug), not a "site has no sitemap" condition — the
    /// old empty soft-success (<c>new SitemapData([], 0, [])</c>) masked that and is vetoed.
    /// </summary>
    internal static async Task<SitemapData> LoadSitemapAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var urlsResult = await profileRepo.GetDiscoveredUrlsAsync(profileId, ct);
        if (urlsResult.IsSuccess && urlsResult.Value is { Count: > 0 } urls)
        {
            var inventoryUrls = urls
                .Where(x =>
                    string.Equals(x.SourceType, "sitemap", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.SourceType, "generated", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Url)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (inventoryUrls.Count > 0)
            {
                // Pillars are derived from the crawl/nav steps elsewhere in the merge pipeline, not
                // from this loader — real discovered URLs are the only thing this method owns.
                return new SitemapData([], inventoryUrls.Count, inventoryUrls);
            }
        }

        throw new InvalidOperationException(
            "Sitemap inventory is not available — step 1 (sitemap generation) has not completed for this profile. " +
            "Re-run Analyze so step 1 can generate and persist the URL inventory.");
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

    /// <summary>
    /// Loads persisted headings for the profile. After site crawl, this is every crawled page
    /// (crawl ReplaceHeadingsAsync supersedes the early homepage-only headings step). The
    /// <see cref="HomepageHeadings"/> wrapper name is historical — <c>Headings</c> is site-wide.
    /// </summary>
    internal static async Task<HomepageHeadings> LoadHeadingsAsync(
        ISiteAnalysisProfileRepository profileRepo,
        Guid profileId,
        string domain,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        // Prefer the real per-page section tree (post-crawl). Flat heading rows are legacy —
        // cleared after crawl writes trees; only used as a fallback for profiles that have not
        // been re-Analyzed since the tree cutover, or for the brief window between the early
        // homepage headings step and site crawl.
        var trees = await LoadPageSectionTreesAsync(profileRepo, profileId, ct);
        if (trees.Count > 0)
        {
            var fromTrees = new List<PageHeading>();
            foreach (var (_, tree) in trees)
                FlattenSectionsToHeadings(tree, fromTrees);

            return new HomepageHeadings
            {
                Title = null,
                MetaDescription = null,
                Headings = fromTrees,
                H2Texts = fromTrees.Where(h => h.Level == 2).Select(h => h.Text).ToList(),
            };
        }

        throw new InvalidOperationException(
            "Heading hierarchy not available — this profile requires re-analysis with the current crawl pipeline to load per-page section trees.");
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
            .Select(x => new CrawledPage(x.Url, x.VisibleText, x.FetchMethod)
            {
                FinalUrl = x.FinalUrl,
                StatusCode = x.StatusCode,
                Canonical = x.Canonical,
                NoIndex = x.NoIndex,
                NoFollow = x.NoFollow,
                RedirectChain = ParseRedirectChain(x.RedirectChainJson),
                FetchedAt = x.FetchedAt,
            })
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
        UrlPatternData urlPatterns,
        bool forceDocumentWrite = true)
    {
        var pages = crawlData.Pages
            .Select((page, index) =>
            {
                var html = page.Html ?? "";
                var context = PageContextBuilder.FromHtml(html);
                var markdown = context.MainContentMarkdown ?? "";
                return new SiteAnalysisProfileSitePageWrite(
                    page.Url,
                    page.FetchMethod,
                    VisibleTextExtractor.Extract(html),
                    VisibleTextExtractor.EstimateWordCount(html),
                    index,
                    context,
                    CrawlDocumentHasher.Sha256Hex(markdown),
                    page.FinalUrl,
                    page.StatusCode,
                    page.Canonical,
                    page.NoIndex,
                    page.NoFollow,
                    JsonSerializer.Serialize(page.RedirectChain),
                    page.FetchedAt == default ? DateTimeOffset.UtcNow : page.FetchedAt);
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
            new SiteAnalysisProfileSiteCrawlMetaWrite(crawlData.PagesAttempted, crawlData.PagesFetched),
            forceDocumentWrite);
    }

    private static IReadOnlyList<string> ParseRedirectChain(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static SchemaOrgData BuildSchemaOrgData(
        IReadOnlyList<SiteAnalysisProfileSchemaSignalRow> signals)
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
        var resolvedPlatforms = SameAsClassifier.ResolvePlatforms(sameAs);

        return new SchemaOrgData(
            serviceNames,
            knowsAbout,
            offerCatalog,
            description,
            brandName,
            areaServed,
            sameAs,
            resolvedPlatforms,
            SameAsClassifier.IsEntityResolved(resolvedPlatforms));
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
}

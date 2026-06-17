using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.NicheExtraction;

namespace GeekSeoBackend.Services;

internal static class NicheAnalysisStepLogBuilder
{
    private const int SampleLimit = 20;

    private static readonly IReadOnlyDictionary<string, string> Titles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["schema"] = "Schema.org",
            ["site_urls"] = "Site URLs",
            ["nav"] = "Navigation",
            ["headings"] = "Homepage headings",
            ["page_content"] = "Page content",
            ["site_crawl"] = "Site crawl",
            ["internal_links"] = "Internal links",
            ["url_patterns"] = "URL patterns",
            ["site_structure"] = "Site structure",
            ["merging"] = "Topic selection",
            ["keywords"] = "Keyword demand",
            ["serp_validation"] = "SERP validation",
            ["profile"] = "Niche profile",
            ["local"] = "Local geography",
            ["coverage"] = "Content coverage",
            ["scoring"] = "Authority score",
            ["complete"] = "Complete",
        };

    internal static NicheAnalysisStepLogEntry Entry(
        int stepNumber,
        string slug,
        string summary,
        IReadOnlyDictionary<string, object?> outputs,
        string status = "complete") =>
        new(
            stepNumber,
            slug,
            Titles.TryGetValue(slug, out var title) ? title : slug,
            status,
            summary,
            outputs);

    internal static NicheAnalysisStepLogEntry Processing(
        int step,
        string slug,
        string summary,
        IReadOnlyDictionary<string, object?>? outputs = null) =>
        Entry(step, slug, summary, outputs ?? new Dictionary<string, object?>(), "processing");

    internal static NicheAnalysisStepLogEntry Schema(int step, SchemaOrgData data, string summary) =>
        Entry(step, "schema", summary, new Dictionary<string, object?>
        {
            ["knowsAboutTopics"] = data.KnowsAboutTopics.Take(SampleLimit).ToArray(),
            ["offerCatalogTopics"] = data.OfferCatalogTopics.Take(SampleLimit).ToArray(),
            ["allSchemaTopics"] = data.ServiceNames.Take(SampleLimit).ToArray(),
            ["areaServed"] = data.AreaServed.Take(SampleLimit).ToArray(),
            ["description"] = data.Description ?? string.Empty,
            ["brandName"] = data.BrandName ?? string.Empty,
            ["sameAsUrls"] = data.SameAsUrls.Take(SampleLimit).ToArray(),
            ["resolvedEntityPlatforms"] = data.ResolvedEntityPlatforms.ToArray(),
            ["entityResolved"] = data.EntityResolved,
            ["becomesPillars"] = true,
        });

    internal static NicheAnalysisStepLogEntry SiteUrls(int step, SitemapData data, string summary) =>
        Entry(step, "site_urls", summary, new Dictionary<string, object?>
        {
            ["totalUrls"] = data.TotalUrlsScanned,
            ["pillarCount"] = data.Pillars.Count,
            ["sampleUrls"] = data.SampleUrls.Take(SampleLimit).ToArray(),
        });

    internal static NicheAnalysisStepLogEntry Nav(int step, NavMenuData data, string summary) =>
        Entry(step, "nav", summary, new Dictionary<string, object?>
        {
            ["extractMethod"] = data.ExtractMethod,
            ["pillarCount"] = data.Pillars.Count,
            ["sampleLabels"] = data.Pillars.Select(p => p.Name).Take(SampleLimit).ToArray(),
        });

    internal static NicheAnalysisStepLogEntry Headings(int step, HomepageHeadings data, string summary) =>
        Entry(step, "headings", summary, new Dictionary<string, object?>
        {
            ["title"] = data.Title ?? string.Empty,
            ["headingCount"] = data.Headings.Count,
            ["sampleHeadings"] = data.Headings
                .Take(SampleLimit)
                .Select(h => $"H{h.Level}: {h.Text}")
                .ToArray(),
        });

    internal static NicheAnalysisStepLogEntry PageContent(int step, PageContentData data, string summary) =>
        Entry(step, "page_content", summary, new Dictionary<string, object?>
        {
            ["verticalTopicCount"] = data.VerticalTopics.Count,
            ["servicePhraseCount"] = data.ServicePhrases.Count,
            ["listItemsScanned"] = data.ListItemsScanned,
            ["sampleVerticalTopics"] = data.VerticalTopics.Take(SampleLimit).ToArray(),
            ["sampleServicePhrases"] = data.ServicePhrases.Take(SampleLimit).ToArray(),
            ["becomesPillars"] = true,
        });

    internal static NicheAnalysisStepLogEntry SiteCrawl(int step, SiteCrawlData crawl, string summary) =>
        Entry(step, "site_crawl", summary, new Dictionary<string, object?>
        {
            ["pagesCrawled"] = crawl.PagesFetched,
            ["pagesAttempted"] = crawl.PagesAttempted,
            ["crawlStopReason"] = CrawlStopReason(crawl),
            ["sampleCrawledUrls"] = crawl.Pages
                .Take(SampleLimit)
                .Select(p => new Dictionary<string, object?>
                {
                    ["url"] = p.Url,
                    ["fetchMethod"] = p.FetchMethod,
                    ["outboundLinkCount"] = 0,
                })
                .ToArray(),
            ["becomesPillars"] = true,
        });

    internal static NicheAnalysisStepLogEntry InternalLinks(
        int step,
        SiteCrawlData crawl,
        InternalLinkData internalLinks,
        string summary) =>
        Entry(step, "internal_links", summary, new Dictionary<string, object?>
        {
            ["pagesCrawled"] = crawl.PagesFetched,
            ["internalLinkCount"] = internalLinks.Links.Count,
            ["internalLinkAnchorCount"] = internalLinks.Links.Count(l => !l.InferredFromUrlSlug),
            ["internalLinkSlugInferredCount"] = internalLinks.Links.Count(l => l.InferredFromUrlSlug),
            ["sampleInternalAnchors"] = internalLinks.Links
                .Select(l => l.AnchorText)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(SampleLimit)
                .ToArray(),
            ["sampleCrawledUrls"] = crawl.Pages
                .Take(SampleLimit)
                .Select(p =>
                {
                    var outbound = internalLinks.Links.Count(l =>
                        l.SourceUrl.Equals(p.Url, StringComparison.OrdinalIgnoreCase));
                    return new Dictionary<string, object?>
                    {
                        ["url"] = p.Url,
                        ["fetchMethod"] = p.FetchMethod,
                        ["outboundLinkCount"] = outbound,
                    };
                })
                .ToArray(),
            ["becomesPillars"] = true,
        });

    internal static NicheAnalysisStepLogEntry UrlPatterns(int step, UrlPatternData urlPatterns, string summary) =>
        Entry(step, "url_patterns", summary, new Dictionary<string, object?>
        {
            ["urlPatternTopicCount"] = urlPatterns.Topics.Count,
            ["sampleUrlPatterns"] = urlPatterns.Topics
                .Select(t => t.Name)
                .Take(SampleLimit)
                .ToArray(),
            ["becomesPillars"] = true,
        });

    internal static NicheAnalysisStepLogEntry SiteStructure(
        int step,
        SiteCrawlData crawl,
        InternalLinkData internalLinks,
        UrlPatternData urlPatterns,
        string summary) =>
        Entry(step, "site_structure", summary, new Dictionary<string, object?>
        {
            ["pagesCrawled"] = crawl.PagesFetched,
            ["pagesAttempted"] = crawl.PagesAttempted,
            ["internalLinkCount"] = internalLinks.Links.Count,
            ["internalLinkAnchorCount"] = internalLinks.Links.Count(l => !l.InferredFromUrlSlug),
            ["internalLinkSlugInferredCount"] = internalLinks.Links.Count(l => l.InferredFromUrlSlug),
            ["urlPatternTopicCount"] = urlPatterns.Topics.Count,
            ["sampleInternalAnchors"] = internalLinks.Links
                .Select(l => l.AnchorText)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(SampleLimit)
                .ToArray(),
            ["sampleUrlPatterns"] = urlPatterns.Topics
                .Select(t => t.Name)
                .Take(SampleLimit)
                .ToArray(),
            ["crawlStopReason"] = CrawlStopReason(crawl),
            ["sampleCrawledUrls"] = crawl.Pages
                .Take(SampleLimit)
                .Select(p =>
                {
                    var outbound = internalLinks.Links.Count(l =>
                        l.SourceUrl.Equals(p.Url, StringComparison.OrdinalIgnoreCase));
                    return new Dictionary<string, object?>
                    {
                        ["url"] = p.Url,
                        ["fetchMethod"] = p.FetchMethod,
                        ["outboundLinkCount"] = outbound,
                    };
                })
                .ToArray(),
            ["becomesPillars"] = true,
        });

    internal static NicheAnalysisStepLogEntry Merging(
        int step,
        int candidateCount,
        int mergedCount,
        IReadOnlyList<DiscoveredPillar> merged,
        int fromSchema,
        int fromSitemap,
        int fromNav,
        int fromHeadings,
        IReadOnlyList<DiscoveredPillar> excludedTopics,
        int fromPageContent,
        int fromPageVertical,
        int fromInternalLink,
        int fromUrlPattern,
        int fromSameAs,
        int fromGsc,
        string fusionVersion,
        IReadOnlyList<string> signalSourcesPresent,
        IReadOnlyList<string> exclusionReasonsSample,
        bool gscConnected,
        bool gscSkipped,
        string? gscSkipReason,
        int gscQueryRowCount,
        int gscMatchedPillars,
        IReadOnlyList<string> gscSilentPillarSlugs,
        IReadOnlyDictionary<string, decimal> normalizedTopicalityBySlug,
        string summary) =>
        Entry(step, "merging", summary, new Dictionary<string, object?>
        {
            ["candidateCount"] = candidateCount,
            ["mergedCount"] = mergedCount,
            ["fromSchema"] = fromSchema,
            ["fromSitemap"] = fromSitemap,
            ["fromNav"] = fromNav,
            ["fromHeadings"] = fromHeadings,
            ["fromPageContent"] = fromPageContent,
            ["fromPageVertical"] = fromPageVertical,
            ["fromInternalLink"] = fromInternalLink,
            ["fromUrlPattern"] = fromUrlPattern,
            ["fromSameAs"] = fromSameAs,
            ["fromGsc"] = fromGsc,
            ["gscConnected"] = gscConnected,
            ["gscSkipped"] = gscSkipped,
            ["gscSkipReason"] = gscSkipReason ?? string.Empty,
            ["gscQueryRowCount"] = gscQueryRowCount,
            ["gscMatchedPillars"] = gscMatchedPillars,
            ["gscSilentPillarSlugs"] = gscSilentPillarSlugs.Take(SampleLimit).ToArray(),
            ["fusionVersion"] = fusionVersion,
            ["signalSourcesPresent"] = signalSourcesPresent.ToArray(),
            ["excludedCount"] = excludedTopics.Count,
            ["excludedSampleNames"] = excludedTopics.Select(p => p.Name).Take(SampleLimit).ToArray(),
            ["exclusionReasonsSample"] = exclusionReasonsSample.Take(SampleLimit).ToArray(),
            ["primarySource"] = DescribePrimarySource(merged),
            ["samplePillarNames"] = merged.Select(p => p.Name).Take(SampleLimit).ToArray(),
            ["pillarSources"] = merged
                .Take(SampleLimit)
                .Select(p => $"{p.Name} ({p.Source})")
                .ToArray(),
            ["normalizedTopicalitySample"] = normalizedTopicalityBySlug
                .OrderByDescending(kv => kv.Value)
                .Take(SampleLimit)
                .Select(kv => $"{kv.Key}: {kv.Value:P0}")
                .ToArray(),
        });

    private static string DescribePrimarySource(IReadOnlyList<DiscoveredPillar> merged)
    {
        if (merged.Count == 0) return "none";
        var sources = merged.GroupBy(p => p.Source).OrderByDescending(g => g.Count()).ToList();
        if (sources.Count == 1) return sources[0].Key;
        return $"mixed ({string.Join(", ", sources.Select(g => $"{g.Key}:{g.Count()}"))})";
    }

    internal static NicheAnalysisStepLogEntry Keywords(
        int step,
        PillarDemandEnrichment demand,
        string summary) =>
        Entry(step, "keywords", summary, new Dictionary<string, object?>
        {
            ["skipped"] = demand.KeywordsSkipped,
            ["skipReason"] = demand.KeywordSkipReason ?? string.Empty,
            ["provider"] = demand.KeywordProvider,
            ["pillarsEnriched"] = demand.Keywords.Count(k => k.Enriched),
            ["pillarsAttempted"] = demand.Keywords.Count,
            ["sampleMetrics"] = demand.Keywords
                .Where(k => k.Enriched)
                .Take(SampleLimit)
                .Select(k => $"{k.Keyword}: vol {k.SearchVolume}, KD {k.KeywordDifficulty:F0}")
                .ToArray(),
        });

    internal static NicheAnalysisStepLogEntry SerpValidation(
        int step,
        PillarDemandEnrichment demand,
        string summary) =>
        Entry(step, "serp_validation", summary, new Dictionary<string, object?>
        {
            ["skipped"] = demand.SerpSkipped,
            ["skipReason"] = demand.SerpSkipReason ?? string.Empty,
            ["provider"] = demand.SerpProvider,
            ["pillarsValidated"] = demand.SerpValidations.Count(v => string.IsNullOrEmpty(v.Error)),
            ["pillarsWithFootprint"] = demand.SerpValidations.Count(v => v.HasSerpFootprint),
            ["pillarsDemoted"] = demand.DemotedSlugs.Count,
            ["demotedSample"] = demand.DemotedSlugs.Take(SampleLimit).ToArray(),
            ["competitorCount"] = demand.Competitors.Count,
            ["localSerpLocation"] = demand.LocalSerpStats?.Location ?? string.Empty,
            ["localSerpAttempted"] = demand.LocalSerpStats?.Attempted ?? 0,
            ["localSerpSucceeded"] = demand.LocalSerpStats?.Succeeded ?? 0,
            ["localSerpFailed"] = demand.LocalSerpStats?.Failed ?? 0,
            ["localSerpEmpty"] = demand.LocalSerpStats?.Empty ?? 0,
            ["localSerpFirstError"] = demand.LocalSerpStats?.FirstError ?? string.Empty,
            ["localScopedCompetitors"] = demand.Competitors.Count(c =>
                string.Equals(c.Scope, "local", StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.Scope, "both", StringComparison.OrdinalIgnoreCase)),
            ["sampleCompetitors"] = demand.Competitors
                .Take(SampleLimit)
                .Select(c => $"{c.Domain} ({c.SerpPresence} SERPs)")
                .ToArray(),
            ["siteRanksCount"] = demand.SerpValidations.Count(v => v.SiteRanks),
        });

    internal static NicheAnalysisStepLogEntry Profile(
        int step, string primaryNiche, string audienceType, IEnumerable<string> nicheTags, string summary) =>
        Entry(step, "profile", summary, new Dictionary<string, object?>
        {
            ["primaryNiche"] = primaryNiche,
            ["audienceType"] = audienceType,
            ["nicheTags"] = nicheTags.Take(SampleLimit).ToArray(),
        });

    internal static NicheAnalysisStepLogEntry LocalDisabled(int step, string summary) =>
        Entry(step, "local", summary, new Dictionary<string, object?>
        {
            ["enabled"] = false,
            ["message"] = summary,
        });

    internal static NicheAnalysisStepLogEntry Local(
        int step,
        LocalGeographyAnalysis local,
        string summary) =>
        Entry(step, "local", summary, new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["isLocalBusiness"] = local.IsLocalBusiness,
            ["areasServed"] = local.AreasServed.Take(SampleLimit).ToArray(),
            ["locationPageCount"] = local.LocationPagesFound.Count,
            ["sampleLocationPages"] = local.LocationPagesFound
                .Take(SampleLimit)
                .Select(p => $"{p.Name} ({p.Url})")
                .ToArray(),
            ["localGapCount"] = local.Gaps.Count,
            ["sampleLocalGaps"] = local.Gaps
                .Take(SampleLimit)
                .Select(g => g.AreaName)
                .ToArray(),
        });

    internal static NicheAnalysisStepLogEntry CoverageDisabled(int step, string summary) =>
        Entry(step, "coverage", summary, new Dictionary<string, object?>
        {
            ["enabled"] = false,
            ["message"] = summary,
        });

    internal static NicheAnalysisStepLogEntry Coverage(
        int step,
        int pillarsCovered,
        int pillarsPartial,
        int pillarsGap,
        int subtopicsCovered,
        int subtopicsTotal,
        IReadOnlyList<string> samplePartialPillars,
        string summary) =>
        Entry(step, "coverage", summary, new Dictionary<string, object?>
        {
            ["enabled"] = true,
            ["pillarsCovered"] = pillarsCovered,
            ["pillarsPartial"] = pillarsPartial,
            ["pillarsGap"] = pillarsGap,
            ["subtopicsCovered"] = subtopicsCovered,
            ["subtopicsTotal"] = subtopicsTotal,
            ["samplePartialPillars"] = samplePartialPillars.Take(SampleLimit).ToArray(),
        });

    internal static NicheAnalysisStepLogEntry Scoring(
        int step,
        decimal authorityScore,
        int covered,
        int partial,
        int gap,
        int pillarCount,
        string summary,
        int entityThinCount = 0,
        int linkGraphEdgeCount = 0,
        int orphanPillarCount = 0,
        int recommendedActionCount = 0) =>
        Entry(step, "scoring", summary, new Dictionary<string, object?>
        {
            ["authorityScore"] = authorityScore,
            ["covered"] = covered,
            ["partial"] = partial,
            ["gap"] = gap,
            ["pillarCount"] = pillarCount,
            ["entityThinCount"] = entityThinCount,
            ["linkGraphEdgeCount"] = linkGraphEdgeCount,
            ["orphanPillarCount"] = orphanPillarCount,
            ["recommendedActionCount"] = recommendedActionCount,
        });

    internal static NicheAnalysisStepLogEntry Complete(
        int step, DateTimeOffset analyzedAt, DateTimeOffset nextDue, string summary) =>
        Entry(step, "complete", summary, new Dictionary<string, object?>
        {
            ["analyzedAt"] = analyzedAt,
            ["nextAnalysisDue"] = nextDue,
        });

    private static string CrawlStopReason(SiteCrawlData crawl) =>
        crawl.PagesFetched >= 20
            ? "Reached max 20 pages"
            : "Queue exhausted (no more same-origin links)";
}

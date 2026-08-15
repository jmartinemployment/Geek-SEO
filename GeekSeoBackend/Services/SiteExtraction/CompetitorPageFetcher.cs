using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Crawls competitor sites (uncapped BFS, same unlimited <see cref="SitePageCrawler"/> rules
/// as the own-site crawl) and extracts topic signals — headings, word count, FAQ schema.
/// Deduplicates by domain so each competitor is crawled once across all pillars.
/// </summary>
public sealed class CompetitorPageFetcher(
    SitePageCrawler crawler,
    ILogger<CompetitorPageFetcher> logger)
{
    public async Task<Dictionary<string, CompetitorSiteInsight>> CrawlCompetitorsAsync(
        IEnumerable<string> domains,
        IBrowser? browser,
        CancellationToken ct)
    {
        var results = new Dictionary<string, CompetitorSiteInsight>(StringComparer.OrdinalIgnoreCase);
        var unique = domains.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var domain in unique)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var siteUrl = domain.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? domain : $"https://{domain}";

                logger.LogDebug("Crawling competitor site: {Domain}", domain);
                var crawl = await crawler.CrawlAsync(siteUrl, [], browser, ct);

                var allHeadings = new List<string>();
                var totalWords = 0;
                var hasFaqSchema = false;
                var pageCount = crawl.Pages.Count;

                var allServices = new List<string>();
                var allKnowsAbout = new List<string>();
                var allAreaServed = new List<string>();
                var allSameAs = new List<string>();
                var siteDescription = (string?)null;
                var siteBrand = (string?)null;

                foreach (var page in crawl.Pages)
                {
                    allHeadings.AddRange(ExtractHeadings(page.Html));
                    totalWords += VisibleTextExtractor.EstimateWordCount(page.Html);
                    if (!hasFaqSchema) hasFaqSchema = HasFaqSchema(page.Html);

                    var schema = SchemaOrgExtractor.ParseFromHtml(page.Html);
                    allServices.AddRange(schema.ServiceNames);
                    allKnowsAbout.AddRange(schema.KnowsAboutTopics);
                    allAreaServed.AddRange(schema.AreaServed);
                    allSameAs.AddRange(schema.SameAsUrls);
                    siteDescription ??= schema.Description;
                    siteBrand ??= schema.BrandName;
                }

                var avgWordCount = pageCount > 0 ? totalWords / pageCount : 0;
                var topHeadings = allHeadings
                    .Where(h => !string.IsNullOrWhiteSpace(h))
                    .GroupBy(h => h, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .Take(30)
                    .ToList();

                results[domain] = new CompetitorSiteInsight(
                    Domain: domain,
                    PagesCrawled: pageCount,
                    AvgWordCount: avgWordCount,
                    TopHeadings: topHeadings,
                    HasFaqSchema: hasFaqSchema,
                    Services: allServices.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    KnowsAbout: allKnowsAbout.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    AreaServed: allAreaServed.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    SameAs: allSameAs.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    Description: siteDescription,
                    BrandName: siteBrand);

                logger.LogInformation("Competitor {Domain}: {Pages} pages, {Words} avg words, {Headings} unique headings",
                    domain, pageCount, avgWordCount, topHeadings.Count);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Competitor crawl failed for {Domain}", domain);
                results[domain] = new CompetitorSiteInsight(domain, 0, 0, [], false, "national");
            }
        }

        return results;
    }

    private static List<string> ExtractHeadings(string html)
    {
        var list = new List<string>();
        Flatten(PageSectionTreeBuilder.Build(html), list);
        return list;
    }

    private static void Flatten(IReadOnlyList<PageSection> nodes, List<string> list)
    {
        foreach (var node in nodes)
        {
            list.Add(node.HeadingText);
            Flatten(node.Children, list);
        }
    }

    private static bool HasFaqSchema(string html) =>
        html.Contains("FAQPage", StringComparison.OrdinalIgnoreCase) ||
        html.Contains("\"@type\":\"Question\"", StringComparison.OrdinalIgnoreCase);
}

public sealed record CompetitorSiteInsight(
    string Domain,
    int PagesCrawled,
    int AvgWordCount,
    IReadOnlyList<string> TopHeadings,
    bool HasFaqSchema,
    string Scope = "national",
    IReadOnlyList<string>? Services = null,
    IReadOnlyList<string>? KnowsAbout = null,
    IReadOnlyList<string>? AreaServed = null,
    IReadOnlyList<string>? SameAs = null,
    string? Description = null,
    string? BrandName = null);

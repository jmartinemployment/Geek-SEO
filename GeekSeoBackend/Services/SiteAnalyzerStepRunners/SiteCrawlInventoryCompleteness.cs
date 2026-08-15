using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Services.SiteAnalyzerStepRunners;

/// <summary>
/// Site crawl succeeds when at least one page was fetched. Unmatched inventory/sitemap URLs
/// are warnings, not an abort.
/// </summary>
internal static class SiteCrawlInventoryCompleteness
{
    public static string Evaluate(
        IReadOnlyList<string> inventoryUrls,
        IReadOnlySet<string> fetchedUrls,
        out IReadOnlyList<string> missing)
    {
        var fetchedKeys = new HashSet<string>(
            fetchedUrls.Select(CrawlUrl.Canonicalize),
            StringComparer.OrdinalIgnoreCase);
        var inventoryKeys = inventoryUrls
            .Select(CrawlUrl.Canonicalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        missing = inventoryKeys.Where(u => !fetchedKeys.Contains(u)).ToList();
        var inventoryFetched = inventoryKeys.Count - missing.Count;

        if (fetchedKeys.Count == 0)
        {
            throw new InvalidOperationException(
                "Site crawl fetched no pages. The site may be unreachable, block this crawler, or Playwright/Chromium is unavailable.");
        }

        if (missing.Count == 0)
        {
            return inventoryKeys.Count == 0
                ? $"Site crawl: {fetchedKeys.Count} page(s) fetched."
                : $"Site crawl: {inventoryFetched} of {inventoryKeys.Count} inventory page(s) fetched (complete).";
        }

        return
            $"Site crawl: {inventoryFetched} of {inventoryKeys.Count} inventory page(s) fetched ({missing.Count} not fetched).";
    }
}

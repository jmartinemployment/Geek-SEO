namespace GeekSeoBackend.Services.SiteAnalyzerStepRunners;

/// <summary>
/// Inventory completeness for site_crawl: missing URLs (soft-404 / noindex / unreachable)
/// are "no page here," not a crawl abort. Hard-fail only when nothing from inventory was fetched.
/// </summary>
internal static class SiteCrawlInventoryCompleteness
{
    public static string Evaluate(
        IReadOnlyList<string> inventoryUrls,
        IReadOnlySet<string> fetchedUrls,
        out IReadOnlyList<string> missing)
    {
        missing = inventoryUrls.Where(u => !fetchedUrls.Contains(u)).ToList();
        var inventoryFetched = inventoryUrls.Count - missing.Count;

        if (inventoryUrls.Count > 0 && inventoryFetched == 0)
        {
            var sample = string.Join(", ", inventoryUrls.Take(10));
            var suffix = inventoryUrls.Count > 10 ? $" (+{inventoryUrls.Count - 10} more)" : string.Empty;
            throw new InvalidOperationException(
                $"Site crawl incomplete: 0 of {inventoryUrls.Count} inventory URL(s) were fetched: {sample}{suffix}");
        }

        if (missing.Count == 0)
        {
            return inventoryUrls.Count == 0
                ? "Site crawl: no inventory URLs; crawl finished."
                : $"Site crawl: {inventoryFetched} of {inventoryUrls.Count} inventory page(s) fetched (complete).";
        }

        return
            $"Site crawl: {inventoryFetched} of {inventoryUrls.Count} inventory page(s) fetched ({missing.Count} excluded as unreachable/soft-404/noindex).";
    }
}

namespace SiteAnalyzer2.Services.CompetitorCrawl;

public static class CompetitorCrawlStatusMessages
{
    public static string BuildSavedPagesMessage(int totalPages, int domainCount, bool researchPackReady) =>
        researchPackReady
            ? $"Saved {totalPages} pages across {domainCount} competitor domains. Research pack ready."
            : $"Saved {totalPages} pages across {domainCount} competitor domains. Research pack assembly did not complete.";

    public static string ResolveStatusMessage(
        int totalPages,
        int domainCount,
        bool researchPackReady,
        string? storedMessage)
    {
        if (researchPackReady)
            return BuildSavedPagesMessage(totalPages, domainCount, researchPackReady: true);

        if (totalPages > 0)
        {
            if (!string.IsNullOrWhiteSpace(storedMessage)
                && !storedMessage.Contains("Research pack ready", StringComparison.OrdinalIgnoreCase))
            {
                return storedMessage.Trim();
            }

            return BuildSavedPagesMessage(totalPages, domainCount, researchPackReady: false);
        }

        return string.IsNullOrWhiteSpace(storedMessage)
            ? "Competitor crawl has not started."
            : storedMessage.Trim();
    }
}

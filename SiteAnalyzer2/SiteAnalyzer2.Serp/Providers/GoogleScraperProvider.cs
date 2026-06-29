using SiteAnalyzer2.Serp.Models;

namespace SiteAnalyzer2.Serp.Providers;

public class GoogleScraperProvider(HttpClient httpClient) : ISerpProvider
{
    public string ProviderKey => "google-scraper";

    public Task<SerpResultSet> FetchOrganicResultsAsync(SerpQuery query, CancellationToken ct = default)
    {
        _ = httpClient;
        throw new NotSupportedException(
            "Automated Google scraping is disabled. Save the SERP HTML in Chrome and POST it to /runs/{id}/serp/import-html.");
    }
}

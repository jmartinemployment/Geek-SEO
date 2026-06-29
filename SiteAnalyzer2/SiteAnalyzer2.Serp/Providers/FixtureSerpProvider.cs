using SiteAnalyzer2.Serp.Models;

namespace SiteAnalyzer2.Serp.Providers;

public class FixtureSerpProvider : ISerpProvider
{
    public string ProviderKey => "fixture";

    public Task<SerpResultSet> FetchOrganicResultsAsync(SerpQuery query, CancellationToken ct = default)
    {
        throw new NotSupportedException(
            "Fixture provider is replaced by inline HTML import from tests/fixtures/serp/*.html.");
    }
}

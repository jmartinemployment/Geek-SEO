using SiteAnalyzer2.Serp.Models;

namespace SiteAnalyzer2.Serp;

public interface ISerpProvider
{
    string ProviderKey { get; }
    Task<SerpResultSet> FetchOrganicResultsAsync(SerpQuery query, CancellationToken ct = default);
}

using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Serp;
using SiteAnalyzer2.Serp.Models;

namespace SiteAnalyzer2.Services.Pipeline;

/// <summary>
/// Inline dev/test SERP discovery via fixture HTML or legacy providers.
/// </summary>
public class SerpDiscoveryService(SerpHtmlImportService htmlImport)
{
    public async Task<SerpImportOutcome> ImportFixtureHtmlAsync(AnalysisRun run, string html, CancellationToken ct = default)
        => await htmlImport.ImportHtmlAsync(run, html, run.Keyword, ct);
}

using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.SiteAnalyzerStepRunners;

public interface ISiteAnalysisStepRunner
{
    SiteAnalysisStepDefinition Definition { get; }
    string Slug { get; }
    Task<SiteAnalysisStepLogEntry> RunAsync(
        Guid profileId,
        Guid userId,
        string domain,
        IBrowser? browser,
        CancellationToken ct);
}

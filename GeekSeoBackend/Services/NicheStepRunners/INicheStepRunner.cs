using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.NicheStepRunners;

public interface INicheStepRunner
{
    NicheStepDefinition Definition { get; }
    string Slug { get; }
    Task<NicheAnalysisStepLogEntry> RunAsync(
        Guid profileId,
        Guid userId,
        string domain,
        IBrowser? browser,
        CancellationToken ct);
}

using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Services.LocalServiceArea;

namespace GeekSeoBackend.Services;

public sealed class UrlResearchAnalyzeService(
    ISerpResearchPackService packService,
    ILocalSerpContextResolver localSerp) : IUrlResearchAnalyzeRunner
{
    public async Task<Result<UrlResearchFullWrite>> BuildFullWriteAsync(
        Guid userId,
        Guid projectId,
        string sourceUrl,
        CancellationToken ct = default)
    {
        var ctx = await localSerp.ResolveAsync(projectId, ct);
        if (!ctx.IsSuccess || ctx.Value is null)
            return Result<UrlResearchFullWrite>.Failure(ctx.Error ?? "Could not resolve SERP location for project.");

        var pack = await packService.BuildAsync(
            userId,
            new UrlAnalyzerResearchRequest
            {
                Url = sourceUrl,
                Location = ctx.Value.SerpMarketLocation,
            },
            ct);

        if (!pack.IsSuccess || pack.Value is null)
            return Result<UrlResearchFullWrite>.Failure(pack.Error ?? "SERP research failed");

        return Result<UrlResearchFullWrite>.Success(
            UrlResearchPackMapper.ToFullWrite(pack.Value, status: "completed"));
    }
}

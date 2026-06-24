using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public static class ResearchBackedWriteGate
{
    public static bool IsResearchBacked(SeoContentDocument document) =>
        document.AnalysisRunId is not null;

    public static Result ValidateAnalysisRunExport(ContentWriterSerpExport export)
    {
        if (string.Equals(export.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            return Result.Failure("Analysis run failed — SERP data is not available for Content Writing.");

        var organicCount = export.Serp.Count(i =>
            string.Equals(i.Type, "organic", StringComparison.OrdinalIgnoreCase));

        if (organicCount == 0)
            return Result.Failure("Analysis run has no organic SERP results yet.");

        return Result.Success();
    }

    public static Result ForbidLiveSerp(string operation) =>
        Result.Failure($"Live SERP is forbidden for research-backed documents during {operation}.");
}

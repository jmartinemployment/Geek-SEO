using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public static class ResearchBackedWriteGate
{
    public static bool IsResearchBacked(SeoContentDocument document) =>
        document.UrlResearchId is not null;

    public static Result EnsureResearchReady(SeoContentDocument document, SeoUrlResearch? research)
    {
        if (!IsResearchBacked(document))
            return Result.Failure("Attach page research from URL Analyzer first.");

        if (research is null || research.Id != document.UrlResearchId)
            return Result.Failure("Page research not found.");

        return ValidateResearchForProject(document.ProjectId, research);
    }

    /// <summary>Shared rules for PATCH attach and POST create with <c>urlResearchId</c>.</summary>
    public static Result ValidateResearchForProject(Guid projectId, SeoUrlResearch research)
    {
        if (research.ProjectId != projectId)
            return Result.Failure("Page research belongs to a different project.");

        if (!string.Equals(research.Status, "completed", StringComparison.OrdinalIgnoreCase))
            return Result.Failure("Page research is not complete yet. Wait for analyze to finish.");

        return Result.Success();
    }

    public static Result ForbidLiveSerp(string operation) =>
        Result.Failure($"Live SERP is forbidden for research-backed documents during {operation}.");
}

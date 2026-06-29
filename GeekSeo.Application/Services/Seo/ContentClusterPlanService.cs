using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentClusterPlanService(
    IContentDocumentService documents,
    IContentDocumentRepository documentRepo) : IContentClusterPlanService
{
    public async Task<Result<ContentLinkPlan>> GetAsync(
        Guid userId, Guid documentId, CancellationToken ct = default)
    {
        var access = await RequirePlanHostAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ContentLinkPlan>.Failure(access.Error ?? "Access denied");

        return Result<ContentLinkPlan>.Success(ContentLinkPlanJson.Parse(access.Value.LinkPlanJson));
    }

    public async Task<Result<ContentLinkPlan>> SaveAsync(
        Guid userId, Guid documentId, ContentLinkPlan plan, CancellationToken ct = default)
    {
        if (plan is null)
            return Result<ContentLinkPlan>.Failure("Plan body is required.");

        var access = await RequirePlanHostAsync(userId, documentId, ct);
        if (!access.IsSuccess)
            return Result<ContentLinkPlan>.Failure(access.Error ?? "Access denied");

        var saved = await documentRepo.UpdateLinkPlanAsync(documentId, ContentLinkPlanJson.Serialize(plan), ct);
        if (!saved.IsSuccess || saved.Value is null)
            return Result<ContentLinkPlan>.Failure(saved.Error ?? "Failed to save link plan");

        return Result<ContentLinkPlan>.Success(ContentLinkPlanJson.Parse(saved.Value.LinkPlanJson));
    }

    private async Task<Result<GeekSeo.Persistence.Entities.SeoContentDocument>> RequirePlanHostAsync(
        Guid userId, Guid documentId, CancellationToken ct)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return access;

        if (string.Equals(access.Value.DocumentKind, ContentDocumentKinds.Spoke, StringComparison.OrdinalIgnoreCase))
            return Result<GeekSeo.Persistence.Entities.SeoContentDocument>.Failure(
                "Link plan is stored on pillar documents only.");

        return access;
    }
}

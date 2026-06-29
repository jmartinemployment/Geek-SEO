using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentClusterPlanService(
    IContentDocumentService documents,
    IContentDocumentRepository documentRepo,
    WritingResearchContextLoader researchLoader) : IContentClusterPlanService
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

        var saved = await PersistLinkPlanAsync(documentId, plan, ct);
        if (!saved.IsSuccess)
            return Result<ContentLinkPlan>.Failure(saved.Error ?? "Failed to save link plan");

        return saved;
    }

    public async Task<Result<ContentClusterPlanResult>> BuildAsync(
        Guid userId, Guid documentId, CancellationToken ct = default)
    {
        var access = await RequirePlanHostAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ContentClusterPlanResult>.Failure(access.Error ?? "Access denied");

        var doc = access.Value;
        var researchResult = await researchLoader.LoadAsync(userId, doc, ct);
        if (!researchResult.IsSuccess || researchResult.Value is null)
            return Result<ContentClusterPlanResult>.Failure(researchResult.Error ?? "Research context is not available.");

        var built = ContentClusterLinkPlanner.Plan(new ContentClusterPlannerInput
        {
            PillarKeyword = doc.TargetKeyword,
            Research = researchResult.Value,
            SiteFocus = researchResult.Value.SiteFocus
                ?? SiteWritingFocusSerializer.TryDeserialize(doc.SiteFocusJson),
        });

        var persisted = await PersistLinkPlanAsync(
            documentId,
            new ContentLinkPlan
            {
                FaqItems = built.FaqItems,
                BodyLinks = [],
            },
            ct);

        if (!persisted.IsSuccess)
            return Result<ContentClusterPlanResult>.Failure(persisted.Error ?? "Failed to save link plan");

        return Result<ContentClusterPlanResult>.Success(built);
    }

    private async Task<Result<ContentLinkPlan>> PersistLinkPlanAsync(
        Guid documentId, ContentLinkPlan plan, CancellationToken ct)
    {
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

using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentBodyLinkService(
    IContentDocumentService documents,
    IContentDocumentRepository documentRepo,
    IContentBlogSpokeMigrator migrator) : IContentBodyLinkService
{
    public async Task<Result<ApplyBodyLinksResponse>> ApplyAsync(
        Guid userId,
        Guid documentId,
        ApplyBodyLinksRequest request,
        CancellationToken ct = default)
    {
        request ??= new ApplyBodyLinksRequest();

        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ApplyBodyLinksResponse>.Failure(access.Error ?? "Access denied");

        var doc = access.Value;
        if (string.Equals(doc.DocumentKind, ContentDocumentKinds.Spoke, StringComparison.OrdinalIgnoreCase))
            return Result<ApplyBodyLinksResponse>.Failure("Body links can only be applied on a pillar document.");

        if (string.IsNullOrWhiteSpace(doc.ContentHtml))
            return Result<ApplyBodyLinksResponse>.Failure("Document has no body HTML to update.");

        var instructions = request.Instructions;
        var pendingCount = 0;

        if (instructions.Count == 0)
        {
            var fromPlan = await BuildInstructionsFromPlanAsync(userId, documentId, doc, ct);
            if (!fromPlan.IsSuccess)
                return Result<ApplyBodyLinksResponse>.Failure(fromPlan.Error ?? "Failed to load body link plan");

            instructions = fromPlan.Value.Instructions;
            pendingCount = fromPlan.Value.PendingCount;

            if (instructions.Count == 0)
            {
                return Result<ApplyBodyLinksResponse>.Failure(
                    "Build and save a cluster link plan with body link slots first.");
            }
        }

        var (updatedHtml, appliedCount) = ContentBodyLinkInserter.ApplyBodyLinks(doc.ContentHtml, instructions);
        var changed = !string.Equals(updatedHtml, doc.ContentHtml, StringComparison.Ordinal);

        if (!changed)
        {
            return Result<ApplyBodyLinksResponse>.Success(new ApplyBodyLinksResponse
            {
                ContentHtml = doc.ContentHtml,
                AppliedCount = 0,
                PendingCount = pendingCount,
                Changed = false,
            });
        }

        var saved = await documents.UpdateContentAsync(
            userId,
            documentId,
            new UpdateContentRequest { ContentHtml = updatedHtml },
            ct);

        if (!saved.IsSuccess || saved.Value is null)
            return Result<ApplyBodyLinksResponse>.Failure(saved.Error ?? "Failed to save updated content.");

        return Result<ApplyBodyLinksResponse>.Success(new ApplyBodyLinksResponse
        {
            ContentHtml = saved.Value.ContentHtml,
            AppliedCount = appliedCount,
            PendingCount = pendingCount,
            Changed = true,
        });
    }

    private async Task<Result<BodyLinkPlanResolution>> BuildInstructionsFromPlanAsync(
        Guid userId,
        Guid pillarDocumentId,
        GeekSeo.Persistence.Entities.SeoContentDocument pillar,
        CancellationToken ct)
    {
        var plan = ContentLinkPlanJson.Parse(pillar.LinkPlanJson);
        if (plan.BodyLinks.Count == 0)
            return Result<BodyLinkPlanResolution>.Success(new BodyLinkPlanResolution([], 0));

        await migrator.EnsureMigratedChildAsync(userId, pillarDocumentId, ct);

        var projectDocs = await documentRepo.GetByProjectAsync(pillar.ProjectId, ct);
        if (!projectDocs.IsSuccess || projectDocs.Value is null)
            return Result<BodyLinkPlanResolution>.Failure(projectDocs.Error ?? "Failed to load project documents");

        var children = projectDocs.Value
            .Where(d => d.ParentDocumentId == pillarDocumentId)
            .ToList();

        var instructions = SpokeStatusResolver.ResolveBodyLinkInstructions(plan, children);
        var pendingCount = instructions.Count(i => !i.IsTargetActive);
        return Result<BodyLinkPlanResolution>.Success(new BodyLinkPlanResolution(instructions, pendingCount));
    }

    private sealed record BodyLinkPlanResolution(
        IReadOnlyList<BodyLinkInsertionInstruction> Instructions,
        int PendingCount);
}

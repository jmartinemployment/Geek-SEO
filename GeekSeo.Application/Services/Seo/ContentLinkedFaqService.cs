using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentLinkedFaqService(
    IContentDocumentService documents,
    IContentDocumentRepository documentRepo,
    IContentBlogSpokeMigrator migrator,
    IAIProvider ai) : IContentLinkedFaqService
{
    public async Task<Result<GenerateLinkedFaqsResponse>> GenerateLinkedFaqsAsync(
        Guid userId,
        Guid pillarDocumentId,
        CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, pillarDocumentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<GenerateLinkedFaqsResponse>.Failure(access.Error ?? "Access denied");

        var pillar = access.Value;
        if (string.Equals(pillar.DocumentKind, ContentDocumentKinds.Spoke, StringComparison.OrdinalIgnoreCase))
            return Result<GenerateLinkedFaqsResponse>.Failure("Linked FAQs can only be generated on a pillar document.");

        if (string.IsNullOrWhiteSpace(pillar.ContentHtml) || pillar.ContentHtml.Length < 200)
        {
            return Result<GenerateLinkedFaqsResponse>.Failure(
                "Write or generate the pillar article before generating linked FAQs.");
        }

        var plan = ContentLinkPlanJson.Parse(pillar.LinkPlanJson);
        if (plan.FaqItems.Count == 0)
            return Result<GenerateLinkedFaqsResponse>.Failure("Build and save a cluster link plan with FAQ items first.");

        await migrator.EnsureMigratedChildAsync(userId, pillarDocumentId, ct);

        var projectDocs = await documentRepo.GetByProjectAsync(pillar.ProjectId, ct);
        if (!projectDocs.IsSuccess || projectDocs.Value is null)
            return Result<GenerateLinkedFaqsResponse>.Failure(projectDocs.Error ?? "Failed to load project documents");

        var children = projectDocs.Value
            .Where(d => d.ParentDocumentId == pillarDocumentId)
            .ToList();

        var assignments = SpokeStatusResolver.ResolveFaqAssignments(plan, children);
        var siteFocus = SiteWritingFocusSerializer.TryDeserialize(pillar.SiteFocusJson);
        var request = LinkedFaqRequestBuilder.Build(
            pillar.TargetKeyword,
            pillar.ContentHtml,
            siteFocus,
            assignments);

        var enriched = await LinkedClosingFaqEnricher.EnrichAsync(pillar.ContentHtml, request, ai, ct);
        if (!enriched.IsSuccess || enriched.Value.Html is null)
            return Result<GenerateLinkedFaqsResponse>.Failure(enriched.Error ?? "Linked FAQ enrichment failed");

        var (html, linkedCount, plainTextOnlyCount, skipped) = enriched.Value;
        var wordCount = html.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var updated = await documentRepo.UpdateContentAsync(
            pillarDocumentId,
            new UpdateContentRequest
            {
                ContentHtml = html,
                Title = pillar.Title,
                TargetKeyword = pillar.TargetKeyword,
                TargetLocation = pillar.TargetLocation,
            },
            wordCount,
            ct);

        if (!updated.IsSuccess || updated.Value is null)
            return Result<GenerateLinkedFaqsResponse>.Failure(updated.Error ?? "Failed to save linked FAQs");

        return Result<GenerateLinkedFaqsResponse>.Success(new GenerateLinkedFaqsResponse
        {
            ContentHtml = html,
            LinkedCount = linkedCount,
            PlainTextOnlyCount = plainTextOnlyCount,
            Skipped = skipped,
        });
    }
}

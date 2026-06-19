using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentResearchWritingService(
    IContentDocumentService documents,
    IUrlResearchService urlResearch,
    IAIWritingService writing) : IContentResearchWritingService
{
    public async Task<Result<SeoContentDocument>> AttachResearchAsync(
        Guid userId, Guid documentId, AttachUrlResearchRequest request, CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<SeoContentDocument>.Failure(access.Error ?? "Access denied");

        var research = await urlResearch.GetFullAsync(userId, request.UrlResearchId, ct);
        if (!research.IsSuccess || research.Value is null)
            return Result<SeoContentDocument>.Failure(research.Error ?? "Page research not found");

        var validation = ResearchBackedWriteGate.ValidateResearchForProject(access.Value.ProjectId, research.Value);
        if (!validation.IsSuccess)
            return Result<SeoContentDocument>.Failure(validation.Error ?? "Invalid page research");

        return await documents.AttachUrlResearchAsync(userId, documentId, request.UrlResearchId, ct);
    }

    public async Task<Result<WritingTextResult>> DraftFromResearchAsync(
        Guid userId, Guid documentId, CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<WritingTextResult>.Failure(access.Error ?? "Access denied");

        var doc = access.Value;
        if (!ResearchBackedWriteGate.IsResearchBacked(doc))
            return Result<WritingTextResult>.Failure(ContentWritingBlockMessage.Default);

        var research = await urlResearch.GetFullAsync(userId, doc.UrlResearchId!.Value, ct);
        if (!research.IsSuccess || research.Value is null)
            return Result<WritingTextResult>.Failure(research.Error ?? "Page research not found");

        var gate = ResearchBackedWriteGate.EnsureResearchReady(doc, research.Value);
        if (!gate.IsSuccess)
            return Result<WritingTextResult>.Failure(gate.Error ?? "Research not ready");

        var context = WritingResearchContextMapper.FromEntity(research.Value);
        var draft = await writing.GenerateDraftFromResearchAsync(userId, new ResearchDraftRequest
        {
            Research = context,
            Title = string.IsNullOrWhiteSpace(doc.Title) || doc.Title == "Untitled Document"
                ? context.DerivedKeyword
                : doc.Title,
            TargetWordCount = context.Benchmarks.MedianWordCountTop5,
        }, ct);

        if (!draft.IsSuccess || draft.Value is null)
            return draft;

        var updated = await documents.UpdateContentAsync(userId, documentId, new UpdateContentRequest
        {
            ContentHtml = draft.Value.Content,
            Title = string.IsNullOrWhiteSpace(doc.Title) || doc.Title == "Untitled Document"
                ? context.DerivedKeyword
                : doc.Title,
            TargetKeyword = context.DerivedKeyword,
            TargetLocation = context.SearchLocation,
        }, ct);

        if (!updated.IsSuccess)
            return Result<WritingTextResult>.Failure(updated.Error ?? "Failed to save draft");

        return draft;
    }
}

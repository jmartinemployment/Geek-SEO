using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentResearchWritingService(
    IContentDocumentService documents,
    IAIWritingService writing,
    WritingResearchContextLoader researchLoader) : IContentResearchWritingService
{
    public async Task<Result<SeoContentDocument>> AttachResearchAsync(
        Guid userId, Guid documentId, AttachAnalysisRunRequest request, CancellationToken ct = default)
    {
        if (request.SiteProfileId is not { } siteProfileId || siteProfileId == Guid.Empty)
            return Result<SeoContentDocument>.Failure(
                "site_profile is required for research-backed content.");

        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<SeoContentDocument>.Failure(access.Error ?? "Access denied");

        var targetKeyword = string.IsNullOrWhiteSpace(request.TargetKeyword)
            ? access.Value.TargetKeyword
            : request.TargetKeyword.Trim();

        return await documents.AttachAnalysisRunAsync(
            userId,
            documentId,
            request.AnalysisRunId,
            targetKeyword,
            string.Empty,
            siteProfileId,
            ct);
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

        var contextResult = researchLoader.LoadAsync(userId, doc, ct);
        var contextResultValue = await contextResult;
        if (!contextResultValue.IsSuccess || contextResultValue.Value is null)
            return Result<WritingTextResult>.Failure(contextResultValue.Error ?? "Research not ready");

        var context = contextResultValue.Value;
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

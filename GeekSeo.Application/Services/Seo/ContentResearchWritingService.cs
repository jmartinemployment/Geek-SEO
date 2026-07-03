using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentResearchWritingService(
    IContentDocumentService documents,
    IAIWritingService writing,
    IContentSpokeService spokes,
    WritingResearchContextLoader researchLoader) : IContentResearchWritingService
{
    public async Task<Result<SeoContentDocument>> AttachResearchAsync(
        Guid userId, Guid documentId, AttachAnalysisRunRequest request, CancellationToken ct = default)
    {
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
            request.SiteProfileId,
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
        var blogHint = await TryCreateBlogHintAsync(userId, doc, context, ct);

        var draft = await writing.GenerateDraftFromResearchAsync(userId, new ResearchDraftRequest
        {
            Research = context,
            Title = string.IsNullOrWhiteSpace(doc.Title) || doc.Title == "Untitled Document"
                ? context.DerivedKeyword
                : doc.Title,
            TargetWordCount = ResearchDraftWordTarget.Resolve(
                0,
                context.Benchmarks.MedianWordCountTop5),
            SupportingBlogPost = blogHint,
        }, ct);

        if (!draft.IsSuccess || draft.Value is null)
            return draft;

        var articleTitle = ScoreSuggestionApplicator.ProposeTitle(
            string.IsNullOrWhiteSpace(doc.Title) || doc.Title == "Untitled Document" ? null : doc.Title,
            context.DerivedKeyword,
            context.Benchmarks.MedianTitleLengthTop10);
        var contentWithH1 = ScoreSuggestionApplicator.EnsureArticleH1(draft.Value.Content, articleTitle);

        var updated = await documents.UpdateContentAsync(userId, documentId, new UpdateContentRequest
        {
            ContentHtml = contentWithH1,
            Title = articleTitle,
            TargetKeyword = context.DerivedKeyword,
            TargetLocation = context.SearchLocation,
        }, ct);

        if (!updated.IsSuccess)
            return Result<WritingTextResult>.Failure(updated.Error ?? "Failed to save draft");

        return Result<WritingTextResult>.Success(new WritingTextResult
        {
            Content = contentWithH1,
        });
    }

    private async Task<SupportingBlogPostHint?> TryCreateBlogHintAsync(
        Guid userId,
        SeoContentDocument doc,
        WritingResearchContext context,
        CancellationToken ct)
    {
        // Pick the best PAA/PASF topic that isn't the pillar keyword itself.
        var topic = context.PeopleAlsoAsk
            .Select(p => p.Question)
            .Concat(context.RelatedSearches.Select(r => r.SearchText))
            .Where(t => !string.IsNullOrWhiteSpace(t)
                && !string.Equals(t.Trim(), context.DerivedKeyword, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(topic))
            return null;

        try
        {
            var spokeTitle = topic.Trim();
            if (!spokeTitle.Contains('?', StringComparison.Ordinal))
                spokeTitle += "?";

            var spoke = await spokes.CreateAsync(userId, doc.Id, new CreateContentSpokeRequest
            {
                Phrase = topic.Trim(),
                Title = spokeTitle,
                SourceType = SpokeSourceTypes.Paa,
            }, ct);

            if (!spoke.IsSuccess || spoke.Value is null || string.IsNullOrWhiteSpace(spoke.Value.PublishSlug))
                return null;

            return new SupportingBlogPostHint
            {
                Topic = topic.Trim(),
                Slug = spoke.Value.PublishSlug,
                Title = spoke.Value.Title,
            };
        }
        catch
        {
            return null;
        }
    }
}

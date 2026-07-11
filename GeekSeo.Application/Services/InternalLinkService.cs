using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class InternalLinkService(
    IProjectRepository projects,
    IContentDocumentRepository documents) : IInternalLinkService
{
    public async Task<Result<IReadOnlyList<InternalLinkSuggestion>>> SuggestAsync(
        Guid userId, InternalLinkSuggestRequest request, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(request.ProjectId, ct);
        if (!project.IsSuccess || project.Value is null || project.Value.UserId != userId)
            return Result<IReadOnlyList<InternalLinkSuggestion>>.Failure("Access denied");

        var doc = await documents.GetByIdAsync(request.DocumentId, ct);
        if (!doc.IsSuccess || doc.Value is null || doc.Value.ProjectId != request.ProjectId)
            return Result<IReadOnlyList<InternalLinkSuggestion>>.Failure("Document not found");

        var allDocs = await documents.GetByProjectAsync(request.ProjectId, ct);
        if (!allDocs.IsSuccess || allDocs.Value is null)
            return Result<IReadOnlyList<InternalLinkSuggestion>>.Failure("Failed to load project documents");

        var current = doc.Value;
        var projectDocs = allDocs.Value
            .Where(IsActiveDocument)
            .Where(d => d.Id != current.Id)
            .ToList();

        var planHost = ResolvePlanHost(current, allDocs.Value);
        var keyword = current.TargetKeyword.Trim();
        var max = Math.Clamp(request.MaxSuggestions, 1, 25);
        var suggestions = new List<InternalLinkSuggestion>();

        foreach (var candidate in projectDocs)
        {
            var (linkType, clusterBoost, reason) = ClassifyClusterRelation(current, candidate);
            var overlap = KeywordOverlap(keyword, candidate.TargetKeyword);
            if (overlap < 0.1 && clusterBoost <= 0)
                continue;

            var planBoost = ResolvePlanPriorityBoost(planHost, candidate) * 0.08;
            var score = overlap + clusterBoost + planBoost;
            if (score < 0.12)
                continue;

            var publishPath = ContentPublishPathResolver.ResolveRelativePath(candidate.PublishSlug);
            suggestions.Add(new InternalLinkSuggestion
            {
                TargetDocumentId = candidate.Id,
                AnchorText = ResolveAnchorText(candidate),
                TargetUrl = publishPath ?? BuildEditorUrl(candidate.Id),
                PublishPath = publishPath,
                LinkType = linkType,
                Reason = reason,
                RelevanceScore = score,
            });
        }

        return Result<IReadOnlyList<InternalLinkSuggestion>>.Success(
            suggestions
                .OrderByDescending(s => s.RelevanceScore)
                .ThenBy(s => s.LinkType, StringComparer.Ordinal)
                .Take(max)
                .ToList());
    }

    public async Task<Result<InternalLinkAutoInsertResult>> AutoInsertAsync(
        Guid userId, InternalLinkAutoInsertRequest request, CancellationToken ct = default)
    {
        var suggestions = await SuggestAsync(
            userId,
            new InternalLinkSuggestRequest
            {
                ProjectId = request.ProjectId,
                DocumentId = request.DocumentId,
                MaxSuggestions = 5,
            },
            ct);

        if (!suggestions.IsSuccess || suggestions.Value is null || suggestions.Value.Count == 0)
            return Result<InternalLinkAutoInsertResult>.Failure("No related articles to link");

        var doc = await documents.GetByIdAsync(request.DocumentId, ct);
        if (!doc.IsSuccess || doc.Value is null || doc.Value.UserId != userId)
            return Result<InternalLinkAutoInsertResult>.Failure("Document not found");

        var html = doc.Value.ContentHtml;
        foreach (var candidate in suggestions.Value.OrderByDescending(s => s.RelevanceScore))
        {
            if (HtmlAlreadyLinksTarget(html, candidate))
                continue;

            var linkHtml =
                $"<a href=\"{candidate.TargetUrl}\">{System.Net.WebUtility.HtmlEncode(candidate.AnchorText)}</a>";
            var updated = InsertLinkIntoHtml(html, linkHtml);
            var wordCount = HtmlTextUtility.CountWords(updated);
            var saved = await documents.UpdateContentAsync(
                request.DocumentId,
                new UpdateContentRequest
                {
                    ContentHtml = updated,
                    Title = doc.Value.Title,
                    TargetKeyword = doc.Value.TargetKeyword,
                    TargetLocation = doc.Value.TargetLocation,
                },
                wordCount,
                ct);

            if (!saved.IsSuccess)
                return Result<InternalLinkAutoInsertResult>.Failure(saved.Error ?? "Could not save document");

            return Result<InternalLinkAutoInsertResult>.Success(new InternalLinkAutoInsertResult
            {
                Inserted = true,
                ContentHtml = updated,
                AnchorText = candidate.AnchorText,
                TargetUrl = candidate.TargetUrl,
                Message = candidate.Reason,
            });
        }

        return Result<InternalLinkAutoInsertResult>.Success(new InternalLinkAutoInsertResult
        {
            Inserted = false,
            ContentHtml = html,
            Message = "Top suggestions are already linked in this document",
        });
    }

    internal static SeoContentDocument? ResolvePlanHost(
        SeoContentDocument current,
        IReadOnlyList<SeoContentDocument> projectDocs)
    {
        if (IsPillar(current))
            return current;

        if (current.ParentDocumentId is Guid parentId)
        {
            return projectDocs.FirstOrDefault(d => d.Id == parentId) ?? current;
        }

        return current;
    }

    internal static (string LinkType, double ClusterBoost, string Reason) ClassifyClusterRelation(
        SeoContentDocument current,
        SeoContentDocument candidate)
    {
        if (IsPillar(current) && candidate.ParentDocumentId == current.Id)
        {
            return (
                InternalLinkTypes.Spoke,
                0.55,
                $"Cluster spoke for this pillar ({candidate.TargetKeyword})");
        }

        if (IsSpoke(current) && candidate.Id == current.ParentDocumentId)
        {
            return (
                InternalLinkTypes.Pillar,
                0.65,
                $"Parent pillar ({candidate.TargetKeyword})");
        }

        if (IsSpoke(current) &&
            current.ParentDocumentId is Guid parentId &&
            candidate.ParentDocumentId == parentId)
        {
            return (
                InternalLinkTypes.Spoke,
                0.45,
                $"Sibling spoke in this cluster ({candidate.TargetKeyword})");
        }

        return (
            InternalLinkTypes.Sibling,
            0,
            $"Related article in this project ({candidate.TargetKeyword})");
    }

    internal static int ResolvePlanPriorityBoost(SeoContentDocument? planHost, SeoContentDocument candidate)
    {
        if (planHost is null || string.IsNullOrWhiteSpace(planHost.LinkPlanJson))
            return 0;

        var plan = ContentLinkPlanJson.Parse(planHost.LinkPlanJson);
        var publishPath = ContentPublishPathResolver.ResolveRelativePath(candidate.PublishSlug);

        foreach (var body in plan.BodyLinks.OrderByDescending(b => b.Priority))
        {
            if (body.TargetDocumentId == candidate.Id)
                return Math.Max(body.Priority, 1);

            if (publishPath is not null &&
                string.Equals(body.TargetPath, publishPath, StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(body.Priority, 1);
            }
        }

        foreach (var faq in plan.FaqItems)
        {
            if (faq.TargetDocumentId == candidate.Id)
                return 3;

            if (publishPath is not null &&
                string.Equals(faq.TargetPath, publishPath, StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }
        }

        return 0;
    }

    private static bool HtmlAlreadyLinksTarget(string html, InternalLinkSuggestion candidate)
    {
        if (html.Contains(candidate.TargetUrl, StringComparison.OrdinalIgnoreCase))
            return true;

        if (candidate.PublishPath is not null &&
            html.Contains(candidate.PublishPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var editorUrl = BuildEditorUrl(candidate.TargetDocumentId);
        return html.Contains(editorUrl, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveAnchorText(SeoContentDocument doc)
    {
        if (!string.IsNullOrWhiteSpace(doc.SpokeSourcePhrase))
            return doc.SpokeSourcePhrase.Trim();

        if (!string.IsNullOrWhiteSpace(doc.Title))
            return doc.Title.Trim();

        return doc.TargetKeyword.Trim();
    }

    private static bool IsActiveDocument(SeoContentDocument doc) =>
        !string.Equals(doc.Status, "deleted", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(doc.TargetKeyword);

    private static bool IsPillar(SeoContentDocument doc) =>
        string.Equals(doc.DocumentKind, ContentDocumentKinds.Pillar, StringComparison.OrdinalIgnoreCase);

    private static bool IsSpoke(SeoContentDocument doc) =>
        doc.ParentDocumentId is not null ||
        string.Equals(doc.DocumentKind, ContentDocumentKinds.Spoke, StringComparison.OrdinalIgnoreCase);

    private static string BuildEditorUrl(Guid documentId) => $"/content-writing?documentId={documentId}";

    internal static string InsertLinkIntoHtml(string html, string linkHtml)
    {
        if (string.IsNullOrWhiteSpace(html))
            return $"<p>Related reading: {linkHtml}</p>";

        var closeIdx = html.IndexOf("</p>", StringComparison.OrdinalIgnoreCase);
        if (closeIdx >= 0)
            return html.Insert(closeIdx, $" Related reading: {linkHtml}.");

        return $"{html}<p>Related reading: {linkHtml}</p>";
    }

    internal static double KeywordOverlap(string a, string b)
    {
        var setA = a.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var setB = b.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (setA.Count == 0 || setB.Count == 0)
            return 0;

        var intersection = setA.Intersect(setB).Count();
        return intersection / (double)Math.Max(setA.Count, setB.Count);
    }
}

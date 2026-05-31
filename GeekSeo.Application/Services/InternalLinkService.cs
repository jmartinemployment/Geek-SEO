using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

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
        var siblings = (allDocs.Value ?? [])
            .Where(d => d.Id != request.DocumentId && !string.IsNullOrWhiteSpace(d.TargetKeyword))
            .ToList();

        var keyword = doc.Value.TargetKeyword.Trim();
        var suggestions = new List<InternalLinkSuggestion>();
        var max = Math.Clamp(request.MaxSuggestions, 1, 25);

        foreach (var sibling in siblings)
        {
            if (suggestions.Count >= max) break;
            var overlap = KeywordOverlap(keyword, sibling.TargetKeyword);
            if (overlap < 0.15) continue;

            suggestions.Add(new InternalLinkSuggestion
            {
                AnchorText = sibling.Title.Length > 0 ? sibling.Title : sibling.TargetKeyword,
                TargetUrl = $"/app/content/{sibling.Id}",
                Reason = $"Related article in this project ({sibling.TargetKeyword})",
                RelevanceScore = overlap,
            });
        }

        return Result<IReadOnlyList<InternalLinkSuggestion>>.Success(
            suggestions.OrderByDescending(s => s.RelevanceScore).Take(max).ToList());
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
            if (html.Contains(candidate.TargetUrl, StringComparison.OrdinalIgnoreCase))
                continue;

            var linkHtml = $"<a href=\"{candidate.TargetUrl}\">{System.Net.WebUtility.HtmlEncode(candidate.AnchorText)}</a>";
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

    private static string InsertLinkIntoHtml(string html, string linkHtml)
    {
        if (string.IsNullOrWhiteSpace(html))
            return $"<p>Related reading: {linkHtml}</p>";

        var closeIdx = html.IndexOf("</p>", StringComparison.OrdinalIgnoreCase);
        if (closeIdx >= 0)
            return html.Insert(closeIdx, $" Related reading: {linkHtml}.");

        return $"{html}<p>Related reading: {linkHtml}</p>";
    }

    private static double KeywordOverlap(string a, string b)
    {
        var setA = a.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var setB = b.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (setA.Count == 0 || setB.Count == 0) return 0;
        var intersection = setA.Intersect(setB).Count();
        return intersection / (double)Math.Max(setA.Count, setB.Count);
    }

}

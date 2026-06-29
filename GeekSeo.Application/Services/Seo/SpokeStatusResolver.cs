using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public static class SpokeStatusResolver
{
    public static IReadOnlyList<LinkedFaqAssignment> ResolveFaqAssignments(
        ContentLinkPlan plan,
        IReadOnlyList<SeoContentDocument> childSpokes)
    {
        if (plan.FaqItems.Count == 0)
            return [];

        var byId = childSpokes.ToDictionary(d => d.Id);
        var bySlug = childSpokes
            .Where(d => !string.IsNullOrWhiteSpace(d.PublishSlug))
            .GroupBy(d => d.PublishSlug!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var assignments = new List<LinkedFaqAssignment>(plan.FaqItems.Count);
        for (var i = 0; i < plan.FaqItems.Count; i++)
        {
            var item = plan.FaqItems[i];
            var id = $"faq-{(i + 1):D2}";
            var anchorText = string.IsNullOrWhiteSpace(item.AnchorText) ? item.Question : item.AnchorText.Trim();
            var child = ResolveChild(item, byId, bySlug);
            var (targetPath, isActive) = ResolveTarget(item, child);

            assignments.Add(new LinkedFaqAssignment(
                id,
                item.Question,
                targetPath,
                anchorText,
                isActive));
        }

        return assignments;
    }

    public static bool IsBodyGenerated(SeoContentDocument doc)
    {
        if (string.Equals(doc.Status, SpokeLinkStatuses.BodyGenerated, StringComparison.OrdinalIgnoreCase))
            return true;

        return doc.WordCount > 80 &&
               !string.IsNullOrWhiteSpace(doc.ContentHtml) &&
               !doc.ContentHtml.Contains("Spoke draft shell", StringComparison.OrdinalIgnoreCase);
    }

    private static SeoContentDocument? ResolveChild(
        ContentLinkFaqItem item,
        IReadOnlyDictionary<Guid, SeoContentDocument> byId,
        IReadOnlyDictionary<string, SeoContentDocument> bySlug)
    {
        if (item.TargetDocumentId is Guid docId && byId.TryGetValue(docId, out var byDocId))
            return byDocId;

        if (TryExtractBlogSlug(item.TargetPath, out var slug) && bySlug.TryGetValue(slug, out var bySlugDoc))
            return bySlugDoc;

        return null;
    }

    private static (string TargetPath, bool IsTargetActive) ResolveTarget(
        ContentLinkFaqItem item,
        SeoContentDocument? child)
    {
        if (child is not null && IsBodyGenerated(child))
        {
            var relative = ContentPublishPathResolver.ResolveRelativePath(child.PublishSlug);
            if (!string.IsNullOrWhiteSpace(relative))
                return (relative, true);
        }

        if (IsAllowlistedExternalUrl(item.TargetPath))
            return (item.TargetPath!.Trim(), true);

        return (string.Empty, false);
    }

    private static bool TryExtractBlogSlug(string? targetPath, out string slug)
    {
        slug = string.Empty;
        if (string.IsNullOrWhiteSpace(targetPath))
            return false;

        var normalized = targetPath.Trim();
        const string prefix = ContentPublishPathResolver.DefaultBlogPathPrefix;
        if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        slug = normalized[prefix.Length..].Trim('/');
        return ContentPublishSlug.IsValid(slug);
    }

    private static bool IsAllowlistedExternalUrl(string? targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
            return false;

        var trimmed = targetPath.Trim();
        if (!trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return false;

        return !trimmed.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
    }
}

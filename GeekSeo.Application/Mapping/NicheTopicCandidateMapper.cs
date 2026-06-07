using System.Text.Json;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Mapping;

public static class NicheTopicCandidateMapper
{
    private const int MaxEvidenceItems = 5;
    private const int MaxSnippetLength = 120;

    public static IReadOnlyList<NicheTopicCandidateBulkUpsert> FromSiteTopicProfile(
        Guid profileId,
        SiteTopicProfile fused,
        bool includeEvidence)
    {
        var selectedSlugs = fused.SelectedPillars.Select(p => p.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var order = 0;
        return fused.AllCandidates.Select(c =>
        {
            var isSelected = selectedSlugs.Contains(c.Slug);
            fused.ExclusionReasons.TryGetValue(c.Slug, out var exclusionReason);
            return new NicheTopicCandidateBulkUpsert(
                null,
                profileId,
                c.Slug,
                c.Name,
                c.Confidence,
                isSelected,
                isSelected ? null : exclusionReason,
                c.DedicatedPageUrl,
                c.InternalLinkCount,
                c.ContentDepthScore,
                order++,
                includeEvidence ? SerializeEvidence(c.Evidence) : null);
        }).ToList();
    }

    public static string? SerializeEvidence(IReadOnlyList<TopicEvidence> evidence)
    {
        if (evidence.Count == 0) return null;
        var trimmed = evidence
            .Take(MaxEvidenceItems)
            .Select(e => new
            {
                source = e.Source,
                snippet = Truncate(e.Snippet, MaxSnippetLength),
                url = e.Url,
                weight = e.Weight,
            })
            .ToList();
        return JsonSerializer.Serialize(trimmed);
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}

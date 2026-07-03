using System.Text.RegularExpressions;

namespace SiteAnalyzer2.Domain;

public static partial class ResearchTopicSlug
{
    public static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

    /// <summary>Folder slug for <c>research/&lt;topic&gt;/</c> — not the pillar keyword phrase.</summary>
    public static string FromKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return string.Empty;

        var slug = keyword.Trim().ToLowerInvariant().Replace("&", "and");
        slug = SlugRegex().Replace(slug, "-").Trim('-');
        if (slug.Length > 80)
            slug = slug[..80].TrimEnd('-');

        return slug;
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex SlugRegex();
}

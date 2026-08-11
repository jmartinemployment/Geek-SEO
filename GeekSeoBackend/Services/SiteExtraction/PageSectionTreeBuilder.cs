using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Builds a real per-page heading tree (h1-h6, each with its own paragraph text and nested
/// child headings) from already-fetched HTML - the single source of truth replacing both
/// <see cref="HomepageHeadingsExtractor"/>'s flat heading list and
/// <see cref="PageContentExtractor"/>'s separate phrase extraction for site-crawl use.
/// </summary>
public static partial class PageSectionTreeBuilder
{
    // Per-page tree disabled per product direction — waste of time, Site Structure pillar list not needed.
    // Kept for API compat; returns empty so hierarchy child injection is no-op.
    public static IReadOnlyList<PageSection> Build(string html) => [];

    private static bool IsDuplicateHeading(List<MutableNode> siblings, int level, string text)
    {
        var normalizedText = text.ToLowerInvariant();
        foreach (var sibling in siblings)
        {
            if (sibling.Level == level && sibling.HeadingText.ToLowerInvariant() == normalizedText)
                return true;
        }
        return false;
    }

    private static string CleanText(string raw)
    {
        var noTags = TagRegex().Replace(raw, " ");
        var decoded = WebUtility.HtmlDecode(noTags).Trim();
        return decoded.Length == 0 ? string.Empty : WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    // Nodes are mutable while building (children/paragraphs accumulate as later siblings are
    // parsed) and sealed into the immutable PageSection record once fully populated.
    private sealed class MutableNode
    {
        public required int Level { get; init; }
        public required string HeadingText { get; init; }
        public List<string> Paragraphs { get; } = [];
        public List<MutableNode> Children { get; } = [];

        public PageSection Seal() => new()
        {
            Level = Level,
            HeadingText = HeadingText,
            Paragraphs = Paragraphs,
            Children = Children.ConvertAll(c => c.Seal()),
        };
    }

    [GeneratedRegex(
        "<h(?<hlevel>[1-6])(?:\\s[^>]*)?>(?<htext>[\\s\\S]*?)</h\\k<hlevel>>|<p(?:\\s[^>]*)?>(?<ptext>[\\s\\S]*?)</p>|<li(?:\\s[^>]*)?>(?<ptext>[\\s\\S]*?)</li>",
        RegexOptions.IgnoreCase)]
    private static partial Regex NodeRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

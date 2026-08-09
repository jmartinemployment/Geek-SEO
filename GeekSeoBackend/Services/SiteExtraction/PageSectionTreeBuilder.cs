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
    public static IReadOnlyList<PageSection> Build(string html)
    {
        var roots = new List<MutableNode>();
        var stack = new List<MutableNode>();

        foreach (Match match in NodeRegex().Matches(html))
        {
            if (match.Groups["hlevel"].Success)
            {
                if (!int.TryParse(match.Groups["hlevel"].Value, out var level) || level is < 1 or > 6)
                    continue;

                var text = CleanText(match.Groups["htext"].Value);
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var node = new MutableNode { Level = level, HeadingText = text };

                while (stack.Count > 0 && stack[^1].Level >= level)
                    stack.RemoveAt(stack.Count - 1);

                var parent = stack.Count == 0 ? null : stack[^1];
                var siblings = parent?.Children ?? roots;

                if (IsDuplicateHeading(siblings, level, text))
                    continue;

                siblings.Add(node);
                stack.Add(node);
                continue;
            }

            // Paragraph-like content: attach to the nearest open heading. Content that appears
            // before any heading has no node to attach to and is intentionally dropped - this
            // tree exists to ground heading-scoped topics, not to capture whole-page prose.
            if (stack.Count == 0)
                continue;

            var paragraphText = CleanText(match.Groups["ptext"].Value);
            if (!string.IsNullOrWhiteSpace(paragraphText))
                stack[^1].Paragraphs.Add(paragraphText);
        }

        return roots.ConvertAll(r => r.Seal());
    }

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

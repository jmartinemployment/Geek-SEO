using System.Text.RegularExpressions;
using ContentWriter.Application.Providers;

namespace ContentWriter.Application.Services.Publish;

/// <summary>
/// Detects and removes legacy link-list sections (Related Items, Further reading, etc.) from generated bodies.
/// Companion URLs belong in JSON-LD <citation>, not in-article H2 sections.
/// </summary>
public static partial class JunkBodySectionFilter
{
    private static readonly string[] JunkSectionTitles =
    [
        "related items",
        "related articles",
        "related resources",
        "related links",
        "related content",
        "related",
        "further reading",
        "what to read next",
        "read next",
        "call to action",
        "frequently asked questions",
        "faq",
        "accounting use case",
    ];

    public static bool IsJunkSectionHeading(string? heading)
    {
        if (string.IsNullOrWhiteSpace(heading))
        {
            return false;
        }

        var normalized = NormalizeHeading(heading);
        if (normalized.Length == 0)
        {
            return false;
        }

        foreach (var junk in JunkSectionTitles)
        {
            if (normalized.Equals(junk, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return normalized.StartsWith("related ", StringComparison.Ordinal)
               && (normalized.Contains(" item", StringComparison.Ordinal)
                   || normalized.Contains(" article", StringComparison.Ordinal)
                   || normalized.Contains(" resource", StringComparison.Ordinal)
                   || normalized.Contains(" link", StringComparison.Ordinal));
    }

    public static string StripJunkSectionsFromHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        var matches = H2HeadingRegex().Matches(html).Cast<Match>().ToList();
        if (matches.Count == 0)
        {
            return html;
        }

        var removeRanges = new List<(int Start, int End)>();
        for (var i = 0; i < matches.Count; i++)
        {
            var heading = StripTags(matches[i].Groups[1].Value);
            if (!IsJunkSectionHeading(heading))
            {
                continue;
            }

            var start = matches[i].Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : html.Length;
            removeRanges.Add((start, end));
        }

        if (removeRanges.Count == 0)
        {
            return html;
        }

        var sb = new System.Text.StringBuilder();
        var cursor = 0;
        foreach (var (start, end) in removeRanges.OrderBy(r => r.Start))
        {
            if (start < cursor)
            {
                continue;
            }

            sb.Append(html.AsSpan(cursor, start - cursor));
            cursor = end;
        }

        sb.Append(html.AsSpan(cursor));
        return sb.ToString().Trim();
    }

    public static void ValidateMarkdownHasNoJunkSections(string markdown)
    {
        foreach (var line in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            if (!line.StartsWith("## ", StringComparison.Ordinal))
            {
                continue;
            }

            var heading = line[3..].Trim();
            if (IsJunkSectionHeading(heading))
            {
                throw new ContentGenerationException(
                    $"Body contains legacy section '{heading}'. Regenerate content in Content-Writer.");
            }
        }
    }

    private static string NormalizeHeading(string heading) =>
        StripTags(heading).Trim().ToLowerInvariant();

    private static string StripTags(string html) =>
        Regex.Replace(html, "<[^>]+>", " ").Replace("&nbsp;", " ").Trim();

    [GeneratedRegex(@"<h2[^>]*>(.*?)</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex H2HeadingRegex();
}

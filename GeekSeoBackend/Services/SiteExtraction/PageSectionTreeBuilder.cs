using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;
using HtmlAgilityPack;

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
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var roots = new List<MutableNode>();
        var stack = new List<MutableNode>();

        foreach (var node in doc.DocumentNode.ChildNodes)
        {
            ProcessNode(node, roots, stack);
        }

        return roots.ConvertAll(r => r.Seal());
    }

    private static void ProcessNode(HtmlNode node, List<MutableNode> roots, List<MutableNode> stack)
    {
        if (node.NodeType != HtmlNodeType.Element)
            return;

        var tagName = node.Name.ToLowerInvariant();

        // Handle headings (h1-h6)
        if (tagName.Length == 2 && tagName[0] == 'h' && char.IsDigit(tagName[1]))
        {
            if (!int.TryParse(tagName[1].ToString(), out var level) || level is < 1 or > 6)
                return;

            var text = WebUtility.HtmlDecode(node.InnerText).Trim();
            text = WhitespaceRegex().Replace(text, " ").Trim();
            if (string.IsNullOrWhiteSpace(text))
                return;

            var newNode = new MutableNode { Level = level, HeadingText = text };

            // Pop stack to find the correct parent
            while (stack.Count > 0 && stack[^1].Level >= level)
                stack.RemoveAt(stack.Count - 1);

            var parent = stack.Count == 0 ? null : stack[^1];
            var siblings = parent?.Children ?? roots;

            siblings.Add(newNode);
            stack.Add(newNode);
            return;
        }

        // Handle paragraphs
        if (tagName == "p")
        {
            // Only attach to the nearest open heading
            if (stack.Count == 0)
                return;

            var (paragraphText, links) = CleanAndExtractLinks(node.InnerHtml);
            if (!string.IsNullOrWhiteSpace(paragraphText))
            {
                stack[^1].Paragraphs.Add(paragraphText);
                stack[^1].Links.AddRange(links);
            }
            return;
        }

        // Recursively process child nodes
        foreach (var child in node.ChildNodes)
        {
            ProcessNode(child, roots, stack);
        }
    }


    private static (string cleanText, List<PageSectionLink> links) CleanAndExtractLinks(string raw)
    {
        var links = new List<PageSectionLink>();
        var doc = new HtmlDocument();
        doc.LoadHtml(raw);

        var anchors = doc.DocumentNode.SelectNodes(".//a[@href]");
        if (anchors != null)
        {
            foreach (var anchor in anchors)
            {
                var href = anchor.GetAttributeValue("href", "").Trim();
                var text = anchor.InnerText?.Trim();
                if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(text))
                {
                    links.Add(new PageSectionLink { Text = text, Href = href });
                }
            }
        }

        var innerText = doc.DocumentNode.InnerText;
        var decoded = WebUtility.HtmlDecode(innerText).Trim();
        var cleanText = decoded.Length == 0 ? string.Empty : WhitespaceRegex().Replace(decoded, " ").Trim();
        return (cleanText, links);
    }

    // Nodes are mutable while building (children/paragraphs accumulate as later siblings are
    // parsed) and sealed into the immutable PageSection record once fully populated.
    private sealed class MutableNode
    {
        public required int Level { get; init; }
        public required string HeadingText { get; init; }
        public List<string> Paragraphs { get; } = [];
        public List<PageSectionLink> Links { get; } = [];
        public List<MutableNode> Children { get; } = [];

        public PageSection Seal() => new()
        {
            Level = Level,
            HeadingText = HeadingText,
            Paragraphs = Paragraphs,
            Links = Links,
            Children = Children.ConvertAll(c => c.Seal()),
        };
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

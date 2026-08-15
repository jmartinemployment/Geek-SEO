using GeekSeo.Application.Models.Seo;
using HtmlAgilityPack;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Extraction layer: heading tree from already-fetched, <c>data-gsv</c>-annotated HTML.
/// Skips <c>desktop-only</c> text; keeps <c>collapsed</c> at full weight. Fetch-layer HTML is
/// never stripped.
/// </summary>
public static class PageSectionTreeBuilder
{
    public static IReadOnlyList<PageSection> Build(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var roots = new List<MutableNode>();
        var stack = new List<MutableNode>();

        foreach (var node in doc.DocumentNode.ChildNodes)
            ProcessNode(node, roots, stack);

        return roots.ConvertAll(r => r.Seal());
    }

    private static void ProcessNode(HtmlNode node, List<MutableNode> roots, List<MutableNode> stack)
    {
        if (node.NodeType != HtmlNodeType.Element)
            return;

        if (IsDesktopOnly(node))
        {
            CollectLinksOnly(node, stack);
            return;
        }

        var tagName = node.Name.ToLowerInvariant();
        if (tagName is "script" or "style" or "template" or "noscript")
            return;

        if (tagName.Length == 2 && tagName[0] == 'h' && char.IsDigit(tagName[1]))
        {
            if (!int.TryParse(tagName[1].ToString(), out var level) || level is < 1 or > 6)
                return;

            var text = VisibleTextExtractor.ExtractHeadingText(node);
            if (string.IsNullOrWhiteSpace(text))
                return;

            var newNode = new MutableNode { Level = level, HeadingText = text };

            while (stack.Count > 0 && stack[^1].Level >= level)
                stack.RemoveAt(stack.Count - 1);

            var parent = stack.Count == 0 ? null : stack[^1];
            var siblings = parent?.Children ?? roots;

            siblings.Add(newNode);
            stack.Add(newNode);
            return;
        }

        if (tagName == "p")
        {
            AttachContent(node, stack);
            return;
        }

        if (tagName == "li")
        {
            if (ContainsHeading(node))
            {
                foreach (var child in node.ChildNodes)
                    ProcessNode(child, roots, stack);
                return;
            }

            AttachContent(node, stack);
            return;
        }

        if (tagName == "a")
        {
            if (ContainsHeading(node))
            {
                foreach (var child in node.ChildNodes)
                    ProcessNode(child, roots, stack);
            }

            AttachLink(node, stack);
            return;
        }

        foreach (var child in node.ChildNodes)
            ProcessNode(child, roots, stack);
    }

    private static void AttachContent(HtmlNode node, List<MutableNode> stack)
    {
        if (stack.Count == 0)
            return;

        var (paragraphText, links) = CleanAndExtractLinks(node);
        if (!string.IsNullOrWhiteSpace(paragraphText))
            stack[^1].Paragraphs.Add(paragraphText);
        stack[^1].Links.AddRange(links);
    }

    private static void AttachLink(HtmlNode node, List<MutableNode> stack)
    {
        if (stack.Count == 0)
            return;

        var href = node.GetAttributeValue("href", "").Trim();
        var text = VisibleTextExtractor.Extract(node);
        if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(text))
            return;

        stack[^1].Links.Add(new PageSectionLink
        {
            Text = text,
            Href = href,
            Rel = (node.GetAttributeValue("rel", "") ?? "").Trim(),
        });
    }

    private static void CollectLinksOnly(HtmlNode node, List<MutableNode> stack)
    {
        if (stack.Count == 0)
            return;

        var anchors = node.SelectNodes(".//a[@href]");
        if (anchors is null)
            return;

        foreach (var anchor in anchors)
            AttachLink(anchor, stack);
    }

    private static bool ContainsHeading(HtmlNode node)
    {
        foreach (var child in node.Descendants())
        {
            if (child.NodeType != HtmlNodeType.Element)
                continue;
            var name = child.Name;
            if (name.Length == 2 && name[0] is 'h' or 'H' && char.IsDigit(name[1]))
                return true;
        }

        return false;
    }

    private static bool IsDesktopOnly(HtmlNode node) =>
        node.GetAttributeValue("data-gsv", "").Equals("desktop-only", StringComparison.OrdinalIgnoreCase);

    private static (string cleanText, List<PageSectionLink> links) CleanAndExtractLinks(HtmlNode node)
    {
        var links = new List<PageSectionLink>();
        var anchors = node.SelectNodes(".//a[@href]");
        if (anchors != null)
        {
            foreach (var anchor in anchors)
            {
                var href = anchor.GetAttributeValue("href", "").Trim();
                var text = VisibleTextExtractor.Extract(anchor);
                if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(text))
                    continue;
                links.Add(new PageSectionLink
                {
                    Text = text,
                    Href = href,
                    Rel = (anchor.GetAttributeValue("rel", "") ?? "").Trim(),
                });
            }
        }

        var cleanText = VisibleTextExtractor.Extract(node);
        return (cleanText, links);
    }

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
}

using System.Net;
using System.Text;
using HtmlAgilityPack;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Extraction-layer text: Google-like word boundaries from the DOM, skipping
/// <c>data-gsv="desktop-only"</c> subtrees while keeping <c>visible</c> and <c>collapsed</c>.
/// </summary>
public static class VisibleTextExtractor
{
    private static readonly HashSet<string> SkipTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "template", "noscript",
    };

    private static readonly HashSet<string> BlockTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "address", "article", "aside", "blockquote", "br", "dd", "div", "dl", "dt",
        "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6",
        "header", "hr", "li", "main", "nav", "ol", "p", "pre", "section", "table", "tbody",
        "td", "th", "thead", "tr", "ul",
    };

    public static string Extract(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return Extract(doc.DocumentNode);
    }

    public static string Extract(HtmlNode node)
    {
        var sb = new StringBuilder();
        Walk(node, sb);
        return Collapse(WebUtility.HtmlDecode(sb.ToString()));
    }

    public static int EstimateWordCount(string html) =>
        Extract(html).Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>
    /// Heading text with the typewriter-sizer rule: an <c>aria-hidden</c> +
    /// <c>data-gsv="collapsed"</c> descendant wins over a visible sibling that is empty,
    /// a prefix of the sizer, or otherwise overlapping.
    /// </summary>
    public static string ExtractHeadingText(HtmlNode heading)
    {
        var sizer = FindSizer(heading);
        if (sizer is null)
            return Extract(heading);

        var sb = new StringBuilder();
        WalkHeading(heading, sizer, sb, skipSizerSiblings: false);
        return Collapse(WebUtility.HtmlDecode(sb.ToString()));
    }

    private static HtmlNode? FindSizer(HtmlNode root)
    {
        foreach (var node in root.Descendants())
        {
            if (node.NodeType != HtmlNodeType.Element)
                continue;
            var aria = node.GetAttributeValue("aria-hidden", "");
            var gsv = node.GetAttributeValue("data-gsv", "");
            if (aria.Equals("true", StringComparison.OrdinalIgnoreCase)
                && gsv.Equals("collapsed", StringComparison.OrdinalIgnoreCase))
                return node;
        }

        return null;
    }

    private static void WalkHeading(HtmlNode node, HtmlNode sizer, StringBuilder sb, bool skipSizerSiblings)
    {
        if (skipSizerSiblings)
            return;

        if (ReferenceEquals(node, sizer))
        {
            Walk(node, sb);
            return;
        }

        if (node.NodeType == HtmlNodeType.Text)
        {
            sb.Append(node.InnerText);
            return;
        }

        if (node.NodeType != HtmlNodeType.Element && node.NodeType != HtmlNodeType.Document)
            return;

        if (IsDesktopOnly(node) || SkipTags.Contains(node.Name))
            return;

        var block = BlockTags.Contains(node.Name);
        if (block)
            sb.Append(' ');

        var droppingVisibleSibling = false;
        foreach (var child in node.ChildNodes)
        {
            if (droppingVisibleSibling)
                continue;

            if (ReferenceEquals(child, sizer))
            {
                Walk(child, sb);
                droppingVisibleSibling = true;
                continue;
            }

            if (child.NodeType == HtmlNodeType.Element
                && IsLikelyAnimatedSibling(child, sizer))
            {
                continue;
            }

            WalkHeading(child, sizer, sb, skipSizerSiblings: false);
        }

        if (block)
            sb.Append(' ');
    }

    private static bool IsLikelyAnimatedSibling(HtmlNode sibling, HtmlNode sizer)
    {
        // <br> / empty blocks are word boundaries, not typewriter leftovers.
        if (sibling.Name.Equals("br", StringComparison.OrdinalIgnoreCase)
            || sibling.Name.Equals("hr", StringComparison.OrdinalIgnoreCase))
            return false;

        var siblingText = Collapse(WebUtility.HtmlDecode(VisibleTextExtractor.Extract(sibling)));
        var sizerText = Collapse(WebUtility.HtmlDecode(Extract(sizer)));
        if (string.IsNullOrEmpty(siblingText))
            return true;
        if (sizerText.StartsWith(siblingText, StringComparison.OrdinalIgnoreCase))
            return true;
        // Fallback: drop the visible sibling anyway rather than concatenating both.
        var gsv = sibling.GetAttributeValue("data-gsv", "");
        return gsv.Equals("visible", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(gsv);
    }

    private static void Walk(HtmlNode node, StringBuilder sb)
    {
        if (node.NodeType == HtmlNodeType.Text)
        {
            sb.Append(node.InnerText);
            return;
        }

        if (node.NodeType != HtmlNodeType.Element && node.NodeType != HtmlNodeType.Document)
            return;

        if (IsDesktopOnly(node) || SkipTags.Contains(node.Name))
            return;

        var block = BlockTags.Contains(node.Name);
        if (block)
            sb.Append(' ');

        foreach (var child in node.ChildNodes)
            Walk(child, sb);

        if (block)
            sb.Append(' ');
    }

    private static bool IsDesktopOnly(HtmlNode node) =>
        node.GetAttributeValue("data-gsv", "").Equals("desktop-only", StringComparison.OrdinalIgnoreCase);

    internal static string Collapse(string text) =>
        string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

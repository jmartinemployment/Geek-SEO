using System.Net;
using System.Text;
using GeekSeo.Application.Models.Seo;
using HtmlAgilityPack;

namespace GeekSeo.Application.Services.Seo;

public static class ContentBodyLinkInserter
{
    public static (string Html, int AppliedCount) ApplyBodyLinks(
        string currentHtml,
        IReadOnlyList<BodyLinkInsertionInstruction> instructions)
    {
        if (string.IsNullOrWhiteSpace(currentHtml) || instructions.Count == 0)
            return (currentHtml, 0);

        var doc = new HtmlDocument();
        doc.LoadHtml(currentHtml);

        var applied = 0;
        var unmatched = new List<BodyLinkInsertionInstruction>();

        foreach (var instruction in instructions)
        {
            if (!instruction.IsTargetActive)
                continue;
            if (string.IsNullOrWhiteSpace(instruction.TargetPath) ||
                string.IsNullOrWhiteSpace(instruction.AnchorText))
            {
                continue;
            }

            var headingNode = FindHeading(doc, instruction.TargetHeadingId);
            if (headingNode is null)
            {
                unmatched.Add(instruction);
                continue;
            }

            switch (instruction.PlacementStrategy)
            {
                case BodyLinkPlacementStrategy.ReplaceExistingText:
                    if (ExecuteReplaceText(headingNode, instruction))
                        applied++;
                    break;
                case BodyLinkPlacementStrategy.AppendToParagraph:
                    if (ExecuteAppendParagraph(headingNode, instruction))
                        applied++;
                    break;
                case BodyLinkPlacementStrategy.SectionFooter:
                    ExecuteSectionFooter(headingNode, instruction);
                    applied++;
                    break;
            }
        }

        if (unmatched.Count > 0)
            applied += AppendRelatedGuidesFallback(doc, unmatched);

        return (doc.DocumentNode.OuterHtml, applied);
    }

    private static HtmlNode? FindHeading(HtmlDocument doc, string targetHeadingId)
    {
        if (string.IsNullOrWhiteSpace(targetHeadingId))
            return null;

        var trimmed = targetHeadingId.Trim();
        var byId = doc.DocumentNode.SelectSingleNode($"//h2[@id={XPathLiteral(trimmed)}]");
        if (byId is not null)
            return byId;

        foreach (var h2 in doc.DocumentNode.SelectNodes("//h2") ?? Enumerable.Empty<HtmlNode>())
        {
            var text = HtmlEntity.DeEntitize(h2.InnerText).Trim();
            if (text.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                return h2;
        }

        return null;
    }

    private static bool ExecuteReplaceText(HtmlNode headingNode, BodyLinkInsertionInstruction instruction)
    {
        var anchorHtml = BuildAnchor(instruction.TargetPath, instruction.AnchorText);
        var currentNode = headingNode.NextSibling;

        while (currentNode is not null && !IsH2(currentNode))
        {
            if (currentNode.NodeType == HtmlNodeType.Element &&
                currentNode.InnerText.Contains(instruction.AnchorText, StringComparison.Ordinal))
            {
                currentNode.InnerHtml = currentNode.InnerHtml.Replace(
                    instruction.AnchorText,
                    anchorHtml,
                    StringComparison.Ordinal);
                return true;
            }

            currentNode = currentNode.NextSibling;
        }

        return false;
    }

    private static bool ExecuteAppendParagraph(HtmlNode headingNode, BodyLinkInsertionInstruction instruction)
    {
        var firstParagraph = FindFirstParagraphInSection(headingNode);
        if (firstParagraph is null)
            return false;

        var phrase = instruction.ContextPhrase;
        if (string.IsNullOrWhiteSpace(phrase))
        {
            phrase = $"Learn more in our <a href=\"{instruction.TargetPath}\">{instruction.AnchorText}</a>.";
        }
        else
        {
            phrase = phrase
                .Replace("{targetPath}", instruction.TargetPath, StringComparison.Ordinal)
                .Replace("{anchorText}", instruction.AnchorText, StringComparison.Ordinal);
        }

        firstParagraph.InnerHtml = $"{firstParagraph.InnerHtml.TrimEnd()} {phrase.Trim()}";
        return true;
    }

    private static void ExecuteSectionFooter(HtmlNode headingNode, BodyLinkInsertionInstruction instruction)
    {
        var lastNodeInSection = headingNode;
        while (lastNodeInSection.NextSibling is HtmlNode next && !IsH2(next))
            lastNodeInSection = next;

        var doc = headingNode.OwnerDocument ?? throw new InvalidOperationException("Heading node has no owner document.");
        var footer = doc.CreateElement("div");
        footer.SetAttributeValue("class", "related-guide-box");
        footer.InnerHtml =
            $"<p><strong>Related Guide:</strong> {BuildAnchor(instruction.TargetPath, instruction.AnchorText)}</p>";

        lastNodeInSection.ParentNode?.InsertAfter(footer, lastNodeInSection);
    }

    private static int AppendRelatedGuidesFallback(
        HtmlDocument doc,
        IReadOnlyList<BodyLinkInsertionInstruction> instructions)
    {
        var listItems = instructions
            .Select(i => $"<li>{BuildAnchor(i.TargetPath, i.AnchorText)}</li>")
            .ToList();

        if (listItems.Count == 0)
            return 0;

        var existing = doc.DocumentNode.SelectSingleNode("//ul[contains(@class,'related-guides')]");
        if (existing is not null)
        {
            existing.InnerHtml += string.Concat(listItems);
            return listItems.Count;
        }

        var ownerDoc = doc;
        var container = ownerDoc.CreateElement("div");
        container.SetAttributeValue("class", "related-guides");
        container.InnerHtml =
            $"<h2>Related guides</h2><ul class=\"related-guides\">{string.Concat(listItems)}</ul>";

        var faqHeading = doc.DocumentNode.SelectSingleNode(
            "//h2[contains(translate(normalize-space(.),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'frequently asked')]");
        if (faqHeading?.ParentNode is not null)
        {
            faqHeading.ParentNode.InsertBefore(container, faqHeading);
            return listItems.Count;
        }

        doc.DocumentNode.AppendChild(container);
        return listItems.Count;
    }

    private static HtmlNode? FindFirstParagraphInSection(HtmlNode headingNode)
    {
        var currentNode = headingNode.NextSibling;
        while (currentNode is not null && !IsH2(currentNode))
        {
            if (currentNode.NodeType == HtmlNodeType.Element &&
                string.Equals(currentNode.Name, "p", StringComparison.OrdinalIgnoreCase))
            {
                return currentNode;
            }

            currentNode = currentNode.NextSibling;
        }

        return null;
    }

    private static bool IsH2(HtmlNode node) =>
        node.NodeType == HtmlNodeType.Element &&
        string.Equals(node.Name, "h2", StringComparison.OrdinalIgnoreCase);

    private static string BuildAnchor(string targetPath, string anchorText)
    {
        var href = WebUtility.HtmlEncode(targetPath.Trim());
        var text = WebUtility.HtmlEncode(anchorText.Trim());
        return $"<a href=\"{href}\">{text}</a>";
    }

    private static string XPathLiteral(string value)
    {
        if (!value.Contains('\'', StringComparison.Ordinal))
            return $"'{value}'";

        if (!value.Contains('"', StringComparison.Ordinal))
            return $"\"{value}\"";

        var builder = new StringBuilder();
        builder.Append("concat(");
        var parts = value.Split('\'');
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0)
                builder.Append(", \"'\", ");

            builder.Append('\'').Append(parts[i]).Append('\'');
        }

        builder.Append(')');
        return builder.ToString();
    }
}

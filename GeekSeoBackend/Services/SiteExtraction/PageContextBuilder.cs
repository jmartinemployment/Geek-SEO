using System.Net;
using System.Text;
using GeekSeo.Application.Models.Seo;
using HtmlAgilityPack;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Serializes the extracted heading tree to flat PageContext (headings + Markdown).
/// Does not guess <c>//main</c> / <c>//article</c>; every heading the tree saw is kept.
/// </summary>
public static class PageContextBuilder
{
    public static PageContext FromHtml(string html)
    {
        var sections = PageSectionTreeBuilder.Build(html);
        var (title, description) = ExtractTitleAndDescription(html);
        return FromSections(sections, title, description);
    }

    public static PageContext FromSections(
        IReadOnlyList<PageSection> sections,
        string? title = null,
        string? description = null)
    {
        var headings = new List<string>();
        var md = new StringBuilder();
        foreach (var section in sections)
            WriteSection(section, headings, md);

        var firstH1 = headings.Count > 0 ? headings[0] : "";
        return new PageContext
        {
            Title = string.IsNullOrWhiteSpace(title) ? firstH1 : title.Trim(),
            Description = (description ?? "").Trim(),
            Headings = headings,
            MainContentMarkdown = md.ToString().TrimEnd(),
        };
    }

    private static void WriteSection(PageSection section, List<string> headings, StringBuilder md)
    {
        var heading = (section.HeadingText ?? "").Trim();
        if (heading.Length > 0)
        {
            headings.Add(heading);
            var level = Math.Clamp(section.Level, 1, 6);
            md.Append(new string('#', level));
            md.Append(' ');
            md.AppendLine(heading);
            md.AppendLine();
        }

        foreach (var paragraph in section.Paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
                continue;
            md.AppendLine(paragraph.Trim());
            md.AppendLine();
        }

        var emittedLink = false;
        foreach (var link in section.Links)
        {
            var text = (link.Text ?? "").Trim();
            var href = (link.Href ?? "").Trim();
            if (text.Length == 0 || href.Length == 0)
                continue;
            md.Append("- [");
            md.Append(text);
            md.Append("](");
            md.Append(href);
            md.AppendLine(")");
            emittedLink = true;
        }

        if (emittedLink)
            md.AppendLine();

        foreach (var child in section.Children)
            WriteSection(child, headings, md);
    }

    private static (string Title, string Description) ExtractTitleAndDescription(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return ("", "");

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var title = WebUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? "").Trim();
        var meta = doc.DocumentNode.SelectSingleNode("//meta[@name='description']")
            ?? doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']");
        var description = WebUtility.HtmlDecode(meta?.GetAttributeValue("content", "") ?? "").Trim();
        return (title, description);
    }
}

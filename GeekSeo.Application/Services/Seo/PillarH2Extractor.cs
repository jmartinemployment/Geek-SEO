using System.Net;
using HtmlAgilityPack;

namespace GeekSeo.Application.Services.Seo;

public sealed record PillarH2Heading(string? Id, string Text);

public static class PillarH2Extractor
{
    public static IReadOnlyList<PillarH2Heading> ExtractBodyHeadings(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<PillarH2Heading>();
        foreach (var h2 in doc.DocumentNode.SelectNodes("//h2") ?? Enumerable.Empty<HtmlNode>())
        {
            var text = WebUtility.HtmlDecode(HtmlEntity.DeEntitize(h2.InnerText)).Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (text.Contains("frequently asked", StringComparison.OrdinalIgnoreCase))
                break;

            var id = h2.GetAttributeValue("id", string.Empty)?.Trim();
            results.Add(new PillarH2Heading(
                string.IsNullOrWhiteSpace(id) ? null : id,
                text));
        }

        return results;
    }

    public static string ResolveHint(PillarH2Heading heading) =>
        !string.IsNullOrWhiteSpace(heading.Id) ? heading.Id! : heading.Text;
}

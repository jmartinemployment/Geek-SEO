using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ScoreSuggestionApplicator
{
    public static string? TryApplyDeterministic(
        string suggestionId,
        string contentHtml,
        string keyword,
        int avgTitleLength,
        string plainText,
        IReadOnlyList<SerpOrganicResult> organicResults)
    {
        return suggestionId switch
        {
            "title_keyword" => ApplyTitleKeyword(contentHtml, keyword, avgTitleLength),
            "meta_description" => ApplyMetaDescription(contentHtml, keyword, plainText),
            "geo_citations" => ApplyExternalCitations(contentHtml, organicResults),
            "geo_structure" => ApplyFaqStructure(contentHtml, keyword),
            _ => null,
        };
    }

    public static string ProposeTitle(string? currentTitle, string keyword, int targetLength)
    {
        var title = string.IsNullOrWhiteSpace(currentTitle) ? keyword : currentTitle.Trim();
        if (!string.IsNullOrWhiteSpace(keyword)
            && !title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        {
            title = $"{keyword}: {title}";
        }

        var goal = targetLength is >= 30 and <= 65 ? targetLength : 55;
        if (title.Length <= goal)
            return title;

        if (title.Length > goal)
        {
            var cut = title[..goal];
            var lastSpace = cut.LastIndexOf(' ');
            return lastSpace > goal / 2 ? cut[..lastSpace].TrimEnd() : cut.TrimEnd();
        }

        return title;
    }

    public static string ProposeMetaDescription(string plainText, string keyword)
    {
        var body = string.IsNullOrWhiteSpace(plainText) ? keyword : plainText.Trim();
        body = Regex.Replace(body, @"\s+", " ");
        if (body.Length > 130)
            body = body[..130].TrimEnd();

        var meta = body.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            ? body
            : $"{keyword} — {body}";

        return meta.Length > 160 ? meta[..157].TrimEnd() + "…" : meta;
    }

    private static string ApplyTitleKeyword(string html, string keyword, int avgTitleLength)
    {
        var current = ExtractTagInner(html, "h1");
        var proposed = ProposeTitle(current, keyword, avgTitleLength);
        return ReplaceTagInner(html, "h1", proposed);
    }

    private static string ApplyMetaDescription(string html, string keyword, string plainText)
    {
        var meta = WebUtility.HtmlEncode(ProposeMetaDescription(plainText, keyword));
        var tag = $"<meta name=\"description\" content=\"{meta}\">";

        if (html.Contains("name=\"description\"", StringComparison.OrdinalIgnoreCase))
        {
            return Regex.Replace(
                html,
                "<meta[^>]*name=[\"']description[\"'][^>]*>",
                tag,
                RegexOptions.IgnoreCase);
        }

        return tag + "\n" + html;
    }

    private static string ApplyExternalCitations(string html, IReadOnlyList<SerpOrganicResult> organicResults)
    {
        var linked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(html, "href=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase))
        {
            if (match.Groups.Count > 1)
                linked.Add(match.Groups[1].Value);
        }

        var picks = organicResults
            .Where(r => !string.IsNullOrWhiteSpace(r.Url))
            .Where(r => !linked.Contains(r.Url))
            .Take(3)
            .ToList();

        if (picks.Count == 0)
            return html;

        var links = picks
            .Select(r =>
                $"<a href=\"{WebUtility.HtmlEncode(r.Url)}\" rel=\"noopener noreferrer\">{WebUtility.HtmlEncode(r.Title)}</a>")
            .ToList();

        var paragraph = links.Count switch
        {
            1 => $"According to {links[0]}, this topic is widely covered by industry leaders.",
            2 => $"According to {links[0]} and {links[1]}, authoritative sources reinforce these recommendations.",
            _ => $"According to {links[0]}, {links[1]}, and {links[2]}, leading references support this guidance.",
        };

        return html.TrimEnd() +
               "\n<h2>Sources</h2>\n" +
               $"<p>{paragraph}</p>\n";
    }

    public static string DescribeDeterministicFailure(string suggestionId, string html, string keyword) =>
        suggestionId switch
        {
            "title_keyword" =>
                "The title already matches the suggested change. Refresh the score to clear this hint.",
            "meta_description" =>
                "A meta description is already present with the suggested content.",
            "geo_citations" =>
                "No new external sources were available to link, or they are already cited.",
            "geo_structure" when ArticleClosingFaqEnricher.HasClosingFaqSection(html) =>
                "The closing FAQ section is already present.",
            "geo_structure" =>
                "Could not append the FAQ block automatically. Add it manually in the editor.",
            _ => "Could not apply this change automatically.",
        };

    private static string ApplyFaqStructure(string html, string keyword)
    {
        if (ArticleClosingFaqEnricher.HasClosingFaqSection(html))
            return html;

        var topic = string.IsNullOrWhiteSpace(keyword) ? "this topic" : keyword;
        var block = $"""
            <h2>Frequently asked questions</h2>
            <h3>What is {WebUtility.HtmlEncode(topic)}?</h3>
            <p>Add a concise definition here so readers and AI models can extract a direct answer.</p>
            <h3>Why does {WebUtility.HtmlEncode(topic)} matter?</h3>
            <p>Explain the main benefit or outcome in one or two short sentences.</p>
            <ul>
            <li>Key point one</li>
            <li>Key point two</li>
            </ul>
            """;

        return html.TrimEnd() + "\n" + block;
    }

    private static string ExtractTagInner(string html, string tag)
    {
        var pattern = $@"<{tag}[^>]*>(.*?)</{tag}>";
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static string ReplaceTagInner(string html, string tag, string inner)
    {
        var pattern = $@"<{tag}[^>]*>.*?</{tag}>";
        var replacement = $"<{tag}>{WebUtility.HtmlEncode(inner)}</{tag}>";
        if (Regex.IsMatch(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            return Regex.Replace(html, pattern, replacement, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        return $"<{tag}>{WebUtility.HtmlEncode(inner)}</{tag}>\n{html}";
    }
}

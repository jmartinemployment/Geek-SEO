using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static partial class ScoreSuggestionApplicator
{
    public static string? TryApplyDeterministic(
        string suggestionId,
        string contentHtml,
        string keyword,
        int avgTitleLength,
        string plainText,
        IReadOnlyList<SerpOrganicResult> organicResults,
        string? featuredSnippetText = null)
    {
        return suggestionId switch
        {
            "title_keyword" => ApplyTitleKeyword(contentHtml, keyword, avgTitleLength),
            "meta_description" => ApplyMetaDescription(contentHtml, keyword, plainText),
            "geo_citations" => TryAppendSourcesFromSerp(contentHtml, organicResults),
            "geo_structure" => AppendClosingFaq(contentHtml, keyword, []),
            "serp_featured_snippet" => InsertFeaturedSnippetDirectAnswer(
                contentHtml,
                keyword,
                plainText,
                featuredSnippetText),
            _ => null,
        };
    }

    public static string ProposeFeaturedSnippetDirectAnswer(
        string keyword,
        string plainText,
        string? featuredSnippetText = null)
    {
        var seed = SerpCaptureTextSanitizer.Sanitize(featuredSnippetText);
        if (string.IsNullOrWhiteSpace(seed))
            seed = BuildDirectAnswerSeed(keyword, plainText);

        seed = Regex.Replace(seed, @"\s+", " ").Trim();
        if (string.IsNullOrWhiteSpace(seed))
            seed = keyword;

        if (!string.IsNullOrWhiteSpace(keyword)
            && !seed.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        {
            seed = $"{keyword} is {seed.TrimEnd('.')}.";
        }

        return TrimToWordRange(seed, minWords: 40, maxWords: 60);
    }

    public static bool HasDirectAnswerAfterFirstH2(string html)
    {
        var match = FirstH2Regex().Match(html);
        if (!match.Success)
            return false;

        var after = html[(match.Index + match.Length)..].TrimStart();
        var paragraph = FirstParagraphRegex().Match(after);
        if (!paragraph.Success)
            return false;

        var inner = Regex.Replace(paragraph.Groups[1].Value, "<[^>]+>", " ");
        inner = Regex.Replace(inner, @"\s+", " ").Trim();
        var words = CountWords(inner);
        return words is >= 35 and <= 65;
    }

    public static string? InsertFeaturedSnippetDirectAnswer(
        string html,
        string keyword,
        string plainText,
        string? featuredSnippetText = null)
    {
        if (HasDirectAnswerAfterFirstH2(html))
            return null;

        var match = FirstH2Regex().Match(html);
        if (!match.Success)
            return null;

        var answer = ProposeFeaturedSnippetDirectAnswer(keyword, plainText, featuredSnippetText);
        var paragraph = $"<p>{WebUtility.HtmlEncode(answer)}</p>";
        var insertAt = match.Index + match.Length;
        return html[..insertAt] + "\n" + paragraph + "\n" + html[insertAt..];
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

        if (!string.IsNullOrWhiteSpace(keyword)
            && body.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
        {
            body = body[keyword.Length..].TrimStart(' ', '—', '-', ':');
        }

        if (body.Length > 130)
            body = body[..130].TrimEnd();

        if (string.IsNullOrWhiteSpace(body))
            body = keyword;

        var meta = body.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            ? body
            : $"{keyword}: {body}";

        return meta.Length > 160 ? meta[..157].TrimEnd() + "…" : meta;
    }

    public static string AppendClosingFaq(
        string html,
        string keyword,
        IEnumerable<string> serpPaaQuestions)
    {
        if (ArticleClosingFaqEnricher.HasCompleteClosingFaqSection(html))
            return html;

        var questions = ContentWritingRules.BuildClosingFaqQuestions(keyword, serpPaaQuestions, null);
        var builder = new System.Text.StringBuilder(html.TrimEnd());
        if (builder.Length > 0)
            builder.Append('\n');
        builder.Append("<h2>").Append(ContentWritingRules.ClosingFaqHeading).Append("</h2>\n");
        foreach (var question in questions)
        {
            builder.Append("<h3>").Append(WebUtility.HtmlEncode(question)).Append("</h3>\n");
            builder.Append("<p>Expand this answer in the editor.</p>\n");
        }

        return builder.ToString().TrimEnd();
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

    public static bool HasUsableSerpCitationPicks(string html, IReadOnlyList<SerpOrganicResult> organicResults) =>
        SelectSerpCitationPicks(html, organicResults).Count > 0;

    public static string? TryAppendSourcesFromSerp(string html, IReadOnlyList<SerpOrganicResult> organicResults)
    {
        if (HasSourcesSection(html))
            return null;

        var picks = SelectSerpCitationPicks(html, organicResults);
        if (picks.Count == 0)
            return null;

        var links = picks
            .Select(x =>
                $"<a href=\"{WebUtility.HtmlEncode(x.Url)}\" rel=\"noopener noreferrer\">{WebUtility.HtmlEncode(x.Label)}</a>")
            .ToList();

        return AppendSourcesBlock(html, BuildSourcesParagraph(links));
    }

    public static string? TryAppendSourcesFromDiscovered(string html, IReadOnlyList<DiscoveredSource> sources)
    {
        if (HasSourcesSection(html))
            return null;

        var picks = sources
            .Where(s => IsValidExternalUrl(s.Url))
            .Take(3)
            .ToList();
        if (picks.Count == 0)
            return null;

        var items = picks
            .Select(source =>
            {
                var label = string.IsNullOrWhiteSpace(source.AnchorText) ? source.Title : source.AnchorText!;
                return $"<li><a href=\"{WebUtility.HtmlEncode(source.Url.Trim())}\" rel=\"noopener noreferrer\">{WebUtility.HtmlEncode(label.Trim())}</a></li>";
            });

        return AppendSourcesBlock(
            html,
            $"<ul>\n{string.Join("\n", items)}\n</ul>");
    }

    private static bool HasSourcesSection(string html) =>
        html.Contains("<h2>Sources</h2>", StringComparison.OrdinalIgnoreCase);

    private static string AppendSourcesBlock(string html, string body) =>
        html.TrimEnd() + "\n<h2>Sources</h2>\n" + body + "\n";

    private static string BuildSourcesParagraph(IReadOnlyList<string> links) =>
        links.Count switch
        {
            1 => $"<p>According to {links[0]}, this topic is widely covered by industry leaders.</p>",
            2 => $"<p>According to {links[0]} and {links[1]}, authoritative sources reinforce these recommendations.</p>",
            _ => $"<p>According to {links[0]}, {links[1]}, and {links[2]}, leading references support this guidance.</p>",
        };

    private static List<(string Url, string Label)> SelectSerpCitationPicks(
        string html,
        IReadOnlyList<SerpOrganicResult> organicResults)
    {
        var linked = CollectLinkedUrls(html);
        return organicResults
            .Select(r => new { Result = r, Url = ResolveOrganicUrl(r) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))
            .Where(x => !IsUrlAlreadyLinked(x.Url, linked))
            .Take(3)
            .Select(x => (x.Url, x.Result.Title ?? x.Result.Domain ?? "Source"))
            .ToList();
    }

    private static bool IsValidExternalUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));

    private static HashSet<string> CollectLinkedUrls(string html)
    {
        var linked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(html, "href=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase))
        {
            if (match.Groups.Count <= 1)
                continue;

            var href = match.Groups[1].Value.Trim();
            if (href.Length == 0)
                continue;

            linked.Add(NormalizeUrl(href));
            var host = TryGetHost(href);
            if (!string.IsNullOrWhiteSpace(host))
                linked.Add(host);
        }

        return linked;
    }

    private static bool IsUrlAlreadyLinked(string url, HashSet<string> linked)
    {
        var normalized = NormalizeUrl(url);
        if (linked.Contains(normalized))
            return true;

        var host = TryGetHost(url);
        return !string.IsNullOrWhiteSpace(host) && linked.Contains(host);
    }

    private static string ResolveOrganicUrl(SerpOrganicResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Url))
            return result.Url.Trim();

        if (string.IsNullOrWhiteSpace(result.Domain))
            return string.Empty;

        var domain = result.Domain.Trim().TrimStart('/');
        return domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? domain
            : $"https://{domain}";
    }

    private static string NormalizeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.Trim().TrimEnd('/');

        var builder = new UriBuilder(uri) { Fragment = string.Empty, Query = string.Empty };
        return builder.Uri.ToString().TrimEnd('/');
    }

    private static string? TryGetHost(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host.ToLowerInvariant() : null;

    public static string DescribeDeterministicFailure(string suggestionId, string html, string keyword) =>
        suggestionId switch
        {
            "title_keyword" =>
                "The title already matches the suggested change. Refresh the score to clear this hint.",
            "meta_description" =>
                "A meta description is already present with the suggested content.",
            "geo_citations" when HasSourcesSection(html) =>
                "A Sources section is already present. Refresh the score to clear this hint.",
            "geo_citations" =>
                "No new external sources were available to link, or they are already cited.",
            "geo_structure" when ArticleClosingFaqEnricher.HasCompleteClosingFaqSection(html) =>
                "The closing FAQ section is already present with full answers.",
            "geo_structure" when ArticleClosingFaqEnricher.HasClosingFaqSection(html) =>
                "FAQ headings are present but answers are still placeholders. Try Apply again to generate full answers.",
            "geo_structure" =>
                "Could not append the FAQ block automatically. Add it manually in the editor.",
            "serp_featured_snippet" when HasDirectAnswerAfterFirstH2(html) =>
                "A direct answer paragraph is already present after the first H2.",
            "serp_featured_snippet" =>
                "Could not insert the direct answer. Add an H2 section first, then try again.",
            _ => "Could not apply this change automatically.",
        };

    private static string BuildDirectAnswerSeed(string keyword, string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return keyword;

        var sentences = plainText
            .Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .ToList();

        if (sentences.Count == 0)
            return plainText;

        var builder = new System.Text.StringBuilder();
        foreach (var sentence in sentences)
        {
            if (builder.Length > 0)
                builder.Append(' ');
            builder.Append(sentence.TrimEnd('.'));
            if (CountWords(builder.ToString()) >= 40)
                break;
        }

        var result = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? plainText : result + ".";
    }

    private static string TrimToWordRange(string text, int minWords, int maxWords)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= maxWords)
        {
            if (words.Length >= minWords)
                return string.Join(' ', words);

            var padded = text.TrimEnd('.');
            while (words.Length < minWords)
            {
                padded += " This guide explains what matters, who it helps, and the practical steps to get results.";
                words = padded.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            }

            return TrimToWordRange(padded, minWords, maxWords);
        }

        var trimmed = string.Join(' ', words.Take(maxWords));
        if (!trimmed.EndsWith('.'))
            trimmed += ".";
        return trimmed;
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    [GeneratedRegex(@"<h2\b[^>]*>.*?</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex FirstH2Regex();

    [GeneratedRegex(@"<p\b[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex FirstParagraphRegex();

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

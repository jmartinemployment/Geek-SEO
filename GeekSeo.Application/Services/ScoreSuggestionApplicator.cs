using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static partial class ScoreSuggestionApplicator
{
    public static string? TryAppendSchemaScripts(string html, IReadOnlyList<string> schemaScripts)
    {
        if (schemaScripts.Count == 0 || HasArticleSchema(html))
            return null;

        return $"{html.TrimEnd()}\n{string.Join("\n", schemaScripts)}";
    }

    public static bool HasArticleSchema(string html) =>
        html.Contains("application/ld+json", StringComparison.OrdinalIgnoreCase)
        && (html.Contains("TechArticle", StringComparison.OrdinalIgnoreCase)
            || html.Contains("\"Article\"", StringComparison.OrdinalIgnoreCase)
            || html.Contains("@type\":\"Article", StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<string> ExtractSchemaScripts(string html)
    {
        var scripts = new List<string>();
        foreach (Match match in SchemaScriptRegex().Matches(html))
            scripts.Add(match.Value);

        return scripts;
    }

    public static IReadOnlyList<string> InferSchemaTypes(IReadOnlyList<string> schemaScripts)
    {
        var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var script in schemaScripts)
        {
            if (script.Contains("TechArticle", StringComparison.OrdinalIgnoreCase))
                types.Add("TechArticle");
            if (script.Contains("FAQPage", StringComparison.OrdinalIgnoreCase))
                types.Add("FAQPage");
            if (script.Contains("SoftwareApplication", StringComparison.OrdinalIgnoreCase))
                types.Add("SoftwareApplication");
            if (types.Count == 0 && script.Contains("\"Article\"", StringComparison.OrdinalIgnoreCase))
                types.Add("Article");
        }

        return types.Count == 0 ? ["TechArticle"] : types.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string? TryApplyDeterministic(
        string suggestionId,
        string contentHtml,
        string keyword,
        int avgTitleLength,
        string plainText,
        IReadOnlyList<SerpOrganicResult> organicResults,
        string? featuredSnippetText = null,
        string? aiOverviewText = null)
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
            "serp_ai_overview" => InsertAiOverviewDefinition(
                contentHtml,
                keyword,
                plainText,
                aiOverviewText),
            _ => null,
        };
    }

    public static string ProposeAiOverviewDefinition(
        string keyword,
        string plainText,
        string? aiOverviewText = null)
    {
        var seed = SerpCaptureTextSanitizer.Sanitize(aiOverviewText);
        if (string.IsNullOrWhiteSpace(seed))
            seed = BuildConciseDefinitionSeed(keyword, plainText);

        seed = Regex.Replace(seed, @"\s+", " ").Trim();
        if (string.IsNullOrWhiteSpace(seed))
            seed = keyword;

        if (!string.IsNullOrWhiteSpace(keyword)
            && !seed.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        {
            seed = $"{keyword} is {seed.TrimEnd('.')}.";
        }

        return TrimToWordRange(seed, minWords: 20, maxWords: 35);
    }

    public static bool HasConciseDefinitionInOpening(string html, string keyword)
    {
        var opening = ExtractOpeningParagraphText(html);
        if (string.IsNullOrWhiteSpace(opening))
            return false;

        var words = CountWords(opening);
        if (words is < 15 or > 45)
            return false;

        return string.IsNullOrWhiteSpace(keyword)
            || opening.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    public static string? InsertAiOverviewDefinition(
        string html,
        string keyword,
        string plainText,
        string? aiOverviewText = null)
    {
        if (HasConciseDefinitionInOpening(html, keyword))
            return null;

        var definition = ProposeAiOverviewDefinition(keyword, plainText, aiOverviewText);
        var paragraph = $"<p>{WebUtility.HtmlEncode(definition)}</p>";

        var h1 = H1Regex().Match(html);
        if (!h1.Success)
            return paragraph + "\n" + html.TrimStart();

        var insertAt = h1.Index + h1.Length;
        return html[..insertAt] + "\n" + paragraph + "\n" + html[insertAt..].TrimStart();
    }

    private static string? ExtractOpeningParagraphText(string html)
    {
        var searchFrom = 0;
        var h1 = H1Regex().Match(html);
        if (h1.Success)
            searchFrom = h1.Index + h1.Length;

        var after = html[searchFrom..].TrimStart();
        var paragraph = FirstParagraphRegex().Match(after);
        if (!paragraph.Success)
            return null;

        return Regex.Replace(paragraph.Groups[1].Value, "<[^>]+>", " ").Trim();
    }

    private static string BuildConciseDefinitionSeed(string keyword, string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return keyword;

        var sentences = plainText
            .Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .Take(2)
            .ToList();

        if (sentences.Count == 0)
            return plainText;

        var result = string.Join(' ', sentences).Trim();
        return string.IsNullOrWhiteSpace(result) ? plainText : result.TrimEnd('.') + ".";
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

    public static string? TryAppendSourcesFromSerp(string html, IReadOnlyList<SerpOrganicResult> organicResults) =>
        TryInsertInlineCitations(html, SelectSerpCitationPicks(html, organicResults));

    public static string? TryAppendSourcesFromDiscovered(string html, IReadOnlyList<DiscoveredSource> sources)
    {
        var picks = SelectDiscoveredCitationPicks(html, sources);
        return TryInsertInlineCitations(html, picks);
    }

    public static string? TryInsertResearchCitation(string html, string url, string? title)
    {
        if (!IsValidExternalUrl(url))
            return null;

        var label = string.IsNullOrWhiteSpace(title) ? url.Trim() : title.Trim();
        return TryInsertInlineCitations(html, [(url.Trim(), label)]);
    }

    public static string? TryApplyEeat(string suggestionId, string html, EeatApplyContext context) =>
        suggestionId switch
        {
            "eeat_first_hand_experience" => InsertFirstHandExperience(html, context),
            "eeat_author_bio" => InsertAuthorBio(html, context),
            "eeat_original_media" => InsertOriginalMedia(html, context),
            "eeat_freshness_signal" => InsertFreshnessSignal(html),
            _ => null,
        };

    public static bool HasFirstHandExperience(string plainText)
    {
        var lower = plainText.ToLowerInvariant();
        return lower.Contains("our team", StringComparison.Ordinal)
            || lower.Contains("in our experience", StringComparison.Ordinal)
            || lower.Contains("we found that", StringComparison.Ordinal);
    }

    public static bool HasAuthorBio(string plainText)
    {
        var lower = plainText.ToLowerInvariant();
        return lower.Contains("about the author", StringComparison.Ordinal)
            || lower.Contains("written by", StringComparison.Ordinal);
    }

    public static bool HasFreshnessSignal(string plainText)
    {
        var lower = plainText.ToLowerInvariant();
        return lower.Contains("updated", StringComparison.Ordinal)
            || lower.Contains("reviewed", StringComparison.Ordinal);
    }

    private static string? InsertFirstHandExperience(string html, EeatApplyContext context)
    {
        if (HasFirstHandExperience(StripTags(html)))
            return null;

        var topic = string.IsNullOrWhiteSpace(context.Keyword) ? "this topic" : context.Keyword.Trim();
        var who = string.IsNullOrWhiteSpace(context.OrganizationName) ? "Our team" : context.OrganizationName.Trim();
        var paragraph =
            $"<p>In our experience, {who} has implemented {WebUtility.HtmlEncode(topic)} for local SMB clients — " +
            "mapping real workflows, testing integrations, and measuring outcomes before scaling.</p>";

        return InsertAfterFirstH2(html, paragraph);
    }

    private static string? InsertAuthorBio(string html, EeatApplyContext context)
    {
        if (HasAuthorBio(StripTags(html)))
            return null;

        var org = string.IsNullOrWhiteSpace(context.OrganizationName) ? "Our team" : context.OrganizationName.Trim();
        var summary = string.IsNullOrWhiteSpace(context.BusinessSummary)
            ? "implementation-focused AI and automation consulting for small businesses."
            : context.BusinessSummary.Trim();
        if (summary.Length > 220)
            summary = summary[..217].TrimEnd() + "…";

        var orgHtml = WebUtility.HtmlEncode(org);
        var summaryHtml = WebUtility.HtmlEncode(summary);
        var block =
            $"<h2>About the author</h2>\n<p>Written by <strong>{orgHtml}</strong> — {summaryHtml}</p>";

        var faqStart = FindFaqSectionStart(html);
        if (faqStart < 0)
            return html.TrimEnd() + "\n" + block + "\n";

        return html[..faqStart].TrimEnd() + "\n" + block + "\n" + html[faqStart..].TrimStart();
    }

    private static string? InsertOriginalMedia(string html, EeatApplyContext context)
    {
        if (html.Contains("<img", StringComparison.OrdinalIgnoreCase))
            return null;

        string block;
        if (!string.IsNullOrWhiteSpace(context.FeaturedImageUrl))
        {
            var alt = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(context.Keyword) ? "Article illustration" : context.Keyword.Trim());
            var src = context.FeaturedImageUrl.Trim();
            block = $"<figure class=\"featured-image\"><img src=\"{WebUtility.HtmlEncode(src)}\" alt=\"{alt}\" /></figure>";
        }
        else
        {
            var topic = WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(context.Keyword) ? "this topic" : context.Keyword.Trim());
            block =
                $"<figure class=\"image-placeholder\"><p><strong>Image:</strong> Add an original photo or diagram illustrating {topic}.</p></figure>";
        }

        return InsertAfterH1(html, block);
    }

    private static string? InsertFreshnessSignal(string html)
    {
        if (HasFreshnessSignal(StripTags(html)))
            return null;

        var stamp = DateTime.UtcNow.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
        var paragraph = $"<p><em>Last updated: {stamp}</em></p>";
        return InsertAfterH1(html, paragraph);
    }

    private static string? InsertAfterH1(string html, string insertion)
    {
        var match = H1Regex().Match(html);
        if (!match.Success)
            return html.TrimEnd() + "\n" + insertion + "\n";

        var insertAt = match.Index + match.Length;
        return html[..insertAt] + "\n" + insertion + "\n" + html[insertAt..].TrimStart();
    }

    private static string? InsertAfterFirstH2(string html, string insertion)
    {
        var match = FirstH2Regex().Match(html);
        if (!match.Success)
            return html.TrimEnd() + "\n" + insertion + "\n";

        var insertAt = match.Index + match.Length;
        return html[..insertAt] + "\n" + insertion + "\n" + html[insertAt..].TrimStart();
    }

    private static string StripTags(string html) =>
        Regex.Replace(html, "<[^>]+>", " ");

    public static IReadOnlyList<(string Url, string Label)> SelectDiscoveredCitationPicks(
        string html,
        IReadOnlyList<DiscoveredSource> sources)
    {
        var linked = CollectLinkedUrls(html);
        return sources
            .Where(s => IsValidExternalUrl(s.Url))
            .Where(s => AuthoritativeCitationRules.IsAcceptableDiscoveredCitationUrl(s.Url)
                || AuthoritativeCitationRules.IsAuthoritativeCitationUrl(s.Url))
            .Where(s => !IsUrlAlreadyLinked(s.Url.Trim(), linked))
            .Take(3)
            .Select(s => (
                s.Url.Trim(),
                string.IsNullOrWhiteSpace(s.AnchorText) ? s.Title : s.AnchorText!))
            .ToList();
    }

    private static string? TryInsertInlineCitations(
        string html,
        IReadOnlyList<(string Url, string Label)> picks)
    {
        if (picks.Count == 0)
            return null;

        var linked = CollectLinkedUrls(html);
        var fresh = picks
            .Where(pick => !IsUrlAlreadyLinked(pick.Url, linked))
            .Take(3)
            .ToList();
        if (fresh.Count == 0)
            return null;

        var linkList = fresh
            .Select(pick =>
                $"<a href=\"{WebUtility.HtmlEncode(pick.Url)}\" rel=\"noopener noreferrer\">{WebUtility.HtmlEncode(pick.Label.Trim())}</a>")
            .ToList();

        var paragraph = linkList.Count == 1
            ? $"<p>For authoritative context, see {linkList[0]}.</p>"
            : $"<p>For authoritative context, see {string.Join(", ", linkList.Take(linkList.Count - 1))}, and {linkList[^1]}.</p>";

        var faqStart = FindFaqSectionStart(html);
        if (faqStart < 0)
            return html.TrimEnd() + "\n" + paragraph + "\n";

        return html[..faqStart].TrimEnd() + "\n" + paragraph + "\n" + html[faqStart..].TrimStart();
    }

    private static bool HasInlineAuthoritativeCitations(string html)
    {
        foreach (Match match in Regex.Matches(html, "href=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase))
        {
            if (match.Groups.Count <= 1)
                continue;

            var href = match.Groups[1].Value.Trim();
            if (AuthoritativeCitationRules.IsAuthoritativeCitationUrl(href))
                return true;
        }

        return false;
    }

    private static List<(string Url, string Label)> SelectSerpCitationPicks(
        string html,
        IReadOnlyList<SerpOrganicResult> organicResults)
    {
        var linked = CollectLinkedUrls(html);
        var candidates = organicResults
            .Select(r => new { Result = r, Url = ResolveOrganicUrl(r) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))
            .Where(x => AuthoritativeCitationRules.IsAuthoritativeCitationUrl(x.Url))
            .Where(x => !IsUrlAlreadyLinked(x.Url, linked))
            .Take(3)
            .Select(x => (x.Url, x.Result.Domain ?? x.Result.Title ?? "Source"))
            .ToList();

        return candidates;
    }

    private static int FindFaqSectionStart(string html)
    {
        var match = FaqHeadingRegex().Match(html);
        if (match.Success)
            return match.Index;

        var headingIndex = html.IndexOf(ContentWritingRules.ClosingFaqHeading, StringComparison.OrdinalIgnoreCase);
        if (headingIndex < 0)
            return -1;

        var h2Start = html.LastIndexOf("<h2", headingIndex, StringComparison.OrdinalIgnoreCase);
        return h2Start >= 0 ? h2Start : headingIndex;
    }

    [GeneratedRegex("<h2[^>]*>\\s*[^<]*faq[^<]*</h2>", RegexOptions.IgnoreCase)]
    private static partial Regex FaqHeadingRegex();

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
            "geo_citations" when HasInlineAuthoritativeCitations(html) =>
                "Authoritative inline citations are already present. Refresh the score to clear this hint.",
            "geo_citations" =>
                "No new external sources were available to link, or they are already cited.",
            "geo_authority" when HasArticleSchema(html) =>
                "Article JSON-LD is already present. Refresh the score to clear this hint.",
            "geo_authority" =>
                "Could not generate JSON-LD schema. Add headings and body content, then try again.",
            "eeat_first_hand_experience" when HasFirstHandExperience(StripTags(html)) =>
                "A first-hand experience paragraph is already present.",
            "eeat_first_hand_experience" =>
                "Could not insert a first-hand experience paragraph. Add an H2 section first.",
            "eeat_author_bio" when HasAuthorBio(StripTags(html)) =>
                "An author bio is already present.",
            "eeat_author_bio" =>
                "Could not insert an author bio block.",
            "eeat_original_media" when html.Contains("<img", StringComparison.OrdinalIgnoreCase) =>
                "An image is already present in the article.",
            "eeat_original_media" =>
                "Could not insert an image block.",
            "eeat_freshness_signal" when HasFreshnessSignal(StripTags(html)) =>
                "A freshness date is already present.",
            "eeat_freshness_signal" =>
                "Could not insert a last-updated date.",
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
            "serp_ai_overview" when HasConciseDefinitionInOpening(html, keyword) =>
                "A concise definition is already present in the opening paragraph.",
            "serp_ai_overview" =>
                "Could not insert the opening definition. Add an H1 heading first, then try again.",
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

    [GeneratedRegex(@"<script[^>]*type=[""']application/ld\+json[""'][^>]*>[\s\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex SchemaScriptRegex();

    [GeneratedRegex(@"<h1\b[^>]*>.*?</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex H1Regex();

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

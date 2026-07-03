using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using GeekSeo.Application.Services.Seo;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Serp.Models;

namespace SiteAnalyzer2.Serp;

public static class GoogleSerpHtmlParser
{
    private static readonly Regex BlockSignature = new(
        @"(/sorry/|unusual traffic|recaptcha|captcha)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CurrentPageLabel = new(
        @"aria-current=""page""[^>]*>\s*(\d+)\s*<",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StartOffset = new(
        @"[?&]start=(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NumResults = new(
        @"[?&]num=(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SeResultsCount = new(
        @"(?:About\s+)?([\d,]+)\s+results",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ResultStatsCount = new(
        @"id=(?:\\""|"")result-stats(?:\\""|"")[^>]*>(?:About\s+)?([\d,]+)\s+results",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DateLikeSnippet = new(
        @"^(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2},?\s+\d{4}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RelatedQuestionPair = new(
        @"class=""[^""]*related-question-pair[^""]*""[^>]*>.*?<span[^>]*>([^<]+)</span>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex D3Pe6eBlock = new(
        @"jsname=(?:\\""|"")d3PE6e(?:\\""|"")[^>]*>(.*?)</div>\s*<c-wiz",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex AiOverviewUnavailable = new(
        @"An AI Overview is not available[^<]{0,400}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PaidBlock = new(
        @"<div class=""uEierd"">(.*?)</div>\s*<div",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex PaidHref = new(
        @"href=""(https?://[^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PasfChipText = new(
        @"data-hveid=""[^""]+""[^>]*>([^<]{3,200})</div>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PasfSearchQuery = new(
        @"[?&]q=([^""&]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PasfChipSpan = new(
        @"<span[^>]*\bdg6jd\b[^>]*>(.*?)</span>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public static bool LooksLikeBlockedOrCaptcha(string html) => BlockSignature.IsMatch(html);

    public static bool LooksLikeJavaScriptRequired(string html) =>
        html.Contains("enablejs", StringComparison.OrdinalIgnoreCase)
        || html.Contains("/httpservice/retry/enablejs", StringComparison.OrdinalIgnoreCase);

    public static bool LooksLikeSerpPage(string html) =>
        html.Contains("#search", StringComparison.OrdinalIgnoreCase)
        || html.Contains("id=\"search\"", StringComparison.OrdinalIgnoreCase)
        || html.Contains("id=\"rso\"", StringComparison.OrdinalIgnoreCase)
        || html.Contains("data-sokoban-container", StringComparison.OrdinalIgnoreCase)
        || html.Contains("data-hveid", StringComparison.OrdinalIgnoreCase);

    public static SerpLivePageParseResult ParseLivePage(string html, string? keywordOverride = null)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = context.OpenAsync(req => req.Content(html)).GetAwaiter().GetResult();

        var keyword = keywordOverride ?? TryGetSearchKeyword(document) ?? "unknown keyword";
        var page = DetectPageNumber(html);
        var language = DetectLanguage(html);
        var checkUrl = BuildCheckUrl(keyword, language);
        var seResultsCount = TryGetSeResultsCount(html);
        var localPackPresent = DetectLocalPack(html, document);
        var shoppingPresent = DetectShopping(html, document);

        var items = new List<SerpParsedItem>();
        var rankAbsolute = 1;

        items.AddRange(ParseAiOverviewItems(html, page, ref rankAbsolute));
        items.AddRange(ParsePaidItems(html, page, ref rankAbsolute));
        items.AddRange(ParseOrganicItems(document, page, ref rankAbsolute));

        var related = ParseRelatedSearchBlock(document, html, keyword, page, ref rankAbsolute);
        if (related is not null)
            items.Add(related);

        // Legacy PAA nodes are folded into the related_searches block above.

        var organicCount = items.Count(i => i.Type is SerpItemTypes.Organic or SerpItemTypes.Paid);
        var itemTypes = items.Select(i => i.Type).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList();

        return new SerpLivePageParseResult(
            keyword,
            LocationCode: 2840,
            language,
            Device: "desktop",
            Os: "windows",
            Depth: organicCount,
            SeDomain: "google.com",
            checkUrl,
            DateTime.UtcNow,
            seResultsCount,
            page,
            itemTypes,
            localPackPresent,
            shoppingPresent,
            items);
    }

    public static int DetectPageNumber(string html)
    {
        var current = CurrentPageLabel.Match(html);
        if (current.Success && int.TryParse(current.Groups[1].Value, out var labeled) && labeled > 0)
            return labeled;

        var startMatch = StartOffset.Match(html);
        if (!startMatch.Success || !int.TryParse(startMatch.Groups[1].Value, out var start))
            return 1;

        var perPage = 10;
        var numMatch = NumResults.Match(html);
        if (numMatch.Success && int.TryParse(numMatch.Groups[1].Value, out var num) && num > 0)
            perPage = num;

        return start / perPage + 1;
    }

    private static string DetectLanguage(string html)
    {
        var hl = Regex.Match(html, @"[?&]hl=([a-z]{2})", RegexOptions.IgnoreCase);
        return hl.Success ? hl.Groups[1].Value.ToLowerInvariant() : "en";
    }

    private static string BuildCheckUrl(string keyword, string language) =>
        $"https://www.google.com/search?q={Uri.EscapeDataString(keyword)}&hl={language}&gl=US&ie=UTF-8&num=20&pws=0";

    private static long? TryGetSeResultsCount(string html)
    {
        var stats = ResultStatsCount.Match(html);
        if (stats.Success && TryParseResultCount(stats.Groups[1].Value, out var fromStats))
            return fromStats;

        var match = SeResultsCount.Match(html);
        if (!match.Success)
            return null;

        return TryParseResultCount(match.Groups[1].Value, out var count) ? count : null;
    }

    private static bool TryParseResultCount(string raw, out long count)
    {
        var digits = raw.Replace(",", "", StringComparison.Ordinal);
        return long.TryParse(digits, out count);
    }

    private static bool DetectLocalPack(string html, IDocument document) =>
        html.Contains("local pack", StringComparison.OrdinalIgnoreCase)
        || document.QuerySelector("[data-local-attribute], .VkpGBb, .rllt__details") != null;

    private static bool DetectShopping(string html, IDocument document)
    {
        if (html.Contains("shopping_results", StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var block in document.QuerySelectorAll("div.g, div[data-sokoban-container]"))
        {
            var className = block.ClassName ?? "";
            if (className.Contains("shopping", StringComparison.OrdinalIgnoreCase)
                || className.Contains("pla", StringComparison.OrdinalIgnoreCase)
                || className.Contains("commercial", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static List<SerpParsedItem> ParseAiOverviewItems(string html, int page, ref int rankAbsolute)
    {
        var items = new List<SerpParsedItem>();
        if (!html.Contains("AI Overview", StringComparison.OrdinalIgnoreCase))
            return items;

        var text = TryExtractAiOverviewText(html);
        if (LooksLikeAiOverviewContent(text))
        {
            items.Add(new SerpParsedItem(
                SerpItemTypes.AiOverview,
                RankGroup: 1,
                rankAbsolute++,
                page,
                AiOverviewAvailable: true,
                AiOverviewMarkdown: text));
            return items;
        }

        var unavailable = AiOverviewUnavailable.Match(html);
        if (unavailable.Success)
        {
            items.Add(new SerpParsedItem(
                SerpItemTypes.AiOverview,
                RankGroup: 1,
                rankAbsolute++,
                page,
                AiOverviewAvailable: false,
                AiOverviewStatusMessage: WebUtilityDecode(unavailable.Value).Trim()));
            return items;
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            items.Add(new SerpParsedItem(
                SerpItemTypes.AiOverview,
                RankGroup: 1,
                rankAbsolute++,
                page,
                AiOverviewAvailable: true,
                AiOverviewMarkdown: text));
        }

        return items;
    }

    private static List<SerpParsedItem> ParsePaidItems(string html, int page, ref int rankAbsolute)
    {
        var items = new List<SerpParsedItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rankGroup = 1;

        foreach (Match block in PaidBlock.Matches(html))
        {
            var chunk = block.Groups[1].Value;
            var hrefMatch = PaidHref.Match(chunk);
            if (!hrefMatch.Success)
                continue;

            var url = NormalizeResultUrl(hrefMatch.Groups[1].Value);
            if (string.IsNullOrWhiteSpace(url) || ShouldSkipResultUrl(url) || !seen.Add(url))
                continue;

            var title = Regex.Replace(chunk, "<[^>]+>", " ");
            title = Regex.Replace(WebUtilityDecode(title), @"\s+", " ").Trim();
            if (title.Length > 2048)
                title = title[..2048];

            var domain = TryGetDomain(url) ?? "";
            var description = TryExtractPaidDescription(chunk);
            items.Add(new SerpParsedItem(
                SerpItemTypes.Paid,
                rankGroup++,
                rankAbsolute++,
                page,
                Domain: domain,
                Title: string.IsNullOrWhiteSpace(title) ? null : title,
                Url: url,
                Breadcrumb: domain,
                WebsiteName: domain,
                Description: description,
                Ads: true));
        }

        return items;
    }

    private static string? TryExtractAiOverviewText(string html)
    {
        var structured = ExtractAiOverviewFromMarkup(html);
        if (!string.IsNullOrWhiteSpace(structured))
            return structured;

        var start = html.IndexOf("AI Overview", StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return null;

        var slice = html.AsSpan(start, Math.Min(40_000, html.Length - start)).ToString();
        var text = Regex.Replace(slice, "<[^>]+>", " ");
        text = WebUtilityDecode(text);
        text = Regex.Replace(text, @"\s+", " ").Trim();
        text = StripAiOverviewBoilerplate(text);
        if (text.Length == 0)
            return null;

        return text.Length > 8000 ? text[..8000] : text;
    }

    private static string? ExtractAiOverviewFromMarkup(string html)
    {
        var frameworkIdx = html.IndexOf("Step-by-Step", StringComparison.OrdinalIgnoreCase);
        if (frameworkIdx < 0)
            return null;

        var regionStart = Math.Max(0, frameworkIdx - 800);
        var regionEnd = Math.Min(html.Length, frameworkIdx + 18_000);
        var region = html[regionStart..regionEnd];
        var text = StripAiOverviewBoilerplate(StripHtmlTags(WebUtilityDecode(region)));
        text = Regex.Replace(text, @"\s+", " ").Trim();
        if (text.Length < 80)
            return null;

        return text.Length > 8000 ? text[..8000] : text;
    }

    private static bool LooksLikeAiOverviewContent(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var cleaned = StripAiOverviewBoilerplate(text);
        if (cleaned.Length < 80)
            return false;

        if (cleaned.Contains("Implementation Framework", StringComparison.OrdinalIgnoreCase))
            return true;

        if (Regex.IsMatch(cleaned, @"\.\s+[A-Z]") && cleaned.Length >= 120)
            return true;

        return cleaned.Length >= 200;
    }

    private static string StripAiOverviewBoilerplate(string text)
    {
        text = Regex.Replace(text, @"An AI Overview is not available for this search\.?", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"Share more feedback|Report a problem|Close|Listen|Show more", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^AI Overview\s*", "", RegexOptions.IgnoreCase);
        return text.Trim();
    }

    private static string? TryExtractPaidDescription(string chunk)
    {
        var match = Regex.Match(
            chunk,
            @"class=""[^""]*VwiC3b[^""]*""[^>]*>(.*?)</span>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
            return null;

        var text = Regex.Replace(WebUtilityDecode(match.Groups[1].Value), "<[^>]+>", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length >= 12 ? text : null;
    }

    private static List<SerpParsedItem> ParseOrganicItems(IDocument document, int page, ref int rankAbsolute)
    {
        var fromModern = ParseModernOrganicItems(document, page, ref rankAbsolute);
        if (fromModern.Count > 0)
            return fromModern;

        var fromHeadings = ParseHeadingOrganicItems(document, page, ref rankAbsolute);
        if (fromHeadings.Count > 0)
            return fromHeadings;

        return ParseClassicOrganicItems(document, page, ref rankAbsolute);
    }

    private static List<SerpParsedItem> ParseModernOrganicItems(IDocument document, int page, ref int rankAbsolute)
    {
        var results = new List<SerpParsedItem>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rankGroup = 1;

        foreach (var anchor in document.QuerySelectorAll("a[jsname='UWckNb'][href]"))
        {
            var href = NormalizeResultUrl(anchor.GetAttribute("href"));
            if (string.IsNullOrWhiteSpace(href) || ShouldSkipResultUrl(href) || !seenUrls.Add(href))
                continue;

            var block = ResolveOrganicScope(anchor, href);
            var heading = anchor.QuerySelector("h3")
                ?? block.QuerySelector("h3")
                ?? anchor.ParentElement?.QuerySelector("h3");
            var title = heading?.TextContent.Trim() ?? anchor.TextContent.Trim();
            if (string.IsNullOrWhiteSpace(title))
                continue;

            results.Add(BuildOrganicItem(block, title, href, page, rankGroup, rankAbsolute));
            rankGroup++;
            rankAbsolute++;
        }

        return results;
    }

    private static List<SerpParsedItem> ParseClassicOrganicItems(IDocument document, int page, ref int rankAbsolute)
    {
        var results = new List<SerpParsedItem>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rankGroup = 1;

        foreach (var block in document.QuerySelectorAll("div.g, div[data-sokoban-container]"))
        {
            if (IsNonOrganicBlock(block))
                continue;

            var item = TryParseOrganicBlock(block, page, rankGroup, rankAbsolute, seenUrls);
            if (item is null)
                continue;

            results.Add(item);
            rankGroup++;
            rankAbsolute++;
        }

        return results;
    }

    private static List<SerpParsedItem> ParseHeadingOrganicItems(IDocument document, int page, ref int rankAbsolute)
    {
        var results = new List<SerpParsedItem>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rankGroup = 1;

        foreach (var heading in document.QuerySelectorAll("h3"))
        {
            var title = heading.TextContent.Trim();
            if (string.IsNullOrWhiteSpace(title))
                continue;

            var anchor = heading.ParentElement?.QuerySelector("a[href]")
                ?? heading.ParentElement?.ParentElement?.QuerySelector("a[href]");
            if (anchor is null)
                continue;

            var href = NormalizeResultUrl(anchor.GetAttribute("href"));
            if (string.IsNullOrWhiteSpace(href) || !seenUrls.Add(href) || ShouldSkipResultUrl(href))
                continue;

            var block = FindOrganicResultBlock(heading) ?? heading.ParentElement?.ParentElement ?? heading;
            var item = BuildOrganicItem(block, title, href, page, rankGroup, rankAbsolute);
            results.Add(item);
            rankGroup++;
            rankAbsolute++;
        }

        return results;
    }

    private static SerpParsedItem? TryParseOrganicBlock(
        IElement block,
        int page,
        int rankGroup,
        int rankAbsolute,
        HashSet<string> seenUrls)
    {
        var heading = block.QuerySelector("h3");
        var anchor = heading?.ParentElement?.QuerySelector("a[href]") ?? block.QuerySelector("a[href]");
        if (anchor is null)
            return null;

        var href = NormalizeResultUrl(anchor.GetAttribute("href"));
        var title = heading?.TextContent.Trim() ?? anchor.TextContent.Trim();
        if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(title) || !seenUrls.Add(href))
            return null;

        return BuildOrganicItem(block, title, href, page, rankGroup, rankAbsolute);
    }

    private static IElement? FindOrganicResultBlock(IElement heading)
    {
        for (var node = heading.ParentElement; node is not null; node = node.ParentElement)
        {
            var className = node.ClassName ?? "";
            if (className.Contains("g ", StringComparison.OrdinalIgnoreCase)
                || className.StartsWith("g ", StringComparison.OrdinalIgnoreCase)
                || className == "g"
                || className.Contains("MjjYud", StringComparison.OrdinalIgnoreCase)
                || node.HasAttribute("data-sokoban-container")
                || node.QuerySelector(".VwiC3b, .IsZvec, .st, [data-sncf=\"1\"]") != null)
            {
                return node;
            }
        }

        return null;
    }

    private static IElement? FindSnippetElement(IElement block)
    {
        const string selector = ".VwiC3b, .IsZvec, .st, [data-sncf=\"1\"]";
        for (var node = block; node is not null; node = node.ParentElement)
        {
            var snippet = node.QuerySelector(selector);
            if (snippet is not null)
                return snippet;
        }

        return null;
    }

    private static SerpParsedItem BuildOrganicItem(
        IElement block,
        string title,
        string href,
        int page,
        int rankGroup,
        int rankAbsolute)
    {
        var scope = block;
        var (preSnippet, description) = ExtractSnippetParts(scope);
        var cite = scope.QuerySelector("cite");
        var breadcrumbPath = scope.QuerySelector(".ylgVCe, .ob9lvb");
        var breadcrumb = breadcrumbPath?.TextContent.Trim() ?? cite?.TextContent.Trim() ?? "";
        var websiteName = FindWebsiteName(scope);
        if (string.IsNullOrWhiteSpace(websiteName))
            websiteName = breadcrumb.Contains('›', StringComparison.Ordinal) ? null : breadcrumb;

        var domain = TryGetDomain(href) ?? "";
        if (string.IsNullOrWhiteSpace(breadcrumb))
            breadcrumb = domain;
        if (string.IsNullOrWhiteSpace(websiteName))
            websiteName = domain;

        var snippetEl = FindSnippetElement(scope);
        var className = scope.ClassName ?? "";
        var isFeatured = className.Contains("xpdopen", StringComparison.OrdinalIgnoreCase)
            || scope.QuerySelector("[data-attrid='wa:/description']") != null;
        var isVideo = scope.QuerySelector("[data-ved][data-url*='youtube']") != null
            || title.Contains("YouTube", StringComparison.OrdinalIgnoreCase);
        var isImage = scope.QuerySelector("img[src], g-img") != null
            && string.IsNullOrWhiteSpace(description);

        var links = ParseSitelinks(scope);
        var highlighted = ParseHighlighted(snippetEl);
        var ratingJson = TrySerializeRating(scope);
        var priceJson = TrySerializePrice(scope);

        return new SerpParsedItem(
            SerpItemTypes.Organic,
            rankGroup,
            rankAbsolute,
            page,
            Domain: domain,
            Title: title,
            Url: href,
            Breadcrumb: breadcrumb,
            WebsiteName: websiteName,
            IsImage: isImage,
            IsVideo: isVideo,
            IsFeaturedSnippet: isFeatured,
            Description: string.IsNullOrWhiteSpace(description) ? null : description,
            PreSnippet: string.IsNullOrWhiteSpace(preSnippet) ? null : preSnippet,
            RatingJson: ratingJson,
            PriceJson: priceJson,
            Links: links,
            Highlighted: highlighted);
    }

    private static IElement ResolveOrganicScope(IElement anchor, string href)
    {
        IElement? scope = null;
        for (var node = anchor.ParentElement; node is not null; node = node.ParentElement)
        {
            if (CountDistinctResultLinks(node) == 1 && NodeContainsHref(node, href))
                scope = node;

            if (CountDistinctResultLinks(node) >= 2)
                break;
        }

        return scope ?? FindOrganicResultBlock(anchor) ?? anchor.ParentElement?.ParentElement ?? anchor;
    }

    private static int CountDistinctResultLinks(IElement node) =>
        node.QuerySelectorAll("a[jsname='UWckNb'][href]")
            .Select(a => NormalizeResultUrl(a.GetAttribute("href")))
            .Where(u => !string.IsNullOrWhiteSpace(u) && !ShouldSkipResultUrl(u!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    private static bool NodeContainsHref(IElement node, string href) =>
        node.QuerySelectorAll("a[jsname='UWckNb'][href]")
            .Any(a => string.Equals(NormalizeResultUrl(a.GetAttribute("href")), href, StringComparison.OrdinalIgnoreCase));

    private static IElement? FindTightResultScope(IElement anchor, string href)
    {
        for (var node = anchor.ParentElement; node is not null; node = node.ParentElement)
        {
            var resultLinks = node.QuerySelectorAll("a[jsname='UWckNb'][href]")
                .Select(a => NormalizeResultUrl(a.GetAttribute("href")))
                .Where(u => !string.IsNullOrWhiteSpace(u) && !ShouldSkipResultUrl(u!))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (resultLinks.Count == 1
                && string.Equals(resultLinks[0], href, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }
        }

        return null;
    }

    private static string? FindWebsiteName(IElement scope)
    {
        foreach (var el in scope.QuerySelectorAll(".VuuXrf, .CA5RN"))
        {
            var name = el.TextContent.Trim();
            if (string.IsNullOrWhiteSpace(name) || IsDateLikeSnippet(name))
                continue;
            if (name.Contains("http", StringComparison.OrdinalIgnoreCase) || name.Contains('›'))
                continue;
            if (name.Length > 80)
                continue;
            return name;
        }

        return null;
    }

    private static (string? PreSnippet, string? Description) ExtractSnippetParts(IElement initialScope)
    {
        string? pre = null;
        string? desc = null;
        for (var scope = initialScope; scope is not null; scope = scope.ParentElement)
        {
            MergeSnippetCandidates(scope, ref pre, ref desc);
            if (!string.IsNullOrWhiteSpace(desc))
                return (pre, desc);

            if (scope.ParentElement is not null && CountDistinctResultLinks(scope.ParentElement) > 1)
                break;
        }

        return (pre, desc);
    }

    private static void MergeSnippetCandidates(IElement scope, ref string? pre, ref string? desc)
    {
        const string selector = ".VwiC3b, .IsZvec, .st, [data-sncf=\"1\"], .YrbPuc";
        foreach (var el in scope.QuerySelectorAll(selector))
        {
            var text = WebUtilityDecode(el.TextContent).Trim();
            if (text.Length < 3)
                continue;

            if (IsDateLikeSnippet(text))
            {
                pre ??= text.TrimEnd('—', '-', ' ', '\u2014');
                continue;
            }

            if (text.Length > (desc?.Length ?? 0))
                desc = text;
        }

        if (!string.IsNullOrWhiteSpace(desc) && !string.IsNullOrWhiteSpace(pre)
            && desc.StartsWith(pre, StringComparison.OrdinalIgnoreCase))
        {
            desc = desc[pre.Length..].TrimStart('—', '-', ' ', '\u2014');
        }
    }

    private static bool IsDateLikeSnippet(string text) =>
        DateLikeSnippet.IsMatch(text)
        || (text.Length <= 24 && Regex.IsMatch(text, @"\b\d{4}\b"));

    private static List<SerpParsedLink> ParseSitelinks(IElement block)
    {
        var links = new List<SerpParsedLink>();
        var sequence = 1;
        foreach (var anchor in block.QuerySelectorAll(".HiHjCd a[href], .nsj7Df a[href], .vlGucd a[href]"))
        {
            var url = NormalizeResultUrl(anchor.GetAttribute("href"));
            var title = anchor.TextContent.Trim();
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(title))
                continue;

            links.Add(new SerpParsedLink(title, url));
            sequence++;
        }

        return links;
    }

    private static List<string> ParseHighlighted(IElement? snippetEl)
    {
        var highlighted = new List<string>();
        if (snippetEl is null)
            return highlighted;

        foreach (var node in snippetEl.QuerySelectorAll("b, em, strong"))
        {
            var text = node.TextContent.Trim();
            if (text.Length >= 3)
                highlighted.Add(text);
        }

        return highlighted;
    }

    private static string? TrySerializeRating(IElement block)
    {
        var rating = block.QuerySelector("[aria-label*='stars'], .yi40Hd, [role='img'][aria-label*='Rated']");
        if (rating is null)
            return null;

        return JsonSerializer.Serialize(new { label = rating.GetAttribute("aria-label") ?? rating.TextContent.Trim() });
    }

    private static string? TrySerializePrice(IElement block)
    {
        var price = block.QuerySelector(".e10twf, .T14wmb, [data-dtype='d3ph']");
        if (price is null)
            return null;

        return JsonSerializer.Serialize(new { text = price.TextContent.Trim() });
    }

    private static SerpParsedItem? ParseRelatedSearchBlock(
        IDocument document,
        string html,
        string? keyword,
        int page,
        ref int rankAbsolute)
    {
        var queries = new List<SerpParsedRelatedQuery>();
        var sequence = 1;

        foreach (var pasf in ExtractPasfQueries(html))
            AddRelatedQuery(queries, ref sequence, pasf, keyword, SerpRelatedQueryType.PeopleAlsoAsk);

        foreach (Match match in D3Pe6eBlock.Matches(html))
        {
            foreach (Match chip in PasfChipText.Matches(match.Groups[1].Value))
            {
                AddRelatedQuery(
                    queries,
                    ref sequence,
                    WebUtilityDecode(chip.Groups[1].Value).Trim(),
                    keyword);
            }
        }

        foreach (var element in document.QuerySelectorAll("div[jsname='d3PE6e'] div[data-hveid], [jsname='Cpkphb']"))
            AddRelatedQuery(queries, ref sequence, element.TextContent.Trim(), keyword);

        foreach (Match match in RelatedQuestionPair.Matches(html))
        {
            AddRelatedQuery(
                queries,
                ref sequence,
                WebUtilityDecode(match.Groups[1].Value).Trim(),
                keyword);
        }

        foreach (var element in document.QuerySelectorAll("[data-q]"))
        {
            AddRelatedQuery(
                queries,
                ref sequence,
                WebUtilityDecode(element.GetAttribute("data-q") ?? "").Trim(),
                keyword);
        }

        if (queries.Count == 0)
            return null;

        return new SerpParsedItem(
            SerpItemTypes.RelatedSearches,
            RankGroup: 1,
            rankAbsolute++,
            page,
            RelatedQueries: queries);
    }

    private static void AddRelatedQuery(
        List<SerpParsedRelatedQuery> queries,
        ref int sequence,
        string text,
        string? keyword,
        SerpRelatedQueryType queryType = SerpRelatedQueryType.PeopleAlsoAsk)
    {
        if (ShouldSkipRelatedQuery(text, keyword))
            return;

        text = NormalizeQueryText(text);
        if (queries.Any(q => string.Equals(NormalizeQueryText(q.QueryText), text, StringComparison.OrdinalIgnoreCase)))
            return;

        // PASF and classic PAA nodes are stored as PAA suggestions.
        queries.Add(new SerpParsedRelatedQuery(sequence++, text, queryType));
    }

    private static IEnumerable<string> ExtractPasfQueries(string html)
    {
        var pasfStart = html.IndexOf("People also search for", StringComparison.OrdinalIgnoreCase);
        if (pasfStart < 0)
            yield break;

        var slice = html.AsSpan(pasfStart, Math.Min(12_000, html.Length - pasfStart)).ToString();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in PasfChipText.Matches(slice))
        {
            var text = WebUtilityDecode(match.Groups[1].Value).Trim();
            if (text.Length >= 3 && seen.Add(text))
                yield return text;
        }

        foreach (Match match in PasfChipSpan.Matches(slice))
        {
            var text = StripHtmlTags(WebUtilityDecode(match.Groups[1].Value)).Trim();
            if (text.Length >= 3 && seen.Add(text))
                yield return text;
        }

        foreach (Match match in PaidHref.Matches(slice))
        {
            var href = WebUtilityDecode(match.Groups[1].Value);
            var queryMatch = PasfSearchQuery.Match(href);
            if (!queryMatch.Success)
                continue;

            var text = WebUtilityDecode(Uri.UnescapeDataString(queryMatch.Groups[1].Value.Replace('+', ' '))).Trim();
            if (text.Length >= 3 && seen.Add(text))
                yield return text;
        }
    }

    private static string StripHtmlTags(string value) =>
        Regex.Replace(value, "<[^>]+>", " ", RegexOptions.IgnoreCase).Trim();

    private static string? TryGetSearchKeyword(IDocument document)
    {
        var title = document.QuerySelector("title")?.TextContent?.Trim();
        if (string.IsNullOrWhiteSpace(title))
            return null;

        const string suffix = " - Google Search";
        if (title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            title = title[..^suffix.Length].Trim();

        return SerpSearchKeywordNormalizer.Normalize(title);
    }

    private static bool ShouldSkipRelatedQuery(string question, string? keyword)
    {
        if (question.Length < 5)
            return true;

        if (string.IsNullOrWhiteSpace(keyword))
            return false;

        return string.Equals(NormalizeQueryText(question), NormalizeQueryText(keyword), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeQueryText(string value) =>
        Regex.Replace(WebUtilityDecode(value).Trim(), @"\s+", " ");

    private static bool IsNonOrganicBlock(IElement block)
    {
        var className = block.ClassName ?? "";
        if (className.Contains("commercial", StringComparison.OrdinalIgnoreCase)
            || className.Contains("shopping", StringComparison.OrdinalIgnoreCase)
            || className.Contains("pla", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return block.QuerySelector("[data-text-ad], .uEierd, .cu-container") != null;
    }

    private static string? NormalizeResultUrl(string? href) => SerpResultUrlNormalizer.Normalize(href);

    public static bool ShouldSkipResultUrlPublic(string href) => ShouldSkipResultUrl(href);

    private static bool ShouldSkipResultUrl(string href)
    {
        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
            return true;

        var host = uri.Host.ToLowerInvariant();
        string[] skip = ["google.com", "googleusercontent.com", "gstatic.com", "youtube.com", "goo.gl"];
        return skip.Any(s => host == s || host.EndsWith("." + s, StringComparison.Ordinal));
    }

    private static string? TryGetDomain(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? uri.Host[4..]
            : uri.Host;
    }

    private static string WebUtilityDecode(string value) =>
        System.Net.WebUtility.HtmlDecode(value);
}

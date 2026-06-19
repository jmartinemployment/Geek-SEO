using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GeekSeoBackend.Services.SiteAnalyzer;

public static partial class PageHtmlSignalExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static IReadOnlyList<PageHeadingSignal> ExtractHeadings(string html)
    {
        var results = new List<PageHeadingSignal>();
        foreach (Match match in HeadingRegex().Matches(html))
        {
            var level = int.Parse(match.Groups["level"].Value);
            var text = WebUtility.HtmlDecode(StripTags(match.Groups["text"].Value)).Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;
            results.Add(new PageHeadingSignal(level, text));
        }

        return results;
    }

    public static IReadOnlyList<string> ExtractJsonLdBlocks(string html)
    {
        var results = new List<string>();
        foreach (Match match in JsonLdScriptRegex().Matches(html))
        {
            var body = match.Groups["body"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(body))
                results.Add(body);
        }

        return results;
    }

    public static string SerializeHeadings(IReadOnlyList<PageHeadingSignal> headings) =>
        JsonSerializer.Serialize(headings, JsonOptions);

    public static string SerializeJsonLd(IReadOnlyList<string> blocks) =>
        JsonSerializer.Serialize(blocks, JsonOptions);

    private static string StripTags(string value) =>
        TagRegex().Replace(value, " ").Replace('\n', ' ').Trim();

    [GeneratedRegex(@"<h(?<level>[1-6])[^>]*>(?<text>.*?)</h\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"<script[^>]+type=[""']application/ld\+json[""'][^>]*>(?<body>.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex JsonLdScriptRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}

public sealed record PageHeadingSignal(int Level, string Text);

using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static partial class UrlPageBusinessContextResolver
{
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex Whitespace();

    public static string Derive(PageContent page, string normalizedUrl)
    {
        var domain = ExtractDomainLabel(normalizedUrl);
        var topic = UrlPageKeywordResolver.Derive(page, normalizedUrl);
        var sentences = new List<string>();

        if (!string.IsNullOrWhiteSpace(domain))
            sentences.Add($"Source site: {domain}.");

        if (!string.IsNullOrWhiteSpace(page.MetaDescription))
        {
            sentences.Add(TrimToLength(NormalizeWhitespace(page.MetaDescription), 260));
        }
        else if (!string.IsNullOrWhiteSpace(page.FullText))
        {
            var lead = ExtractLeadSentences(page.FullText, 2, 320);
            if (!string.IsNullOrWhiteSpace(lead))
                sentences.Add(lead);
        }

        if (sentences.Count <= 1 && !string.IsNullOrWhiteSpace(topic))
            sentences.Add($"This page focuses on {topic.ToLowerInvariant()}.");

        return string.Join(" ", sentences.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
    }

    private static string ExtractDomainLabel(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "";

        var host = uri.Host;
        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            host = host[4..];

        var label = host.Split('.')[0];
        if (string.IsNullOrWhiteSpace(label))
            return host;

        return char.ToUpperInvariant(label[0]) + label[1..];
    }

    private static string ExtractLeadSentences(string fullText, int maxSentences, int maxChars)
    {
        var normalized = NormalizeWhitespace(fullText);
        if (normalized.Length == 0)
            return "";

        var sentences = normalized
            .Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length >= 20)
            .Take(maxSentences)
            .ToList();

        if (sentences.Count == 0)
            return TrimToLength(normalized, maxChars);

        return TrimToLength(string.Join(". ", sentences) + ".", maxChars);
    }

    private static string NormalizeWhitespace(string value) =>
        Whitespace().Replace(value.Trim(), " ");

    private static string TrimToLength(string value, int maxChars) =>
        value.Length <= maxChars ? value : value[..(maxChars - 1)].TrimEnd() + "…";
}

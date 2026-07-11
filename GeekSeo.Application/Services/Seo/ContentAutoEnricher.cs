using System.Text.RegularExpressions;

namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Deterministic HTML fixes applied before scoring or review — no user-facing suggestions for these.
/// </summary>
public static partial class ContentAutoEnricher
{
    public static string Enrich(string html, string keyword, out bool changed)
    {
        var result = ArticleMethodologyScaffold.StripMovementLabels(html);
        changed = !string.Equals(result, html, StringComparison.Ordinal);
        result = EnsureMetaDescription(result, keyword, out var metaChanged);
        changed |= metaChanged;
        return result;
    }

    public static string EnsureMetaDescription(string html, string keyword, out bool changed)
    {
        changed = false;
        if (HasMetaDescription(html))
            return html;

        var plain = ExtractFirstParagraphPlainText(html);
        var patched = ScoreSuggestionApplicator.TryApplyDeterministic(
            "meta_description",
            html,
            keyword,
            55,
            plain,
            []);
        if (patched is null || string.Equals(patched, html, StringComparison.Ordinal))
            return html;

        changed = true;
        return patched;
    }

    public static bool HasMetaDescription(string html) =>
        html.Contains("name=\"description\"", StringComparison.OrdinalIgnoreCase)
        || html.Contains("name='description'", StringComparison.OrdinalIgnoreCase);

    private static string ExtractFirstParagraphPlainText(string html)
    {
        var match = FirstParagraphRegex().Match(html);
        if (!match.Success)
            return string.Empty;

        return Regex.Replace(match.Groups[1].Value, "<[^>]+>", string.Empty).Trim();
    }

    [GeneratedRegex(@"<p[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex FirstParagraphRegex();
}

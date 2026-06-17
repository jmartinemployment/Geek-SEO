using System.Globalization;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static partial class UrlPageKeywordResolver
{
    [GeneratedRegex(@"[\|\-–—:]", RegexOptions.Compiled)]
    private static partial Regex TitleSeparator();

    public static string NormalizeUrl(string url)
    {
        var trimmed = url.Trim();
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "https://" + trimmed;
        return trimmed;
    }

    public static string Derive(PageContent page, string url)
    {
        var fromTitle = CleanSegment(page.MetaTitle);
        if (IsUsable(fromTitle))
            return fromTitle!;

        var h1 = page.Headings.FirstOrDefault(h => h.Level == 1)?.Text;
        if (IsUsable(h1))
            return h1!.Trim();

        var fromSlug = SlugFromUrl(url);
        if (IsUsable(fromSlug))
            return fromSlug!;

        return "";
    }

    private static string? CleanSegment(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var trimmed = raw.Trim();
        var parts = TitleSeparator().Split(trimmed);
        var primary = parts[0].Trim();
        return primary.Length >= 3 ? primary : trimmed;
    }

    private static string? SlugFromUrl(string url)
    {
        if (!Uri.TryCreate(NormalizeUrl(url), UriKind.Absolute, out var uri))
            return null;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return null;

        var last = segments[^1].Replace('-', ' ').Replace('_', ' ');
        var dot = last.LastIndexOf('.');
        if (dot > 0)
            last = last[..dot];

        if (string.IsNullOrWhiteSpace(last))
            return null;

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(last.ToLowerInvariant());
    }

    private static bool IsUsable(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length >= 3;
}

using System.Text.RegularExpressions;

namespace GeekSeo.Application.Services.Seo;

public static partial class SerpCaptureTextSanitizer
{
    private static readonly string[] JunkMarkers =
    [
        "function(",
        "(function(){",
        "window.",
        "google.rll",
        "Can't generate an AI overview",
        "not available for this search",
        "Try again later",
        "AI Overview (function",
    ];

    public static bool IsUsable(string? text)
    {
        var sanitized = Sanitize(text);
        return !string.IsNullOrWhiteSpace(sanitized);
    }

    public static string? Sanitize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = WhitespaceRegex().Replace(text.Trim(), " ");
        if (trimmed.Length < 12)
            return null;

        foreach (var marker in JunkMarkers)
        {
            if (trimmed.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return null;
        }

        if (ScriptTagRegex().IsMatch(trimmed))
            return null;

        return trimmed.Length > 500 ? trimmed[..500].TrimEnd() : trimmed;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"<script\b", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();
}

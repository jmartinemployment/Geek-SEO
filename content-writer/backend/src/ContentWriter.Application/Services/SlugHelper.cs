using System.Text.RegularExpressions;

namespace ContentWriter.Application.Services;

public static class SlugHelper
{
    public static string Slugify(string title)
    {
        var lower = title.Trim().ToLowerInvariant();
        var withoutDiacritics = Regex.Replace(lower, @"[^a-z0-9\s-]", "");
        var collapsed = Regex.Replace(withoutDiacritics, @"[\s-]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(collapsed) ? Guid.NewGuid().ToString("N")[..8] : collapsed;
    }
}

using System.Text;
using System.Text.RegularExpressions;

namespace GeekSeo.Application.Services.Seo;

public static partial class ContentPublishSlug
{
    private static readonly Regex ValidSlugPattern = ValidSlugRegex();

    public static bool IsValid(string? slug) =>
        !string.IsNullOrWhiteSpace(slug) && ValidSlugPattern.IsMatch(slug.Trim());

    public static string NormalizeFromPhrase(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            return string.Empty;

        var builder = new StringBuilder(phrase.Length);
        var previousWasDash = false;

        foreach (var ch in phrase.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousWasDash = false;
                continue;
            }

            if (!previousWasDash && builder.Length > 0)
            {
                builder.Append('-');
                previousWasDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    public static string AllocateUnique(string baseSlug, IEnumerable<string> existingSlugs)
    {
        var normalized = baseSlug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        var taken = new HashSet<string>(
            existingSlugs
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLowerInvariant()),
            StringComparer.Ordinal);

        if (!taken.Contains(normalized))
            return normalized;

        for (var suffix = 2; suffix < 1000; suffix++)
        {
            var candidate = $"{normalized}-{suffix}";
            if (!taken.Contains(candidate))
                return candidate;
        }

        return $"{normalized}-{Guid.NewGuid():N}"[..Math.Min(normalized.Length + 33, 200)];
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidSlugRegex();
}

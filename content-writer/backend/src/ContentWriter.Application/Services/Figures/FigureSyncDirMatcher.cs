using System.Text.RegularExpressions;

namespace ContentWriter.Application.Services.Figures;

public static partial class FigureSyncDirMatcher
{
    [GeneratedRegex(@"^h2-.+\.webp$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SyncFileNameRegex();

    public static bool IsSyncCandidateFile(string fileName) =>
        SyncFileNameRegex().IsMatch(fileName);

    /// <summary>
    /// Resolves a sync-dir filename to a heading slug by matching known slugs (longest first).
    /// Returns null when no slug matches.
    /// </summary>
    public static string? ResolveHeadingSlug(string fileName, IReadOnlyCollection<string> knownHeadingSlugs)
    {
        if (!IsSyncCandidateFile(fileName))
        {
            return null;
        }

        var ordered = knownHeadingSlugs
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(s => s.Length)
            .ToList();

        foreach (var slug in ordered)
        {
            if (fileName.Equals($"h2-{slug}.webp", StringComparison.OrdinalIgnoreCase))
            {
                return slug;
            }

            if (fileName.StartsWith($"h2-", StringComparison.OrdinalIgnoreCase)
                && fileName.EndsWith($"-{slug}.webp", StringComparison.OrdinalIgnoreCase))
            {
                return slug;
            }
        }

        return null;
    }
}

using System.Security.Cryptography;
using System.Text;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services;

public static class NicheScanFingerprint
{
    public sealed record ScanFingerprintResult(string Fingerprint, decimal ChangeScore);

    public static ScanFingerprintResult Compute(
        string domain,
        string sulVersion,
        SchemaOrgData schema,
        SitemapData sitemap,
        NavMenuData nav,
        IReadOnlyList<string>? priorSitemapUrls = null)
    {
        var coreParts = new[]
        {
            domain.Trim().ToLowerInvariant(),
            sulVersion,
            JoinSorted(schema.KnowsAboutTopics),
            JoinSorted(nav.Pillars.Select(p => p.Name)),
            JoinSorted(schema.AreaServed),
        };
        var fingerprint = Hash(string.Join("|", coreParts));

        var currentUrls = sitemap.SampleUrls
            .Concat(sitemap.Pillars.Select(p => p.PageUrl).Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var changeScore = priorSitemapUrls is { Count: > 0 }
            ? Jaccard(currentUrls, priorSitemapUrls.ToHashSet(StringComparer.OrdinalIgnoreCase))
            : 1m;

        return new ScanFingerprintResult(fingerprint, changeScore);
    }

    private static string JoinSorted(IEnumerable<string> values) =>
        string.Join(",", values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).OrderBy(v => v, StringComparer.OrdinalIgnoreCase));

    private static decimal Jaccard(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 && b.Count == 0) return 1m;
        var intersection = a.Intersect(b, StringComparer.OrdinalIgnoreCase).Count();
        var union = a.Union(b, StringComparer.OrdinalIgnoreCase).Count();
        return union == 0 ? 1m : Math.Round((decimal)intersection / union, 4);
    }

    private static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}

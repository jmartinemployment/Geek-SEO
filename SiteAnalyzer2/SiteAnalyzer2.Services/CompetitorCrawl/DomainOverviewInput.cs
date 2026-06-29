using SiteAnalyzer2.Services.Integrations;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.CompetitorCrawl;

public enum DomainOverviewScope
{
    DomainRoot,
    Url,
}

public sealed record DomainOverviewInput(
    string RequestedInput,
    string Domain,
    string SiteRootUrl,
    string AnalyzedUrl,
    DomainOverviewScope Scope)
{
    public static DomainOverviewInput? Parse(string? domainOrUrl)
    {
        if (string.IsNullOrWhiteSpace(domainOrUrl))
            return null;

        var requested = domainOrUrl.Trim();
        var withScheme = requested.Contains("://", StringComparison.Ordinal)
            ? requested
            : $"https://{requested.TrimStart('.')}";

        if (!Uri.TryCreate(withScheme, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
            return null;

        var domain = DomainHelper.GetRegistrableDomain(uri.Host);
        if (string.IsNullOrWhiteSpace(domain))
            return null;

        var siteRoot = TargetSiteUrlNormalizer.Normalize(withScheme);
        if (string.IsNullOrEmpty(siteRoot))
            return null;

        var path = uri.AbsolutePath.Trim();
        var hasPath = path.Length > 0 && path != "/";
        var scope = hasPath ? DomainOverviewScope.Url : DomainOverviewScope.DomainRoot;

        var analyzed = hasPath
            ? BuildPathUrl(uri)
            : siteRoot;

        return new DomainOverviewInput(requested, domain, siteRoot, analyzed, scope);
    }

    private static string BuildPathUrl(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Scheme = Uri.UriSchemeHttps,
            Fragment = string.Empty,
        };

        var path = builder.Path.TrimEnd('/');
        if (path.Length == 0)
            path = "/";

        builder.Path = path;
        return builder.Uri.ToString();
    }
}

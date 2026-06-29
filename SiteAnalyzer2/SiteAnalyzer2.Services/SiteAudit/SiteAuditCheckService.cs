using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Services.SiteAudit;

/// <summary>
/// Runs crawl-based audit checks (Crawlability, HTTPS, Markups).
/// Pure logic — no database; persistence happens in SiteAuditJobService (slice 2a-3).
/// </summary>
public sealed class SiteAuditCheckService
{
    public const int DefaultMaxCrawlDepth = 4;

    public IReadOnlyList<AuditIssue> RunAllChecks(SiteAuditCheckInput input) =>
        EvaluateCrawlability(input)
            .Concat(EvaluateHttps(input))
            .Concat(EvaluateMarkups(input))
            .ToList();

    public IReadOnlyList<AuditIssue> EvaluateCrawlability(SiteAuditCheckInput input)
    {
        var issues = new List<AuditIssue>();
        var pages = input.Pages;

        var broken = pages.Where(p => p.HttpStatus >= 400).Select(p => p.Url).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (broken.Count > 0)
        {
            issues.Add(new AuditIssue(
                AuditIssueCode.BrokenPage,
                SiteAuditCategory.Crawlability,
                AuditSeverity.Error,
                "Broken pages (4xx/5xx)",
                "Pages returned an error status during crawl.",
                broken,
                "Fix server errors and broken links. Ensure important URLs return HTTP 200."));
        }

        var incoming = BuildIncomingInternalLinkCounts(input);
        var orphans = pages
            .Where(p => p.HttpStatus < 400)
            .Where(p => !IsHomepageUrl(p.Url, input.SiteUrl))
            .Where(p => !incoming.ContainsKey(NormalizeUrl(p.Url)))
            .Select(p => p.Url)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (orphans.Count > 0)
        {
            issues.Add(new AuditIssue(
                AuditIssueCode.OrphanPage,
                SiteAuditCategory.Crawlability,
                AuditSeverity.Warning,
                "Orphan pages",
                "Pages with no incoming internal links from other crawled pages.",
                orphans,
                "Add internal links from hub pages or navigation so crawlers and users can reach these URLs."));
        }

        var deep = pages
            .Where(p => p.DepthFromHomepage is int depth && depth > DefaultMaxCrawlDepth)
            .Select(p => p.Url)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (deep.Count > 0)
        {
            issues.Add(new AuditIssue(
                AuditIssueCode.ExcessiveCrawlDepth,
                SiteAuditCategory.Crawlability,
                AuditSeverity.Notice,
                "Deep crawl depth",
                $"Pages more than {DefaultMaxCrawlDepth} clicks from the homepage.",
                deep,
                "Flatten site structure or add internal links from higher-authority pages."));
        }

        return issues;
    }

    public IReadOnlyList<AuditIssue> EvaluateHttps(SiteAuditCheckInput input)
    {
        var insecure = input.Pages
            .Where(p => p.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Url)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (insecure.Count == 0)
            return [];

        return
        [
            new AuditIssue(
                AuditIssueCode.HttpUrl,
                SiteAuditCategory.Https,
                AuditSeverity.Error,
                "HTTP URLs crawled",
                "Site audit found pages served over HTTP instead of HTTPS.",
                insecure,
                "Redirect HTTP to HTTPS and update internal links to use https:// URLs."),
        ];
    }

    public IReadOnlyList<AuditIssue> EvaluateMarkups(SiteAuditCheckInput input)
    {
        var issues = new List<AuditIssue>();

        var missingTitle = input.Pages
            .Where(p => !HasMeta(p, "title") && !HasMeta(p, "og:title"))
            .Select(p => p.Url)
            .ToList();
        if (missingTitle.Count > 0)
        {
            issues.Add(new AuditIssue(
                AuditIssueCode.MissingTitleTag,
                SiteAuditCategory.Markups,
                AuditSeverity.Error,
                "Missing title tag",
                "Pages without a title or og:title meta tag.",
                missingTitle,
                "Add a unique, descriptive <title> for each indexable page."));
        }

        var missingDescription = input.Pages
            .Where(p => !HasMeta(p, "description") && !HasMeta(p, "og:description"))
            .Select(p => p.Url)
            .ToList();
        if (missingDescription.Count > 0)
        {
            issues.Add(new AuditIssue(
                AuditIssueCode.MissingMetaDescription,
                SiteAuditCategory.Markups,
                AuditSeverity.Warning,
                "Missing meta description",
                "Pages without meta description or og:description.",
                missingDescription,
                "Add a concise meta description that summarizes the page for search snippets."));
        }

        var missingH1 = input.Pages
            .Where(p => !p.HeadingLevels.Any(l => string.Equals(l, "h1", StringComparison.OrdinalIgnoreCase)))
            .Select(p => p.Url)
            .ToList();
        if (missingH1.Count > 0)
        {
            issues.Add(new AuditIssue(
                AuditIssueCode.MissingH1,
                SiteAuditCategory.Markups,
                AuditSeverity.Warning,
                "Missing H1",
                "Pages without an H1 heading.",
                missingH1,
                "Add one clear H1 per page that matches the primary topic."));
        }

        var missingJsonLd = input.Pages
            .Where(p => p.JsonLdTypes.Count == 0)
            .Select(p => p.Url)
            .ToList();
        if (missingJsonLd.Count > 0)
        {
            issues.Add(new AuditIssue(
                AuditIssueCode.MissingJsonLd,
                SiteAuditCategory.Markups,
                AuditSeverity.Notice,
                "No JSON-LD",
                "Pages without structured data blocks.",
                missingJsonLd,
                "Add relevant JSON-LD (Organization, WebPage, FAQ, etc.) where it helps search and AI systems understand the page."));
        }

        return issues;
    }

    private static Dictionary<string, int> BuildIncomingInternalLinkCounts(SiteAuditCheckInput input)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var link in input.InternalLinks.Where(l => l.IsInternal))
        {
            var target = NormalizeUrl(link.TargetUrl);
            counts[target] = counts.GetValueOrDefault(target) + 1;
        }

        return counts;
    }

    private static bool IsHomepageUrl(string pageUrl, string siteUrl)
    {
        static string NormalizeHome(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return url.TrimEnd('/');
            return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        }

        return string.Equals(NormalizeHome(pageUrl), NormalizeHome(siteUrl), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.TrimEnd('/');
        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    private static bool HasMeta(SiteAuditPageSnapshot page, string key) =>
        page.MetaTags.Any(m =>
            string.Equals(m.NameOrProperty, key, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(m.Content));
}

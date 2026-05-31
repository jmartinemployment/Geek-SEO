using System.Text.Json;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class SiteAuditPageAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public const int MaxPagesPerRun = 25;

    public static (int Score, string IssuesJson) Analyze(PageContent page)
    {
        var issues = new List<SiteAuditIssue>();

        if (page.HttpStatusCode >= 400)
        {
            issues.Add(new SiteAuditIssue(
                "http_error",
                "critical",
                $"Page returned HTTP {page.HttpStatusCode}.",
                "status"));
        }

        if (string.IsNullOrWhiteSpace(page.MetaTitle))
        {
            issues.Add(new SiteAuditIssue(
                "missing_title",
                "critical",
                "Missing or empty title tag.",
                "title"));
        }
        else
        {
            var titleLen = page.MetaTitle.Length;
            if (titleLen < 30)
            {
                issues.Add(new SiteAuditIssue(
                    "title_too_short",
                    "warning",
                    $"Title is short ({titleLen} characters). Aim for 30–60.",
                    "title"));
            }
            else if (titleLen > 60)
            {
                issues.Add(new SiteAuditIssue(
                    "title_too_long",
                    "warning",
                    $"Title may truncate in search ({titleLen} characters). Aim for 30–60.",
                    "title"));
            }
        }

        if (string.IsNullOrWhiteSpace(page.MetaDescription))
        {
            issues.Add(new SiteAuditIssue(
                "missing_meta_description",
                "warning",
                "Missing meta description.",
                "metaDescription"));
        }
        else if (page.MetaDescription.Length > 160)
        {
            issues.Add(new SiteAuditIssue(
                "meta_description_too_long",
                "warning",
                $"Meta description may truncate ({page.MetaDescription.Length} characters). Aim for 120–160.",
                "metaDescription"));
        }

        var h1Count = page.Headings.Count(h => h.Level == 1);
        if (h1Count == 0)
        {
            issues.Add(new SiteAuditIssue(
                "missing_h1",
                "critical",
                "No H1 heading found.",
                "h1"));
        }
        else if (h1Count > 1)
        {
            issues.Add(new SiteAuditIssue(
                "multiple_h1",
                "warning",
                $"Multiple H1 headings ({h1Count}). Use a single primary H1.",
                "h1"));
        }

        if (page.WordCount < 300)
        {
            issues.Add(new SiteAuditIssue(
                "thin_content",
                "warning",
                $"Low word count ({page.WordCount}). Consider expanding substantive content.",
                "wordCount"));
        }

        if (!page.HasStructuredData)
        {
            issues.Add(new SiteAuditIssue(
                "no_structured_data",
                "info",
                "No JSON-LD structured data detected.",
                "structuredData"));
        }

        var score = Math.Clamp(100 - ScoreDeduction(issues), 0, 100);
        return (score, JsonSerializer.Serialize(issues, JsonOptions));
    }

    private static int ScoreDeduction(IReadOnlyList<SiteAuditIssue> issues)
    {
        var total = 0;
        foreach (var issue in issues)
        {
            total += issue.Severity switch
            {
                "critical" => 25,
                "warning" => 10,
                _ => 3,
            };
        }

        return total;
    }
}

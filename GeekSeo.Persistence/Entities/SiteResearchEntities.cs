using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GeekSeo.Persistence.Entities;

public sealed class SeoSiteResearch
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public required string SiteUrl { get; set; }
    /// <summary>JSON array of discovered sitemap URLs (step 1).</summary>
    public string DiscoveredUrlsJson { get; set; } = "[]";
    public string BusinessSummary { get; set; } = string.Empty;
    /// <summary>JSON array of internal link edges { from, to, anchor }.</summary>
    public string InternalLinkMapJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    [ValidateNever]
    public SeoProject? Project { get; set; }
    [ValidateNever]
    public ICollection<SeoSiteResearchPage> Pages { get; set; } = [];
    [ValidateNever]
    public ICollection<SeoSiteAnalyzerStepRun> StepRuns { get; set; } = [];
}

public sealed class SeoSiteResearchPage
{
    public Guid Id { get; set; }
    public Guid SiteResearchId { get; set; }
    public required string Url { get; set; }
    public string Html { get; set; } = string.Empty;
    /// <summary>PostgreSQL jsonb — array of { level, text }.</summary>
    public string HeadingsJson { get; set; } = "[]";
    /// <summary>PostgreSQL jsonb — array of JSON-LD block strings.</summary>
    public string JsonLdJson { get; set; } = "[]";
    public bool ExtractSuccess { get; set; }
    public string? ExtractError { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    [ValidateNever]
    public SeoSiteResearch? SiteResearch { get; set; }
}

public sealed class SeoSiteAnalyzerStepRun
{
    public Guid Id { get; set; }
    public Guid? SiteResearchId { get; set; }
    public Guid? UrlResearchId { get; set; }
    public int StepNumber { get; set; }
    /// <summary>pending | running | green | red</summary>
    public string Status { get; set; } = "pending";
    public string Message { get; set; } = string.Empty;
    public string Log { get; set; } = string.Empty;
    public string? CountsJson { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    [ValidateNever]
    public SeoSiteResearch? SiteResearch { get; set; }
    [ValidateNever]
    public SeoUrlResearch? UrlResearch { get; set; }
}

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GeekSeo.Persistence.Entities;

public sealed class SeoUrlResearch
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public required string SourceUrl { get; set; }
    public string DerivedKeyword { get; set; } = string.Empty;
    public string SearchLocation { get; set; } = "United States";
    public string BusinessContext { get; set; } = string.Empty;
    public string GbpSource { get; set; } = "none";
    /// <summary>queued | running | completed | failed</summary>
    public string Status { get; set; } = "queued";
    public string? ErrorMessage { get; set; }
    /// <summary>full | partial | weak</summary>
    public string? DataQuality { get; set; }
    public string? DataQualityNotes { get; set; }
    public string IntentPrimary { get; set; } = string.Empty;
    public string IntentJustification { get; set; } = string.Empty;
    public string PafType { get; set; } = string.Empty;
    public string PafFormat { get; set; } = string.Empty;
    public string PafText { get; set; } = string.Empty;
    public string PafSourceUrl { get; set; } = string.Empty;
    public string PafBeatStrategy { get; set; } = string.Empty;
    public string DirectAnswerInstruction { get; set; } = string.Empty;
    public bool MustBeatPaf { get; set; }
    public int MedianWordCountTop5 { get; set; }
    public int MedianTitleLengthTop10 { get; set; }
    public int MedianH2CountTop5 { get; set; }
    public string DominantContentFormat { get; set; } = string.Empty;
    public DateTimeOffset? ResearchedAt { get; set; }
    public Guid? SupersedesResearchId { get; set; }
    /// <summary>FK to completed site index (<c>seo_site_research</c>).</summary>
    public Guid? SiteResearchId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    [ValidateNever]
    public SeoProject? Project { get; set; }
    [ValidateNever]
    public ICollection<SeoUrlResearchOrganic> OrganicResults { get; set; } = [];
    [ValidateNever]
    public ICollection<SeoUrlResearchPaa> PeopleAlsoAsk { get; set; } = [];
    [ValidateNever]
    public ICollection<SeoUrlResearchPasf> RelatedSearches { get; set; } = [];
    [ValidateNever]
    public ICollection<SeoUrlResearchCompetitor> Competitors { get; set; } = [];
    [ValidateNever]
    public ICollection<SeoUrlResearchSourceHeading> SourceHeadings { get; set; } = [];
    [ValidateNever]
    public ICollection<SeoUrlResearchTerm> RecommendedTerms { get; set; } = [];
    [ValidateNever]
    public ICollection<SeoUrlResearchClosingFaq> ClosingFaqs { get; set; } = [];
    [ValidateNever]
    public ICollection<SeoUrlResearchSectionHint> SectionHints { get; set; } = [];
}

public sealed class SeoUrlResearchOrganic
{
    public Guid Id { get; set; }
    public Guid UrlResearchId { get; set; }
    public int Position { get; set; }
    public required string Url { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    [ValidateNever]
    public SeoUrlResearch? UrlResearch { get; set; }
}

public sealed class SeoUrlResearchPaa
{
    public Guid Id { get; set; }
    public Guid UrlResearchId { get; set; }
    public required string Question { get; set; }
    public string SerpAnswerPreview { get; set; } = string.Empty;
    public int Depth { get; set; } = 1;
    public int DisplayOrder { get; set; }

    [ValidateNever]
    public SeoUrlResearch? UrlResearch { get; set; }
}

public sealed class SeoUrlResearchPasf
{
    public Guid Id { get; set; }
    public Guid UrlResearchId { get; set; }
    public required string SearchText { get; set; }
    public int DisplayOrder { get; set; }

    [ValidateNever]
    public SeoUrlResearch? UrlResearch { get; set; }
}

public sealed class SeoUrlResearchCompetitor
{
    public Guid Id { get; set; }
    public Guid UrlResearchId { get; set; }
    public required string Url { get; set; }
    public int Position { get; set; }
    public string H1 { get; set; } = string.Empty;
    public int EstimatedWordCount { get; set; }

    [ValidateNever]
    public SeoUrlResearch? UrlResearch { get; set; }
    [ValidateNever]
    public ICollection<SeoUrlResearchCompetitorHeading> Headings { get; set; } = [];
}

public sealed class SeoUrlResearchCompetitorHeading
{
    public Guid Id { get; set; }
    public Guid CompetitorId { get; set; }
    public int Level { get; set; }
    public required string Text { get; set; }
    public int DisplayOrder { get; set; }

    [ValidateNever]
    public SeoUrlResearchCompetitor? Competitor { get; set; }
}

public sealed class SeoUrlResearchSourceHeading
{
    public Guid Id { get; set; }
    public Guid UrlResearchId { get; set; }
    public int Level { get; set; }
    public required string Text { get; set; }
    public int DisplayOrder { get; set; }

    [ValidateNever]
    public SeoUrlResearch? UrlResearch { get; set; }
}

public sealed class SeoUrlResearchTerm
{
    public Guid Id { get; set; }
    public Guid UrlResearchId { get; set; }
    public required string Term { get; set; }
    public int DisplayOrder { get; set; }

    [ValidateNever]
    public SeoUrlResearch? UrlResearch { get; set; }
}

public sealed class SeoUrlResearchClosingFaq
{
    public Guid Id { get; set; }
    public Guid UrlResearchId { get; set; }
    public required string Question { get; set; }
    /// <summary>paa | pasf | suggested</summary>
    public required string Source { get; set; }
    public int DisplayOrder { get; set; }

    [ValidateNever]
    public SeoUrlResearch? UrlResearch { get; set; }
}

public sealed class SeoUrlResearchSectionHint
{
    public Guid Id { get; set; }
    public Guid UrlResearchId { get; set; }
    public int DisplayOrder { get; set; }
    public int Movement { get; set; }
    public string Label { get; set; } = string.Empty;
    public string SuggestedH2 { get; set; } = string.Empty;
    public string[] SubtopicsFromSerp { get; set; } = [];

    [ValidateNever]
    public SeoUrlResearch? UrlResearch { get; set; }
}

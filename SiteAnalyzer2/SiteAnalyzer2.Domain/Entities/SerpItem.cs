using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Domain.Entities;

/// <summary>
/// One element from the DataForSEO Live Advanced <c>items[]</c> array for a SERP capture.
/// </summary>
public class SerpItem
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    /// <summary>null or <c>keyword</c> = primary SERP; supplemental: edu, gov, local, wiki.</summary>
    public string? ResearchLane { get; set; }

    public string Type { get; set; } = SerpItemTypes.Organic;
    public int RankGroup { get; set; }
    public int RankAbsolute { get; set; }
    public int Page { get; set; } = 1;
    public string Position { get; set; } = "left";
    public string? Xpath { get; set; }
    public string? RectangleJson { get; set; }

    public string? Domain { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? CacheUrl { get; set; }
    public string? RelatedSearchUrl { get; set; }
    public string? Breadcrumb { get; set; }
    public string? WebsiteName { get; set; }
    public bool IsImage { get; set; }
    public bool IsVideo { get; set; }
    public bool IsFeaturedSnippet { get; set; }
    public bool IsMalicious { get; set; }
    public bool IsWebStory { get; set; }
    public string? Description { get; set; }
    public string? PreSnippet { get; set; }
    public string? ExtendedSnippet { get; set; }
    public string? ImagesJson { get; set; }
    public bool AmpVersion { get; set; }
    public string? RatingJson { get; set; }
    public string? PriceJson { get; set; }
    public string? FaqJson { get; set; }
    public string? ExtendedPeopleAlsoSearchJson { get; set; }
    public string? AboutThisResultJson { get; set; }
    public string? RelatedResultJson { get; set; }
    public DateTime? Timestamp { get; set; }

    public bool? AiOverviewAvailable { get; set; }
    public string? AiOverviewMarkdown { get; set; }
    public string? AiOverviewStatusMessage { get; set; }

    public bool Ads { get; set; }
    public bool Filtered { get; set; }
    public FilterStatus? FilterStatus { get; set; }
    public IncludeReason? IncludeReason { get; set; }
    public string? ExcludeReason { get; set; }

    public AnalysisRun Run { get; set; } = null!;
    public ICollection<SerpItemLink> Links { get; set; } = [];
    public ICollection<SerpItemHighlighted> HighlightedPhrases { get; set; } = [];
    public ICollection<SerpRelatedQuery> RelatedQueries { get; set; } = [];
}

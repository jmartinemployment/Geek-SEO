using SiteAnalyzer2.Domain.Entities;

namespace SiteAnalyzer2.Repositories;

public sealed class SiteProfileAssemblySource
{
    public required SiteProfile SiteProfile { get; init; }
    public required AnalysisRun Run { get; init; }
    public IReadOnlyList<TargetPageSnapshot> TargetPages { get; init; } = [];
    public IReadOnlyList<SerpItem> SerpItems { get; init; } = [];
    public IReadOnlyList<Finding> GapFindings { get; init; } = [];
    public IReadOnlyList<string> SiteKeywords { get; init; } = [];
    public IReadOnlyList<string> CompetitorHeadingTexts { get; init; } = [];
}

public sealed class TargetPageInternalLink
{
    public string AbsoluteUrl { get; init; } = string.Empty;
    public string? AnchorText { get; init; }
}

public sealed class TargetPageSnapshot
{
    public required Page Page { get; init; }
    public IReadOnlyList<PageHeading> Headings { get; init; } = [];
    public IReadOnlyList<PageMetaTag> MetaTags { get; init; } = [];
    public IReadOnlyList<PageJsonLd> JsonLdBlocks { get; init; } = [];
    public IReadOnlyList<TargetPageInternalLink> InternalLinks { get; init; } = [];
}

public sealed class SiteProfileAssemblyWrite
{
    public string? BusinessType { get; init; }
    public string? BusinessDescription { get; init; }
    public string? BusinessSummary { get; init; }
    public string? ServiceAreaDescription { get; init; }
    public IReadOnlyList<string> GeoAnchorNodes { get; init; } = [];
    public string? PrimaryNiche { get; init; }
    public string? NicheDescription { get; init; }
    public IReadOnlyList<string> NicheTags { get; init; } = [];
    public IReadOnlyList<string> CompetitorDomains { get; init; } = [];
    public IReadOnlyList<string> AuthorityPageUrls { get; init; } = [];
    public IReadOnlyList<string> WritingRecommendations { get; init; } = [];
}

public sealed class RunWritingFocusWrite
{
    public string? MatchedPillarTopic { get; init; }
    public string? MatchedPillarIntent { get; init; }
    public string? MatchedPillarAngle { get; init; }
    public IReadOnlyList<string> GapTopics { get; init; } = [];
    public string? WritingInstructions { get; init; }
}

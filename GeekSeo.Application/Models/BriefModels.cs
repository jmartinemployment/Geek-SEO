namespace GeekSeo.Application.Models.Seo;

public sealed record ContentBrief
{
    public required string Keyword { get; init; }
    public required string Location { get; init; }
    public int TargetWordCount { get; init; }
    public int AvgTitleLength { get; init; }
    public IReadOnlyList<string> RecommendedTerms { get; init; } = [];
    public IReadOnlyList<string> SuggestedHeadings { get; init; } = [];
    public IReadOnlyList<BriefCompetitorSummary> TopCompetitors { get; init; } = [];
    public IReadOnlyList<string> CompetitorDomains { get; init; } = [];
    public IReadOnlyList<string> CompetitorHeadingHighlights { get; init; } = [];
    public IReadOnlyList<string> CompetitorSchemaTypes { get; init; } = [];
    public IReadOnlyList<string> PeopleAlsoAsk { get; init; } = [];
    public WritingMethodologySpec Methodology { get; init; } = WritingMethodologySpec.FourPhase;
    public IReadOnlyList<DirectAnswerBlockSpec> DirectAnswerBlocks { get; init; } = [];
    public IReadOnlyList<string> TechnicalEvidenceRequirements { get; init; } = [];
    public IReadOnlyList<string> GeoAnchorNodes { get; init; } = [];
    public SchemaBlueprint SchemaBlueprint { get; init; } = new();
    public IReadOnlyList<string> ReviewChecklist { get; init; } = [];
    public NicheContextSpec NicheContext { get; init; } = new();
    public SerpIntelligenceSnapshot SerpIntelligence { get; init; } = new();
    public string? AuthorOrganizationName { get; init; }
    public string? AuthorOrganizationUrl { get; init; }
    public string BenchmarkQuality { get; init; } = "good";
}

public sealed record WritingMethodologySpec(
    string Name,
    IReadOnlyList<string> Phases)
{
    public static readonly WritingMethodologySpec FourPhase = new(
        "Four Phase Methodology",
        [
            "Business Objectives",
            "Data Quality Assessment",
            "Tech Selection",
            "Pilot Implementation Strategy",
        ]);
}

public sealed record DirectAnswerBlockSpec(
    string Label,
    string Instruction);

public sealed record SchemaBlueprint
{
    public string PrimaryType { get; init; } = "TechArticle";
    public IReadOnlyList<string> AdditionalTypes { get; init; } = [];
    public IReadOnlyList<string> SoftwareEntities { get; init; } = [];
    public IReadOnlyList<string> AboutEntities { get; init; } = [];
}

public sealed record NicheContextSpec
{
    public string? PrimaryNiche { get; init; }
    public string? MatchedPillar { get; init; }
    public IReadOnlyList<string> GapTopics { get; init; } = [];
}

public sealed record SerpIntelligenceSnapshot
{
    public IReadOnlyList<string> PeopleAlsoAsk { get; init; } = [];
    public IReadOnlyList<string> RelatedSearches { get; init; } = [];
    public IReadOnlyList<string> FeatureFlags { get; init; } = [];
    public string? FeaturedSnippet { get; init; }
}

public sealed record BriefCompetitorSummary
{
    public required int Position { get; init; }
    public required string Url { get; init; }
    public string? Title { get; init; }
    public int WordCount { get; init; }
}

public sealed record GenerateBriefRequest
{
    public required Guid ProjectId { get; init; }
    public required string Keyword { get; init; }
    public string Location { get; init; } = "United States";
    public int CompetitorCount { get; init; } = 10;
}

public sealed record WritingOutlineRequest
{
    public required string Keyword { get; init; }
    public required ContentBrief Brief { get; init; }
    public string? Title { get; init; }
}

public sealed record WritingDraftRequest
{
    public required string Keyword { get; init; }
    public required string Outline { get; init; }
    public required ContentBrief Brief { get; init; }
    public int TargetWordCount { get; init; }
    public string? Title { get; init; }
}

public sealed record HumanizeRequest
{
    public required Guid DocumentId { get; init; }
    public required string ContentHtml { get; init; }
}

public sealed record DetectAiRequest
{
    public required Guid DocumentId { get; init; }
    public required string ContentHtml { get; init; }
}

public sealed record AiDetectionResult
{
    public required double AiProbability { get; init; }
    public string Summary { get; init; } = string.Empty;
}

public sealed record WritingTextResult
{
    public required string Content { get; init; }
}

public sealed record RenderedArticleResult
{
    public required string BodyHtml { get; init; }
    public required string RenderedHtml { get; init; }
    public IReadOnlyList<string> SchemaScripts { get; init; } = [];
    public IReadOnlyList<string> SchemaTypes { get; init; } = [];
}

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
    /// <summary>Exactly five topic FAQs for the closing section and FAQPage schema.</summary>
    public IReadOnlyList<string> ClosingFaqQuestions { get; init; } = [];
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

public sealed record MethodologyPhaseDefinition(
    string Id,
    string Label,
    string Intent,
    IReadOnlyList<string> HeadingFamilies);

public sealed record WritingMethodologySpec
{
    public required string Name { get; init; }
    public required IReadOnlyList<MethodologyPhaseDefinition> PhaseDefinitions { get; init; }

    public IReadOnlyList<string> Phases =>
        PhaseDefinitions.Select(phase => phase.Label).ToArray();

    private static readonly IReadOnlyList<MethodologyPhaseDefinition> CorePhases =
    [
        new MethodologyPhaseDefinition(
            "business-objectives",
            "Business Objectives",
            "Define why this initiative matters now: target outcomes, stakeholders, success metrics, and the business case or ROI for acting on this topic.",
            ["goals", "business case", "outcomes", "priorities", "why now", "ROI", "success metrics", "stakeholders"]),
        new MethodologyPhaseDefinition(
            "data-quality-assessment",
            "Data Quality Assessment",
            "Evaluate data readiness for AI and automation: source quality, gaps, cleanup work, governance risks, and what must be true before tools can run reliably.",
            ["data health", "readiness", "data gaps", "migration prep", "garbage in", "audit", "data cleanup", "governance"]),
        new MethodologyPhaseDefinition(
            "ai-tech-selection",
            "Choose the Right AI Technologies",
            "Compare AI models, platforms, agents, and integrations for this use case — including build-vs-buy, vendor fit, security, and how each option maps to the business objectives.",
            ["AI tools", "model selection", "LLM comparison", "platform fit", "integrations", "vendor evaluation", "build vs buy", "agent architecture"]),
        new MethodologyPhaseDefinition(
            "implementation-strategy",
            "Implementation Strategy",
            "Lay out a practical rollout: pilot scope, timeline, milestones, proof of value, team adoption, and how to measure success in the first implementation phase.",
            ["implementation plan", "pilot scope", "phased rollout", "first 30 days", "proof of value", "change management", "milestones", "rollout timeline"]),
    ];

    public static WritingMethodologySpec FourPhase { get; } = new()
    {
        Name = "Four Phase Methodology",
        PhaseDefinitions = CorePhases,
    };

    public static WritingMethodologySpec FivePhase { get; } = new()
    {
        Name = "Five Phase Methodology",
        PhaseDefinitions =
        [
            ..CorePhases,
            new MethodologyPhaseDefinition(
                "scaling-safety",
                "Scaling Safety",
                "Explain how to expand beyond the pilot without breaking operations: governance, monitoring, rollback, capacity, and team adoption.",
                ["scaling safely", "governance", "monitoring", "rollback plan", "error handling", "team adoption", "capacity planning", "production readiness"]),
        ],
    };
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
    public string? FeaturedImageUrl { get; init; }
}

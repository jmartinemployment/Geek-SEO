namespace GeekSeo.Application.Models.Seo;

public sealed record ContentClusterPlannerOptions
{
    public int MaxSpokeCandidates { get; init; } = 3;
    public int MaxFaqItems { get; init; } = 5;
    public IReadOnlyList<string> IntentBlocklist { get; init; } =
        ["course", "reddit", "jobs", "salary"];
}

public sealed record ContentClusterPlannerInput
{
    public required string PillarKeyword { get; init; }
    public required WritingResearchContext Research { get; init; }
    public SiteWritingFocus? SiteFocus { get; init; }
    public string? PillarContentHtml { get; init; }
    public ContentClusterPlannerOptions Options { get; init; } = new();
}

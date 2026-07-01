namespace GeekSeo.Application.Models.Seo;

public static class ResearchModes
{
    public const string Manual = "manual";
    public const string Sa2 = "sa2";
}

public static class SerpResearchLanes
{
    public const string Keyword = "keyword";
    public const string Edu = "edu";
    public const string Gov = "gov";
    public const string Local = "local";
    public const string Wiki = "wiki";

    public static readonly IReadOnlySet<string> Supplemental =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Edu, Gov, Local, Wiki };
}

public sealed record ContentWriterManualResearchLane
{
    public required string Lane { get; init; }
    public required string Label { get; init; }
    public int OrganicCount { get; init; }
    public IReadOnlyList<ContentWriterSerpItem> OrganicResults { get; init; } = [];
}

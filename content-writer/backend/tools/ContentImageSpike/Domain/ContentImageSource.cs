namespace ContentImageSpike.Domain;

/// <summary>Read-only snapshot from Content Writer DB — input for image prompt builders.</summary>
public sealed record ContentImageSource(
    Guid ProjectId,
    string ProjectName,
    string TargetKeyword,
    string? DetectedTone,
    PillarImageBrief? Pillar,
    SocialImageBrief? Facebook,
    SocialImageBrief? LinkedIn);

public sealed record PillarImageBrief(
    string Title,
    string MetaDescription,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<string> SectionOutline);

public sealed record SocialImageBrief(
    string Platform,
    string PostText,
    string? RelatedArticleUrl);

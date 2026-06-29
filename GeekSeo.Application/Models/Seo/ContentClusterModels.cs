namespace GeekSeo.Application.Models.Seo;

/// <summary>Resolved spoke readiness for FAQ/body link rendering.</summary>
public static class SpokeLinkStatuses
{
    public const string Planned = "planned";
    public const string ShellCreated = "shell_created";
    public const string BodyGenerated = "body_generated";
}

public sealed record ContentLinkFaqItem
{
    public required string Question { get; init; }
    public Guid? TargetDocumentId { get; init; }
    public string? TargetPath { get; init; }
    public string? AnchorText { get; init; }
    public string? Source { get; init; }
    /// <summary>Derived at render time from child doc state; optional on persisted plan.</summary>
    public string? LinkStatus { get; init; }
}

public sealed record ContentLinkBodySlot
{
    public string? InsertAfterH2Hint { get; init; }
    public Guid? TargetDocumentId { get; init; }
    public string? AnchorText { get; init; }
    public int Priority { get; init; }
    public string MinStatusRequired { get; init; } = SpokeLinkStatuses.BodyGenerated;
}

/// <summary>Pillar-only link graph plan (FAQs + body slots). Stored as JSON on the document.</summary>
public sealed record ContentLinkPlan
{
    public IReadOnlyList<ContentLinkFaqItem> FaqItems { get; init; } = [];
    public IReadOnlyList<ContentLinkBodySlot> BodyLinks { get; init; } = [];
}

public sealed record ContentClusterCandidate
{
    public required string Phrase { get; init; }
    public required string SourceType { get; init; }
    public double Score { get; init; }
    public string? RejectReason { get; init; }
    public string? SuggestedQuestion { get; init; }
    public string? SuggestedSlug { get; init; }
    public Guid? PlannedSpokeId { get; init; }
}

public sealed record ContentClusterFilteredCandidate
{
    public required string Phrase { get; init; }
    public required string SourceType { get; init; }
    public required string RejectReason { get; init; }
}

public sealed record ContentClusterPlanResult
{
    public IReadOnlyList<ContentClusterCandidate> SpokeCandidates { get; init; } = [];
    public IReadOnlyList<ContentLinkFaqItem> FaqItems { get; init; } = [];
    public IReadOnlyList<ContentClusterFilteredCandidate> FilteredOut { get; init; } = [];
}

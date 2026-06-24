using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeekSeo.Application.Models.Seo;

/// <summary>
/// Frozen site + niche context captured when a content document is created or linked to an analysis run.
/// </summary>
public sealed record SiteWritingFocus
{
    public Guid? SiteProfileId { get; init; }
    public required string SiteName { get; init; }
    public required string SiteUrl { get; init; }
    public string PrimaryNiche { get; init; } = string.Empty;
    public string NicheDescription { get; init; } = string.Empty;
    public IReadOnlyList<string> NicheTags { get; init; } = [];
    public string BusinessSummary { get; init; } = string.Empty;
    public string? MatchedPillarTopic { get; init; }
    public string? MatchedPillarIntent { get; init; }
    public string? MatchedPillarAngle { get; init; }
    public IReadOnlyList<string> GeoAnchorNodes { get; init; } = [];
    public string ServiceAreaDescription { get; init; } = string.Empty;
    public IReadOnlyList<string> GapTopics { get; init; } = [];
    public IReadOnlyList<string> CompetitorDomains { get; init; } = [];
    public IReadOnlyList<string> AuthorityPageUrls { get; init; } = [];
    public Guid? NicheProfileId { get; init; }
    public DateTimeOffset? NicheProfileUpdatedAt { get; init; }
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
    /// <summary>Heuristic paragraph for prompts (Phase 1); AI synthesis replaces in Phase 2.</summary>
    public string WritingInstructions { get; init; } = string.Empty;
}

public static class SiteWritingFocusSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string Serialize(SiteWritingFocus focus) =>
        JsonSerializer.Serialize(focus, Options);

    public static SiteWritingFocus? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<SiteWritingFocus>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string ToBusinessContext(SiteWritingFocus focus)
    {
        if (!string.IsNullOrWhiteSpace(focus.WritingInstructions))
            return focus.WritingInstructions.Trim();

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(focus.BusinessSummary))
            parts.Add(focus.BusinessSummary.Trim());
        if (!string.IsNullOrWhiteSpace(focus.PrimaryNiche))
            parts.Add($"Primary niche: {focus.PrimaryNiche.Trim()}.");
        if (!string.IsNullOrWhiteSpace(focus.MatchedPillarTopic))
            parts.Add($"Topic cluster: {focus.MatchedPillarTopic.Trim()}.");

        return parts.Count > 0 ? string.Join(" ", parts) : string.Empty;
    }
}

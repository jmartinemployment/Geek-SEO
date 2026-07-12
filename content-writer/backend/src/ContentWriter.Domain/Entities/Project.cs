using ContentWriter.Domain.Enums;

namespace ContentWriter.Domain.Entities;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ProjectUrl { get; set; } = string.Empty;
    public string TargetKeyword { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
    public LlmProviderType PreferredProvider { get; set; } = LlmProviderType.LmStudio;

    /// <summary>Last tool-page generation outcome (Success, NoToolsSection, etc.). Survives reload.</summary>
    public string? ToolsGenerationOutcome { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public CrawledSite? CrawledSite { get; set; }
    public List<KeywordSource> KeywordSources { get; set; } = new();
    public List<GeneratedContent> GeneratedContents { get; set; } = new();
    public List<ProjectPublication> Publications { get; set; } = new();
}

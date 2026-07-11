using ContentWriter.Domain.Enums;

namespace ContentWriter.Domain.Entities;

public class ContentFigure
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? ImagePromptContentId { get; set; }
    public GeneratedContent? ImagePromptContent { get; set; }

    public string SourceType { get; set; } = FigureSourceType.Pillar;
    public int SectionOrder { get; set; }
    public string HeadingSlug { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public string BriefText { get; set; } = string.Empty;

    public FigureStatus Status { get; set; } = FigureStatus.Pending;
    public string? SkipReason { get; set; }

    public string? ImageUrl { get; set; }
    public int? ImageWidth { get; set; }
    public int? ImageHeight { get; set; }
    public string ImageAlt { get; set; } = string.Empty;

    public string? GeekApiSlug { get; set; }
    public int? GeekPostId { get; set; }
    public bool NeedsFigureMerge { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

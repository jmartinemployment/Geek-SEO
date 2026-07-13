namespace SectionFigures.Models;

public sealed record FigureManifestResponse(
    Guid ProjectId,
    IReadOnlyList<FigureManifestEntry> Figures);

public sealed record FigureManifestEntry(
    string SourceType,
    string HeadingSlug,
    string Heading,
    int SectionOrder,
    string BriefText,
    string Status,
    string? ImageUrl,
    string? GeekApiSlug);

public sealed record FigureJobFile(
    Guid ProjectId,
    string ExportedAtUtc,
    IReadOnlyList<FigureJob> Jobs);

public sealed record FigureJob(
    string SourceType,
    string HeadingSlug,
    string Heading,
    int SectionOrder,
    string GeekApiSlug,
    string BriefText,
    string ComposedPrompt,
    string RelativePath);

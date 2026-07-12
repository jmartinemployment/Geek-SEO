using ContentWriter.Domain.Enums;

namespace ContentWriter.Application.DTOs;

public sealed record ContentFigureDto(
    Guid Id,
    string SourceType,
    int SectionOrder,
    string HeadingSlug,
    string Heading,
    string BriefText,
    FigureStatus Status,
    string? SkipReason,
    string? ImageUrl,
    int? ImageWidth,
    int? ImageHeight,
    string ImageAlt,
    string? GeekApiSlug,
    int? GeekPostId,
    bool NeedsFigureMerge,
    Guid? ImagePromptContentId,
    DateTime UpdatedAtUtc);

public sealed record ContentFiguresListResponse(
    Guid ProjectId,
    IReadOnlyList<ContentFigureDto> Figures,
    ContentFiguresSummary Summary);

public sealed record ContentFiguresSummary(
    int Pending,
    int Ready,
    int Skipped,
    int Published,
    int MissingGeekApiSlug);

public sealed record ContentFigureManifestEntry(
    string SourceType,
    string HeadingSlug,
    string Heading,
    int SectionOrder,
    string BriefText,
    FigureStatus Status,
    string? ImageUrl,
    string? GeekApiSlug,
    bool NeedsFigureMerge);

public sealed record ContentFigureManifestResponse(
    Guid ProjectId,
    IReadOnlyList<ContentFigureManifestEntry> Figures);

public sealed record FigureMergeRequest(string Source);

public sealed record FigureMergeResponse(
    string SourceType,
    string GeekApiSlug,
    int GeekPostId,
    int FiguresMerged,
    string PublicPath);

public sealed record FigureGenerateRequest(string Source, string? HeadingSlug);

public sealed record FigureSetUrlRequest(string Url, string? Alt);

public sealed record FigureGenerateResponse(
    string SourceType,
    int GeneratedCount,
    IReadOnlyList<ContentFigureDto> Figures);

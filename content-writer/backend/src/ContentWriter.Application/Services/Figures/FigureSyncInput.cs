namespace ContentWriter.Application.Services.Figures;

public sealed record FigureSyncSectionInput(
    string SourceType,
    string Heading,
    int SectionOrder,
    string BriefText,
    Guid? ImagePromptContentId);

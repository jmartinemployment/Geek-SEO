using ContentWriter.Application.Services.PromptBuilders;
using ContentWriter.Domain.Enums;

namespace ContentWriter.Application.Services;

public static class ImagePromptWordLimits
{
    public static (int Min, int Max) ForSection(ImagePromptSectionTarget section)
    {
        if (IsAdvertisementFigure(section))
        {
            return (ImagePromptDefaults.AdvertisementPromptMinWords, ImagePromptDefaults.AdvertisementPromptMaxWords);
        }

        return (ImagePromptDefaults.PromptMinWords, ImagePromptDefaults.PromptMaxWords);
    }

    public static bool IsAdvertisementFigure(ImagePromptSectionTarget section) =>
        FigureSourceType.IsTool(section.SourceType)
        || (string.Equals(section.SourceType, FigureSourceType.Pillar, StringComparison.OrdinalIgnoreCase)
            && PillarSectionClassifier.IsToolsSection(section.Heading));
}

using ContentWriter.Application.Providers;
using ContentWriter.Domain.Enums;

namespace ContentWriter.Application.Services.Figures;

public static class FigureSourceValidator
{
    public static void ValidateSourceType(string sourceType)
    {
        if (!FigureSourceType.IsKnown(sourceType))
        {
            throw new ArgumentException(
                $"Source must be '{FigureSourceType.Pillar}', '{FigureSourceType.Blog}', or '{FigureSourceType.ToolPrefix}{{slug}}'.",
                nameof(sourceType));
        }
    }
}

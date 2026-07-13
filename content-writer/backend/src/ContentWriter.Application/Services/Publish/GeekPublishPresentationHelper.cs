using ContentWriter.Application.Services.Figures;

namespace ContentWriter.Application.Services.Publish;

internal static class GeekPublishPresentationHelper
{
    public static string StripMergedFigures(string body) => MergedFigureMarkup.Strip(body);
}

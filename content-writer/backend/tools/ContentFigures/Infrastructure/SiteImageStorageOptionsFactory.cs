using ContentWriter.Application.Services.Figures;

namespace ContentFigures.Infrastructure;

internal static class SiteImageStorageOptionsFactory
{
    public static SiteImageStorageOptions Create()
    {
        var options = new SiteImageStorageOptions();
        var publicBase = Environment.GetEnvironmentVariable("CONTENT_IMAGE_PUBLIC_BASE_URL");
        if (!string.IsNullOrWhiteSpace(publicBase))
        {
            options.PublicBaseUrl = publicBase;
        }

        var outputRoot = Environment.GetEnvironmentVariable("CONTENT_IMAGE_OUTPUT_DIR");
        if (!string.IsNullOrWhiteSpace(outputRoot))
        {
            options.LocalOutputRoot = outputRoot;
        }

        return options;
    }
}

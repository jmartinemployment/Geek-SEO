namespace ContentWriter.Application.Services.Figures;

/// <summary>
/// Builds public image paths under images/{TechnicalArticle|Blog|Tool}/{department}/{pageSlug}/.
/// </summary>
public static class FigurePublicPathBuilder
{
    public static string BuildRelativePath(string geekApiSlug, string headingSlug)
    {
        if (string.IsNullOrWhiteSpace(geekApiSlug))
        {
            throw new ArgumentException("GeekApiSlug is required.", nameof(geekApiSlug));
        }

        if (string.IsNullOrWhiteSpace(headingSlug))
        {
            throw new ArgumentException("HeadingSlug is required.", nameof(headingSlug));
        }

        var (contentFolder, department, pageSlug) = ParseGeekApiSlug(geekApiSlug);
        return $"images/{contentFolder}/{department}/{pageSlug}/h2-{headingSlug.Trim()}.avif";
    }

    public static string BuildHeroRelativePath(string geekApiSlug)
    {
        if (string.IsNullOrWhiteSpace(geekApiSlug))
        {
            throw new ArgumentException("GeekApiSlug is required.", nameof(geekApiSlug));
        }

        var (contentFolder, department, pageSlug) = ParseGeekApiSlug(geekApiSlug);
        return $"images/{contentFolder}/{department}/{pageSlug}/hero.avif";
    }

    public static (string ContentFolder, string Department, string PageSlug) ParseGeekApiSlug(string geekApiSlug)
    {
        var parts = geekApiSlug.Trim().Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            throw new ArgumentException($"GeekApiSlug must be {{prefix}}/{{department}}/{{pageSlug}}: {geekApiSlug}");
        }

        var prefix = parts[0];
        var department = parts[1];
        var pageSlug = string.Join('/', parts.Skip(2));

        var contentFolder = prefix switch
        {
            "use-cases" => "TechnicalArticle",
            "blog" => "Blog",
            "tools" => "Tool",
            _ => throw new ArgumentException($"Unsupported GeekApiSlug prefix '{prefix}'.", nameof(geekApiSlug)),
        };

        return (contentFolder, department, pageSlug);
    }
}

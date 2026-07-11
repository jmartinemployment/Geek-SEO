namespace ContentWriter.Application.Services.Figures;

public static class FigureBlobPathBuilder
{
    public static string BuildBlobPathname(string geekApiSlug, string sourceType, string headingSlug)
    {
        if (string.IsNullOrWhiteSpace(geekApiSlug))
        {
            throw new ArgumentException("GeekApiSlug is required.", nameof(geekApiSlug));
        }

        if (string.IsNullOrWhiteSpace(sourceType))
        {
            throw new ArgumentException("SourceType is required.", nameof(sourceType));
        }

        if (string.IsNullOrWhiteSpace(headingSlug))
        {
            throw new ArgumentException("HeadingSlug is required.", nameof(headingSlug));
        }

        var slug = geekApiSlug.Trim().Trim('/');
        return $"content/{slug}/{sourceType.Trim()}/h2-{headingSlug.Trim()}.webp";
    }
}

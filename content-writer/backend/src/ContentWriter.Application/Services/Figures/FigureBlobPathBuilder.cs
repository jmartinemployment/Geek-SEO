namespace ContentWriter.Application.Services.Figures;

public static class FigureBlobPathBuilder
{
    public static string BuildBlobPathname(string geekApiSlug, string headingSlug)
    {
        if (string.IsNullOrWhiteSpace(geekApiSlug))
        {
            throw new ArgumentException("GeekApiSlug is required.", nameof(geekApiSlug));
        }

        if (string.IsNullOrWhiteSpace(headingSlug))
        {
            throw new ArgumentException("HeadingSlug is required.", nameof(headingSlug));
        }

        return FigurePublicPathBuilder.BuildRelativePath(geekApiSlug, headingSlug);
    }
}

using ContentWriter.Application.Services;

namespace ContentWriter.Application.Services.Figures;

public static class FigureHeadingSlugResolver
{
    public static string ResolveUniqueSlug(string heading, int order, IReadOnlySet<string> usedSlugs)
    {
        var baseSlug = SlugHelper.Slugify(heading);
        if (!usedSlugs.Contains(baseSlug))
        {
            return baseSlug;
        }

        var withOrder = $"{baseSlug}-{order}";
        if (!usedSlugs.Contains(withOrder))
        {
            return withOrder;
        }

        var suffix = 2;
        string candidate;
        do
        {
            candidate = $"{baseSlug}-{order}-{suffix}";
            suffix++;
        } while (usedSlugs.Contains(candidate));

        return candidate;
    }

    public static string DefaultImageAlt(string heading)
    {
        var alt = $"Diagram: {heading.Trim()}";
        return alt.Length <= 200 ? alt : alt[..200];
    }
}

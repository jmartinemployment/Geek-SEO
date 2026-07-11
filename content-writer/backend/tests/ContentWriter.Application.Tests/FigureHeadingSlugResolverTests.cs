using ContentWriter.Application.Services.Figures;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;

namespace ContentWriter.Application.Tests;

public class FigureHeadingSlugResolverTests
{
    [Fact]
    public void ResolveUniqueSlug_returns_base_slug_when_unique()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var slug = FigureHeadingSlugResolver.ResolveUniqueSlug("Cost allocation", 1, used);
        Assert.Equal("cost-allocation", slug);
    }

    [Fact]
    public void ResolveUniqueSlug_appends_order_on_collision()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "tools" };
        var slug = FigureHeadingSlugResolver.ResolveUniqueSlug("Tools", 2, used);
        Assert.Equal("tools-2", slug);
    }

    [Fact]
    public void DefaultImageAlt_prefixes_heading()
    {
        Assert.Equal("Diagram: Revenue recognition", FigureHeadingSlugResolver.DefaultImageAlt("Revenue recognition"));
    }
}

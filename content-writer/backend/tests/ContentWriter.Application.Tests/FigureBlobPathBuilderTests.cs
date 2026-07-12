using ContentWriter.Application.Services.Figures;

namespace ContentWriter.Application.Tests;

public class FigureBlobPathBuilderTests
{
    [Fact]
    public void BuildBlobPathname_uses_type_department_and_page_slug()
    {
        var path = FigureBlobPathBuilder.BuildBlobPathname(
            "use-cases/accounting/smart-bank-reconciliation",
            "cost-allocation");

        Assert.Equal(
            "images/TechnicalArticle/accounting/smart-bank-reconciliation/h2-cost-allocation.webp",
            path);
    }

    [Theory]
    [InlineData("", "intro")]
    [InlineData("use-cases/sales/foo", "")]
    public void BuildBlobPathname_throws_when_required_part_missing(string geekApiSlug, string headingSlug)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            FigureBlobPathBuilder.BuildBlobPathname(geekApiSlug, headingSlug));
    }
}

public class FigureSyncDirMatcherTests
{
    [Fact]
    public void ResolveHeadingSlug_matches_exact_filename()
    {
        var slug = FigureSyncDirMatcher.ResolveHeadingSlug(
            "h2-cost-allocation.webp",
            ["cost-allocation", "intro"]);

        Assert.Equal("cost-allocation", slug);
    }

    [Fact]
    public void ResolveHeadingSlug_matches_prefixed_filename()
    {
        var slug = FigureSyncDirMatcher.ResolveHeadingSlug(
            "h2-03-cost-allocation.webp",
            ["cost-allocation", "intro"]);

        Assert.Equal("cost-allocation", slug);
    }

    [Fact]
    public void ResolveHeadingSlug_returns_null_when_no_match()
    {
        var slug = FigureSyncDirMatcher.ResolveHeadingSlug(
            "h2-unknown.webp",
            ["cost-allocation"]);

        Assert.Null(slug);
    }

    [Fact]
    public void ResolveHeadingSlug_prefers_longest_slug()
    {
        var slug = FigureSyncDirMatcher.ResolveHeadingSlug(
            "h2-smart-bank-reconciliation.webp",
            ["bank", "smart-bank-reconciliation"]);

        Assert.Equal("smart-bank-reconciliation", slug);
    }
}

using ContentWriter.Application.Services.Figures;

namespace ContentWriter.Application.Tests;

public class FigurePublicPathBuilderTests
{
    [Theory]
    [InlineData("use-cases/accounting/smart-bank-reconciliation", "cost-allocation", "images/TechnicalArticle/accounting/smart-bank-reconciliation/h2-cost-allocation.avif")]
    [InlineData("blog/sales/quarterly-update", "intro", "images/Blog/sales/quarterly-update/h2-intro.avif")]
    [InlineData("tools/marketing/hubspot-ai", "capabilities", "images/Tool/marketing/hubspot-ai/h2-capabilities.avif")]
    public void BuildRelativePath_maps_prefix_to_content_folder(
        string geekApiSlug,
        string headingSlug,
        string expected)
    {
        Assert.Equal(expected, FigurePublicPathBuilder.BuildRelativePath(geekApiSlug, headingSlug));
    }

    [Theory]
    [InlineData("blog/sales/quarterly-update", "images/Blog/sales/quarterly-update/hero.avif")]
    [InlineData("tools/marketing/hubspot-ai", "images/Tool/marketing/hubspot-ai/hero.avif")]
    public void BuildHeroRelativePath_maps_prefix_to_content_folder(string geekApiSlug, string expected)
    {
        Assert.Equal(expected, FigurePublicPathBuilder.BuildHeroRelativePath(geekApiSlug));
    }
}

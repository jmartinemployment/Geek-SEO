using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class UrlPageBusinessContextResolverTests
{
    [Fact]
    public void Derive_UsesMetaDescriptionAndDomain()
    {
        var page = new PageContent
        {
            Url = "https://www.example.com/blog/quickbooks-automation",
            FullText = "Long body text that should not be needed when meta description exists.",
            MetaTitle = "QuickBooks automation guide",
            MetaDescription = "We help small businesses automate QuickBooks workflows and integrations.",
            CrawledAt = DateTimeOffset.UtcNow,
        };

        var context = UrlPageBusinessContextResolver.Derive(page, page.Url);

        Assert.Contains("Example", context, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("QuickBooks", context, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Derive_FallsBackToLeadSentences()
    {
        var page = new PageContent
        {
            Url = "https://example.com/blog/quickbooks-automation",
            FullText = "QuickBooks automation saves time for small business owners. It reduces manual bookkeeping and improves accuracy across workflows.",
            CrawledAt = DateTimeOffset.UtcNow,
        };

        var context = UrlPageBusinessContextResolver.Derive(page, page.Url);

        Assert.Contains("QuickBooks", context, StringComparison.OrdinalIgnoreCase);
    }
}

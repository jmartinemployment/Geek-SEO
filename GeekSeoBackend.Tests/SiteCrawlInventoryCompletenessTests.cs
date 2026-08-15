using GeekSeoBackend.Services.SiteAnalyzerStepRunners;

namespace GeekSeoBackend.Tests;

public sealed class SiteCrawlInventoryCompletenessTests
{
    [Fact]
    public void Evaluate_excludes_missing_inventory_urls_without_throwing()
    {
        var inventory = new[]
        {
            "https://example.com/",
            "https://example.com/about",
            "https://example.com/noindex-page",
        };
        var fetched = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "https://example.com/",
            "https://example.com/about",
        };

        var message = SiteCrawlInventoryCompleteness.Evaluate(inventory, fetched, out var missing);

        Assert.Equal(["https://example.com/noindex-page"], missing);
        Assert.Contains("2 of 3 inventory page(s) fetched", message);
        Assert.Contains("1 not fetched", message);
    }

    [Fact]
    public void Evaluate_complete_when_all_inventory_fetched()
    {
        var inventory = new[] { "https://example.com/", "https://example.com/about" };
        var fetched = new HashSet<string>(inventory, StringComparer.OrdinalIgnoreCase);

        var message = SiteCrawlInventoryCompleteness.Evaluate(inventory, fetched, out var missing);

        Assert.Empty(missing);
        Assert.Contains("(complete)", message);
    }

    [Fact]
    public void Evaluate_does_not_throw_when_inventory_misses_but_pages_were_fetched()
    {
        var inventory = new[]
        {
            "https://example.com/a",
            "https://example.com/b",
        };
        var fetched = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "https://example.com/",
        };

        var message = SiteCrawlInventoryCompleteness.Evaluate(inventory, fetched, out var missing);

        Assert.Equal(2, missing.Count);
        Assert.Contains("0 of 2 inventory page(s) fetched", message);
        Assert.Contains("2 not fetched", message);
    }

    [Fact]
    public void Evaluate_treats_trailing_slash_as_the_same_url()
    {
        var inventory = new[] { "https://example.com" };
        var fetched = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "https://example.com/",
        };

        var message = SiteCrawlInventoryCompleteness.Evaluate(inventory, fetched, out var missing);

        Assert.Empty(missing);
        Assert.Contains("(complete)", message);
    }

    [Fact]
    public void Evaluate_throws_when_zero_pages_fetched()
    {
        var inventory = new[] { "https://example.com/a" };
        var fetched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SiteCrawlInventoryCompleteness.Evaluate(inventory, fetched, out _));

        Assert.Contains("fetched no pages", ex.Message);
        Assert.DoesNotContain("inventory URL(s) were fetched", ex.Message);
    }
}

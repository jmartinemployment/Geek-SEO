using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ArticleClosingFaqEnricherTests
{
    [Fact]
    public void HasClosingFaqSection_returns_true_when_five_h3_follow_faq_heading()
    {
        var html = """
            <h1>Guide</h1>
            <h2>Frequently Asked Questions</h2>
            <h3>What is bookkeeping?</h3><p>Answer one.</p>
            <h3>How much does it cost?</h3><p>Answer two.</p>
            <h3>How long does it take?</h3><p>Answer three.</p>
            <h3>What are the benefits?</h3><p>Answer four.</p>
            <h3>Who should use it?</h3><p>Answer five.</p>
            """;

        Assert.True(ArticleClosingFaqEnricher.HasClosingFaqSection(html));
    }

    [Fact]
    public void EnsureClosingFaqOutline_appends_missing_faq_headings()
    {
        var brief = new ContentBrief
        {
            Keyword = "Automated Bookkeeping & Data Entry",
            Location = "United States",
            ClosingFaqQuestions =
            [
                "What is automated bookkeeping?",
                "How much does automated bookkeeping cost?",
                "How long does setup take?",
                "What are the benefits?",
                "Who should use automated bookkeeping?",
            ],
        };

        var outline = "<h2>Business objectives</h2><h2>Implementation plan</h2>";
        var enriched = ArticleClosingFaqEnricher.EnsureClosingFaqOutline(outline, brief);

        Assert.Contains("Frequently Asked Questions", enriched, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("What is automated bookkeeping?", enriched, StringComparison.OrdinalIgnoreCase);
        Assert.True(ArticleClosingFaqEnricher.HasClosingFaqSection(enriched));
    }
}

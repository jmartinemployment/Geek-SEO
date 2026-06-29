using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ContentLinkPlanJsonTests
{
    [Fact]
    public void Parse_returns_empty_plan_for_null_or_blank()
    {
        Assert.Empty(ContentLinkPlanJson.Parse(null).FaqItems);
        Assert.Empty(ContentLinkPlanJson.Parse("   ").BodyLinks);
    }

    [Fact]
    public void Serialize_round_trips_plan_items()
    {
        var plan = new ContentLinkPlan
        {
            FaqItems =
            [
                new ContentLinkFaqItem
                {
                    Question = "Which AI tools are best for market research?",
                    TargetPath = "/blog/best-ai-tools-market-research",
                    AnchorText = "best AI tools for market research",
                    Source = "paa",
                },
            ],
            BodyLinks =
            [
                new ContentLinkBodySlot
                {
                    InsertAfterH2Hint = "implementation",
                    AnchorText = "market research tools guide",
                    Priority = 1,
                },
            ],
        };

        var json = ContentLinkPlanJson.Serialize(plan);
        var parsed = ContentLinkPlanJson.Parse(json);

        Assert.Single(parsed.FaqItems);
        Assert.Equal(plan.FaqItems[0].Question, parsed.FaqItems[0].Question);
        Assert.Equal(plan.FaqItems[0].TargetPath, parsed.FaqItems[0].TargetPath);
        Assert.Single(parsed.BodyLinks);
        Assert.Equal(plan.BodyLinks[0].InsertAfterH2Hint, parsed.BodyLinks[0].InsertAfterH2Hint);
    }
}

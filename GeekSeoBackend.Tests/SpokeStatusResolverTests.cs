using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class SpokeStatusResolverTests
{
    [Fact]
    public void ResolveFaqAssignments_marks_body_generated_spoke_active()
    {
        var childId = Guid.NewGuid();
        var plan = new ContentLinkPlan
        {
            FaqItems =
            [
                new ContentLinkFaqItem
                {
                    Question = "Which AI tools are best for market research?",
                    TargetPath = "/blog/best-ai-tools-market-research",
                    AnchorText = "best AI tools for market research",
                },
            ],
        };

        var children = new[]
        {
            new SeoContentDocument
            {
                Id = childId,
                PublishSlug = "best-ai-tools-market-research",
                Status = SpokeLinkStatuses.BodyGenerated,
                WordCount = 900,
                ContentHtml = "<p>Generated spoke body.</p>",
            },
        };

        var assignments = SpokeStatusResolver.ResolveFaqAssignments(plan, children);

        Assert.Single(assignments);
        Assert.True(assignments[0].IsTargetActive);
        Assert.Equal("/blog/best-ai-tools-market-research", assignments[0].TargetPath);
    }

    [Fact]
    public void ResolveFaqAssignments_keeps_planned_spoke_inactive()
    {
        var plan = new ContentLinkPlan
        {
            FaqItems =
            [
                new ContentLinkFaqItem
                {
                    Question = "Are there free AI tools for market research?",
                    TargetPath = "/blog/free-ai-market-research-tools",
                    AnchorText = "free AI tools for market research",
                },
            ],
        };

        var children = new[]
        {
            new SeoContentDocument
            {
                Id = Guid.NewGuid(),
                PublishSlug = "free-ai-market-research-tools",
                Status = SpokeLinkStatuses.ShellCreated,
                WordCount = 12,
                ContentHtml = "<p>Spoke draft shell. Generate full content in a later step.</p>",
            },
        };

        var assignments = SpokeStatusResolver.ResolveFaqAssignments(plan, children);

        Assert.Single(assignments);
        Assert.False(assignments[0].IsTargetActive);
        Assert.Equal(string.Empty, assignments[0].TargetPath);
    }
}

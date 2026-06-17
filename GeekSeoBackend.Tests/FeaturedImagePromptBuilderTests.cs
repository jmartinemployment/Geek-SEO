using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class FeaturedImagePromptBuilderTests
{
    [Fact]
    public void BuildForDocument_UsesKeywordAndTitle()
    {
        var prompt = FeaturedImagePromptBuilder.BuildForDocument(new SeoContentDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Automated Bookkeeping Playbook",
            TargetKeyword = "bookkeeping automation",
        });

        Assert.Contains("bookkeeping automation", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Automated Bookkeeping Playbook", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no text", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_UsesBookkeepingMetaphor()
    {
        var prompt = FeaturedImagePromptBuilder.Build(
            "bookkeeping automation",
            "Automated Bookkeeping Playbook");

        Assert.Contains("ledger", prompt, StringComparison.OrdinalIgnoreCase);
    }
}

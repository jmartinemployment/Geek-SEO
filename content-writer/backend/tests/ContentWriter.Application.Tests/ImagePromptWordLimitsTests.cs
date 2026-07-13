using ContentWriter.Application.Services;

namespace ContentWriter.Application.Tests;

public class ImagePromptWordLimitsTests
{
    [Fact]
    public void ForSection_PillarToolsHeading_UsesAdvertisementLimits()
    {
        var section = new ImagePromptSectionTarget(
            "pillar",
            "Choosing the Right Tools for Automated Transaction Categorization",
            1);

        var (min, max) = ImagePromptWordLimits.ForSection(section);

        Assert.Equal(ImagePromptDefaults.AdvertisementPromptMinWords, min);
        Assert.Equal(ImagePromptDefaults.AdvertisementPromptMaxWords, max);
    }

    [Fact]
    public void ForSection_ToolSource_UsesAdvertisementLimits()
    {
        var section = new ImagePromptSectionTarget("tool/quickbooks-online", "Overview", 1);

        var (min, max) = ImagePromptWordLimits.ForSection(section);

        Assert.Equal(ImagePromptDefaults.AdvertisementPromptMinWords, min);
        Assert.Equal(ImagePromptDefaults.AdvertisementPromptMaxWords, max);
    }

    [Fact]
    public void ForSection_StandardPillar_UsesTeachingLimits()
    {
        var section = new ImagePromptSectionTarget("pillar", "Why reconciliation matters", 1);

        var (min, max) = ImagePromptWordLimits.ForSection(section);

        Assert.Equal(ImagePromptDefaults.PromptMinWords, min);
        Assert.Equal(ImagePromptDefaults.PromptMaxWords, max);
    }
}

using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class MethodologySectionHintBuilderTests
{
    [Fact]
    public void BuildPlans_UsesMethodologyHeadings_NotOrganicTitles()
    {
        var plans = MethodologySectionHintBuilder.BuildPlans(
            "marketing ai customer journeys",
            [
                "Marketing AI Customer Journeys PDF",
                "Marketing AI Customer Journeys Examples",
                "Customer journey map",
                "AI for customer journeys: a transformer approach",
            ]);

        Assert.Equal(4, plans.Count);
        Assert.Equal("Business Objectives", plans[0].Phase.Label);
        Assert.Contains("marketing ai customer journeys", plans[0].SuggestedH2, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PDF", plans[0].SuggestedH2, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Marketing AI Customer Journeys PDF", plans[0].SubtopicsFromSerp);
    }
}

public sealed class MethodologySectionHintMapperTests
{
    [Fact]
    public void BuildSectionHints_maps_methodology_spine_for_content_writer_export()
    {
        var hints = InvokeBuildSectionHints(
            "marketing ai customer journeys",
            [new ContentWriterSerpItem { Type = "organic", Title = "Marketing AI Customer Journeys PDF", Url = "https://a.test" }],
            ["What is an AI customer journey?"],
            ["customer journey funnel"]);

        Assert.Equal(4, hints.Count);
        Assert.Equal("Business Objectives", hints[0].Label);
        Assert.Contains("marketing ai customer journeys", hints[0].SuggestedH2, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PDF", hints[0].SuggestedH2, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Marketing AI Customer Journeys PDF", hints[0].SubtopicsFromSerp);
    }

    private static IReadOnlyList<WritingResearchSectionHint> InvokeBuildSectionHints(
        string keyword,
        IReadOnlyList<ContentWriterSerpItem> organic,
        IReadOnlyList<string> paaQuestions,
        IReadOnlyList<string> pasfQueries)
    {
        var method = typeof(ContentWriterSerpExportMapper).GetMethod(
            "BuildSectionHints",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        return (IReadOnlyList<WritingResearchSectionHint>)method.Invoke(
            null,
            [keyword, organic, paaQuestions, pasfQueries])!;
    }
}

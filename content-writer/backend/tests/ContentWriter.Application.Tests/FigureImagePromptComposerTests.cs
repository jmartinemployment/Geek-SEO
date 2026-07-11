using ContentWriter.Application.Services.Figures;

namespace ContentWriter.Application.Tests;

public class FigureImagePromptComposerTests
{
    [Fact]
    public void Compose_includes_brief_and_no_text_guardrail()
    {
        var prompt = FigureImagePromptComposer.Compose(
            "Show a funnel with three stages for invoice intake.",
            "Why automation matters");

        Assert.Contains("Why automation matters", prompt);
        Assert.Contains("invoice intake", prompt);
        Assert.Contains("No text", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compose_empty_brief_throws()
    {
        Assert.Throws<Providers.ContentGenerationException>(() =>
            FigureImagePromptComposer.Compose("   ", "Heading"));
    }
}

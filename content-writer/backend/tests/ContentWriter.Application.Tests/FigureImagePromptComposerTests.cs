using ContentWriter.Application.Services.Figures;

namespace ContentWriter.Application.Tests;

public class FigureImagePromptComposerTests
{
    [Fact]
    public void Compose_includes_brief_and_real_label_guardrails()
    {
        var prompt = FigureImagePromptComposer.Compose(
            "Show a funnel with three stages for invoice intake.",
            "Why automation matters");

        Assert.Contains("Why automation matters", prompt);
        Assert.Contains("invoice intake", prompt);
        Assert.Contains("real English labels", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No gibberish", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("No text, letters, words", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compose_empty_brief_throws()
    {
        Assert.Throws<Providers.ContentGenerationException>(() =>
            FigureImagePromptComposer.Compose("   ", "Heading"));
    }
}

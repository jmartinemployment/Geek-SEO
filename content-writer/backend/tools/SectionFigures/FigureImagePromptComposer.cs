// Ported from ContentWriter.Application.Services.Figures.FigureImagePromptComposer — keep in sync.

namespace SectionFigures;

public static class FigureImagePromptComposer
{
    public static string Compose(string briefText, string heading)
    {
        var brief = briefText.Trim();
        if (string.IsNullOrWhiteSpace(brief))
        {
            throw new InvalidOperationException(
                $"Figure brief for \"{heading}\" is empty. Run Step 6 first.");
        }

        return $"""
            Create a flat vector infographic diagram illustrating this section topic.
            No text, letters, words, labels, numbers, logos, or typography anywhere in the image.
            Clean outlines, simple shapes, limited palette, professional B2B style.
            Section topic: {heading.Trim()}
            Art direction: {brief}
            """;
    }
}

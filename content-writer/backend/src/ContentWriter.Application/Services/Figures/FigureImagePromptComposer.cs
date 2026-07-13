using ContentWriter.Application.Providers;

namespace ContentWriter.Application.Services.Figures;

public static class FigureImagePromptComposer
{
    public static string Compose(string briefText, string heading)
    {
        var brief = briefText.Trim();
        if (string.IsNullOrWhiteSpace(brief))
        {
            throw new ContentGenerationException(
                $"Figure brief for \"{heading}\" is empty. Run Step 6 first.");
        }

        return $"""
            Create a flat vector B2B infographic diagram illustrating this section topic.
            Use short, real English labels that match the section topic — clear words the reader of the article would recognize.
            Prefer 3–8 short labels or step titles. Spelling must be correct. No gibberish, fake words, or random letter strings.
            No brand logos, watermarks, or URLs.
            Clean outlines, simple shapes, limited palette, professional fintech / consulting style.
            Section topic: {heading.Trim()}
            Art direction: {brief}
            """;
    }
}

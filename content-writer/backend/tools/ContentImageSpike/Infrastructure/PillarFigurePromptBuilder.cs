using ContentImageSpike.Abstractions;
using ContentImageSpike.Domain;

namespace ContentImageSpike.Infrastructure;

/// <summary>Teaches a concept — flat diagram / figure style for inline pillar use.</summary>
public sealed class PillarFigurePromptBuilder : IImagePromptBuilder
{
    public ImageUseCase UseCase => ImageUseCase.PillarFigure;

    public ImageGenerationRequest Build(ContentImageSource source)
    {
        if (source.Pillar is null)
            throw new InvalidOperationException("Project has no pillar (TechnicalArticle) content.");

        var sections = source.Pillar.SectionOutline.Count > 0
            ? string.Join("; ", source.Pillar.SectionOutline.Take(4))
            : "key implementation steps";

        var keywords = source.Pillar.Keywords.Count > 0
            ? string.Join(", ", source.Pillar.Keywords.Take(6))
            : source.TargetKeyword;

        var prompt = $"""
            Clean flat vector infographic for a B2B IT consulting article.
            Title: "{source.Pillar.Title}".
            Topic keywords: {keywords}.
            Visualize a simple conceptual flow covering: {sections}.
            Style: professional, minimal, white or light gray background, 3–4 labeled stages with simple icons.
            No company logos, no watermarks, no garbled or misspelled text labels.
            Suitable as an inline educational figure in a technical blog post.
            """;

        return new ImageGenerationRequest(UseCase, prompt.Trim(), Width: 1536, Height: 1024);
    }
}

namespace ContentWriter.Application.Services.Figures;

public class FigureImageGenerationOptions
{
    public const string SectionName = "FigureImageGeneration";

    public string OpenAiModel { get; set; } = "dall-e-3";

    /// <summary>When false, in-app OpenAI generation is disabled; operators copy briefs and upload WebP manually.</summary>
    public bool InAppGenerationEnabled { get; set; }

    /// <summary>When true and <see cref="InAppGenerationEnabled"/> is true, publish triggers pending figure generation.</summary>
    public bool AutoGenerateOnPublish { get; set; }
}

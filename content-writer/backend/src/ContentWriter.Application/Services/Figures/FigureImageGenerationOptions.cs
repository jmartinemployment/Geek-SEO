namespace ContentWriter.Application.Services.Figures;

public class FigureImageGenerationOptions
{
    public const string SectionName = "FigureImageGeneration";

    public string OpenAiModel { get; set; } = "dall-e-3";

    /// <summary>When false, in-app OpenAI generation is disabled; operators copy briefs and upload AVIF manually.</summary>
    public bool InAppGenerationEnabled { get; set; }

    /// <summary>
    /// Vetoed — must remain false. Operators save art explicitly (per-section generate/upload/CLI), never on publish.
    /// </summary>
    [Obsolete("Auto-generate on publish is vetoed. Do not enable.")]
    public bool AutoGenerateOnPublish { get; set; }
}

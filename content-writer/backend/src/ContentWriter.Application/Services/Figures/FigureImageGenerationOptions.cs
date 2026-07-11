namespace ContentWriter.Application.Services.Figures;

public class FigureImageGenerationOptions
{
    public const string SectionName = "FigureImageGeneration";

    public string OpenAiModel { get; set; } = "dall-e-3";
}

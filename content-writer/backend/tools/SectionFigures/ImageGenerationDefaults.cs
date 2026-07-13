namespace SectionFigures;

public static class ImageGenerationDefaults
{
    public const string OpenAiModel = "gpt-image-1";
    public const string OpenAiSize = "1536x1024";

    /// <summary>USD per image at 1792x1024 standard quality — verify against current OpenAI pricing.</summary>
    public const decimal EstimatedCostPerImageUsd = 0.080m;
}

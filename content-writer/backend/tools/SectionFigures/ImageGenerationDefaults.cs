namespace SectionFigures;

public static class ImageGenerationDefaults
{
    public const string OpenAiModel = "dall-e-3";
    public const string OpenAiSize = "1792x1024";

    /// <summary>USD per image at 1792x1024 standard quality — verify against current OpenAI pricing.</summary>
    public const decimal EstimatedCostPerImageUsd = 0.080m;

    public const int CostConfirmThreshold = 5;
}

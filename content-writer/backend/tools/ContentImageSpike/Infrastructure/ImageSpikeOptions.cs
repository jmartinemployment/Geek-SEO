namespace ContentImageSpike.Infrastructure;

public sealed class ImageSpikeOptions
{
    public const string SectionName = "ImageSpike";

    public string OutputDirectory { get; set; } = "output/image-spike";

    public string OpenAiApiKey { get; set; } = string.Empty;

    public string OpenAiModel { get; set; } = "gpt-image-1";

    public string LeonardoApiKey { get; set; } = string.Empty;

    /// <summary>Leonardo model UUID (default: Leonardo Phoenix).</summary>
    public string LeonardoModelId { get; set; } = "de7d3faf-762f-48e0-b3b7-9d0ac3a3fcf3";

    public int LeonardoPollIntervalMs { get; set; } = 3000;

    public int LeonardoPollTimeoutSeconds { get; set; } = 120;
}

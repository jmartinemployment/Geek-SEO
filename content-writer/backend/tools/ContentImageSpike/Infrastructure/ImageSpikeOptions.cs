namespace ContentImageSpike.Infrastructure;

public sealed class ImageSpikeOptions
{
    public const string SectionName = "ImageSpike";

    public string OutputDirectory { get; set; } = "output/image-spike";

    public string OpenAiApiKey { get; set; } = string.Empty;

    public string OpenAiModel { get; set; } = "gpt-image-1";
}

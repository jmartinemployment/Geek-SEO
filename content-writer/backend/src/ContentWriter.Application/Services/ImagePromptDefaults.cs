namespace ContentWriter.Application.Services;

public static class ImagePromptDefaults
{
    /// <summary>OpenAI image model for figure generation (FigureImageGeneration:OpenAiModel).</summary>
    public const string OpenAiImageModel = "gpt-image-1";

    public const int PillarWidth = 1536;
    public const int PillarHeight = 1024;

    public const int SocialWidth = 1200;
    public const int SocialHeight = 630;

    /// <summary>Teaching diagram briefs for standard pillar/blog H2 sections.</summary>
    public const int PromptMinWords = 40;
    public const int PromptMaxWords = 400;

    /// <summary>Sponsored advertisement figure briefs — pillar Top AI Tools H2 and tool page sections.</summary>
    public const int AdvertisementPromptMinWords = 80;
    public const int AdvertisementPromptMaxWords = 800;
}

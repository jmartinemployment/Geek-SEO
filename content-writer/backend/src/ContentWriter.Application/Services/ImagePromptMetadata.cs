using System.Text.Json;
using System.Text.Json.Serialization;
using ContentWriter.Application.DTOs;
using ContentWriter.Domain.Entities;

namespace ContentWriter.Application.Services;

public static class ImagePromptMetadata
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(ImagePromptSectionDraft item) =>
        JsonSerializer.Serialize(new StoredImagePromptSettings
        {
            Width = item.Width,
            Height = item.Height,
            ImageModel = ImagePromptDefaults.OpenAiImageModel,
            Notes = item.Notes,
            SourceType = item.SourceType,
            Heading = item.Heading,
            Order = item.Order,
        }, JsonOptions);

    public static ImagePromptSectionContent ToSectionContent(GeneratedContent row)
    {
        var settings = Deserialize(row.MetaDescription);
        return new ImagePromptSectionContent(
            settings.SourceType ?? InferSourceType(row),
            settings.Heading ?? row.Title,
            settings.Order,
            row.BodyHtml,
            settings.Width,
            settings.Height,
            settings.ResolveImageModel(),
            settings.Notes);
    }

    private static string InferSourceType(GeneratedContent row) =>
        row.Slug.Contains("-blog-h2-", StringComparison.OrdinalIgnoreCase) ? "blog" : "pillar";

    private static StoredImagePromptSettings Deserialize(string? metaJson)
    {
        if (string.IsNullOrWhiteSpace(metaJson))
        {
            return DefaultSettings(notes: null);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<StoredImagePromptSettings>(metaJson, JsonOptions);
            return parsed ?? DefaultSettings(notes: null);
        }
        catch (JsonException)
        {
            return DefaultSettings(notes: metaJson);
        }
    }

    private static StoredImagePromptSettings DefaultSettings(string? notes) => new()
    {
        Width = ImagePromptDefaults.PillarWidth,
        Height = ImagePromptDefaults.PillarHeight,
        ImageModel = ImagePromptDefaults.OpenAiImageModel,
        Notes = notes,
        Order = 0,
    };

    private sealed class StoredImagePromptSettings
    {
        public int Width { get; set; } = ImagePromptDefaults.PillarWidth;
        public int Height { get; set; } = ImagePromptDefaults.PillarHeight;
        public string? ImageModel { get; set; }
        public string? LeonardoModel { get; set; }
        public string? Notes { get; set; }
        public string? SourceType { get; set; }
        public string? Heading { get; set; }
        public int Order { get; set; }

        public string ResolveImageModel()
        {
            if (!string.IsNullOrWhiteSpace(ImageModel))
            {
                return ImageModel.Trim();
            }

            // Legacy rows stored Leonardo model names — generation always uses OpenAI now.
            return ImagePromptDefaults.OpenAiImageModel;
        }
    }
}

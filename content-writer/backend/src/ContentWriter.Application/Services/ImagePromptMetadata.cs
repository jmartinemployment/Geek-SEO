using System.Text.Json;
using System.Text.Json.Serialization;
using ContentWriter.Application.DTOs;

namespace ContentWriter.Application.Services;

public static class ImagePromptMetadata
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(ImagePromptItemDraft item) =>
        JsonSerializer.Serialize(new StoredImagePromptSettings(
            item.Width,
            item.Height,
            item.LeonardoModel,
            ResolveModelId(item.LeonardoModel),
            item.StylePreset,
            item.Alchemy,
            item.PhotoReal,
            item.Notes), JsonOptions);

    public static ImagePromptContent ToContent(string useCase, string prompt, string? metaJson)
    {
        var settings = Deserialize(metaJson);
        return new ImagePromptContent(
            useCase,
            prompt,
            settings.Width,
            settings.Height,
            settings.LeonardoModel,
            settings.LeonardoModelId,
            settings.StylePreset,
            settings.Alchemy,
            settings.PhotoReal,
            settings.Notes);
    }

    private static StoredImagePromptSettings Deserialize(string? metaJson)
    {
        if (string.IsNullOrWhiteSpace(metaJson))
        {
            return new StoredImagePromptSettings(
                ImagePromptDefaults.PillarWidth,
                ImagePromptDefaults.PillarHeight,
                ImagePromptDefaults.LeonardoPhoenixModel,
                ImagePromptDefaults.LeonardoPhoenixModelId,
                ImagePromptDefaults.PillarStylePreset,
                Alchemy: true,
                PhotoReal: false,
                Notes: null);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<StoredImagePromptSettings>(metaJson, JsonOptions);
            if (parsed is null)
                throw new JsonException("Null settings.");

            return parsed with
            {
                LeonardoModelId = string.IsNullOrWhiteSpace(parsed.LeonardoModelId)
                    ? ResolveModelId(parsed.LeonardoModel)
                    : parsed.LeonardoModelId,
            };
        }
        catch (JsonException)
        {
            return new StoredImagePromptSettings(
                ImagePromptDefaults.PillarWidth,
                ImagePromptDefaults.PillarHeight,
                ImagePromptDefaults.LeonardoPhoenixModel,
                ImagePromptDefaults.LeonardoPhoenixModelId,
                ImagePromptDefaults.PillarStylePreset,
                Alchemy: true,
                PhotoReal: false,
                Notes: metaJson);
        }
    }

    private static string ResolveModelId(string modelName) =>
        modelName.Contains("Phoenix", StringComparison.OrdinalIgnoreCase)
            ? ImagePromptDefaults.LeonardoPhoenixModelId
            : ImagePromptDefaults.LeonardoPhoenixModelId;

    private sealed record StoredImagePromptSettings(
        int Width,
        int Height,
        string LeonardoModel,
        string LeonardoModelId,
        string StylePreset,
        bool Alchemy,
        bool PhotoReal,
        string? Notes);
}

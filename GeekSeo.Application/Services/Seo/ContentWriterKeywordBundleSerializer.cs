using System.Text.Json;
using System.Text.Json.Serialization;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ContentWriterKeywordBundleSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string Serialize(ContentWriterSerpExport export) =>
        JsonSerializer.Serialize(export, Options);

    public static ContentWriterSerpExport? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ContentWriterSerpExport>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

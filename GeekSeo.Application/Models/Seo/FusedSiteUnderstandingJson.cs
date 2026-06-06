using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeekSeo.Application.Models.Seo;

public static class FusedSiteUnderstandingJson
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(FusedSiteUnderstanding fused) =>
        JsonSerializer.Serialize(fused, Json);

    public static FusedSiteUnderstanding? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
            return null;

        try
        {
            return JsonSerializer.Deserialize<FusedSiteUnderstanding>(json, Json);
        }
        catch
        {
            return null;
        }
    }
}

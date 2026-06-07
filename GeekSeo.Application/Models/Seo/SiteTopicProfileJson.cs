using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeekSeo.Application.Models.Seo;

public static class SiteTopicProfileJson
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(SiteTopicProfile profile) =>
        JsonSerializer.Serialize(profile, Json);

    public static SiteTopicProfile? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
            return null;

        try
        {
            return JsonSerializer.Deserialize<SiteTopicProfile>(json, Json);
        }
        catch
        {
            return null;
        }
    }
}

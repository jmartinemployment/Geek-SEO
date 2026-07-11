using System.Text.Json;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ContentLinkPlanJson
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public static ContentLinkPlan Empty { get; } = new();

    public static ContentLinkPlan Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Empty;

        try
        {
            return JsonSerializer.Deserialize<ContentLinkPlan>(json, JsonOptions) ?? Empty;
        }
        catch (JsonException)
        {
            return Empty;
        }
    }

    public static string Serialize(ContentLinkPlan plan) =>
        JsonSerializer.Serialize(plan ?? Empty, JsonOptions);
}

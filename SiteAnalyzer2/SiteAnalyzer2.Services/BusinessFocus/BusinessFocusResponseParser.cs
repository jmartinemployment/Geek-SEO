using System.Text.Json;

namespace SiteAnalyzer2.Services.BusinessFocus;

internal static class BusinessFocusResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static BusinessFocusClassificationResult Parse(string content)
    {
        var parsed = JsonSerializer.Deserialize<ClassifierJsonResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Business focus AI response was not valid JSON.");

        return new BusinessFocusClassificationResult(
            parsed.BusinessType ?? "Organization",
            parsed.PrimaryServices ?? [],
            parsed.ServiceArea,
            parsed.Description ?? string.Empty,
            parsed.GeneratedSchemaJson ?? "{}",
            parsed.HasExistingSchema,
            parsed.ExistingSchemaMatches);
    }

    private sealed class ClassifierJsonResponse
    {
        public string? BusinessType { get; set; }
        public List<string>? PrimaryServices { get; set; }
        public string? ServiceArea { get; set; }
        public string? Description { get; set; }
        public string? GeneratedSchemaJson { get; set; }
        public bool HasExistingSchema { get; set; }
        public bool? ExistingSchemaMatches { get; set; }
    }
}

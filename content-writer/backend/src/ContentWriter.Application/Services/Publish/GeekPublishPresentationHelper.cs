using System.Text.Json;
using ContentWriter.Application.Services.Figures;

namespace ContentWriter.Application.Services.Publish;

internal static class GeekPublishPresentationHelper
{
    public static string StripMergedFigures(string body) => MergedFigureMarkup.Strip(body);

    public static string? ExtractSchemaDescription(string schemaMetadataJson)
    {
        if (string.IsNullOrWhiteSpace(schemaMetadataJson) || schemaMetadataJson.Trim() == "{}")
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(schemaMetadataJson);
            if (document.RootElement.TryGetProperty("description", out var description))
            {
                var text = description.GetString()?.Trim();
                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    public static (
        string? BlogExcerpt,
        string? TechnicalArticleExcerpt,
        string? ToolExcerpt,
        string? AdvertisingExcerpt) ExcerptsForSlug(
        string apiSlug,
        string? listingExcerpt,
        string? advertisingExcerpt = null)
    {
        var excerpt = string.IsNullOrWhiteSpace(listingExcerpt) ? null : listingExcerpt.Trim();
        var advertising = string.IsNullOrWhiteSpace(advertisingExcerpt) ? null : advertisingExcerpt.Trim();
        var normalized = apiSlug.Trim().Trim('/');

        if (normalized.StartsWith("tools/", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null, excerpt, advertising);
        }

        if (normalized.StartsWith("use-cases/", StringComparison.OrdinalIgnoreCase))
        {
            return (null, excerpt, null, null);
        }

        if (normalized.StartsWith("blog/", StringComparison.OrdinalIgnoreCase))
        {
            return (excerpt, null, null, null);
        }

        return (excerpt, null, null, null);
    }
}

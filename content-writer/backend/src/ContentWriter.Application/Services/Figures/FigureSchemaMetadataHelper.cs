using System.Text.Json.Nodes;

namespace ContentWriter.Application.Services.Figures;

public static class FigureSchemaMetadataHelper
{
    public static string WithHeroImage(string schemaMetadataJson, string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("Hero image URL is required.", nameof(imageUrl));
        }

        JsonObject root;
        if (string.IsNullOrWhiteSpace(schemaMetadataJson) || schemaMetadataJson.Trim() == "{}")
        {
            root = new JsonObject();
        }
        else
        {
            root = JsonNode.Parse(schemaMetadataJson) as JsonObject
                   ?? throw new InvalidOperationException("schema_metadata must be a JSON object.");
        }

        root["image"] = imageUrl;
        return root.ToJsonString();
    }
}

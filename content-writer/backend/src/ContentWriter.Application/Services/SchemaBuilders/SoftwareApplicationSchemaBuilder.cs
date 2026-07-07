using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContentWriter.Application.Services.SchemaBuilders;

public sealed record SoftwareApplicationDescriptor(string Name, string? Description);

public interface ISoftwareApplicationSchemaBuilder
{
    IReadOnlyList<Dictionary<string, object?>> BuildNodes(IReadOnlyList<SoftwareApplicationDescriptor> applications);
    string BuildGraph(IReadOnlyList<SoftwareApplicationDescriptor> applications);
}

public class SoftwareApplicationSchemaBuilder : ISoftwareApplicationSchemaBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public IReadOnlyList<Dictionary<string, object?>> BuildNodes(IReadOnlyList<SoftwareApplicationDescriptor> applications)
    {
        return applications
            .Where(app => !string.IsNullOrWhiteSpace(app.Name))
            .Select(BuildNode)
            .ToList();
    }

    public string BuildGraph(IReadOnlyList<SoftwareApplicationDescriptor> applications)
    {
        var nodes = BuildNodes(applications);
        if (nodes.Count == 0)
        {
            return string.Empty;
        }

        var graph = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = nodes
        };

        return JsonSerializer.Serialize(graph, JsonOptions);
    }

    private static Dictionary<string, object?> BuildNode(SoftwareApplicationDescriptor application)
    {
        var node = new Dictionary<string, object?>
        {
            ["@type"] = "SoftwareApplication",
            ["name"] = application.Name.Trim(),
            ["applicationCategory"] = "BusinessApplication",
            ["operatingSystem"] = "Web"
        };

        if (!string.IsNullOrWhiteSpace(application.Description))
        {
            node["description"] = application.Description.Trim();
        }

        return node;
    }
}

using System.Text.Json;

namespace GeekSeo.Application.Infrastructure;

public static class AiDetectionResponseParser
{
    public static bool TryParse(string raw, out double aiProbability, out string summary)
    {
        aiProbability = 0;
        summary = string.Empty;

        var json = ExtractJsonObject(raw);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!TryGetProbability(root, out aiProbability))
                return false;

            summary = TryGetSummary(root) ?? string.Empty;
            aiProbability = Math.Clamp(aiProbability, 0, 1);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ExtractJsonObject(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var lines = trimmed.Split('\n');
            trimmed = string.Join(
                '\n',
                lines.Skip(1).TakeWhile(l => !l.TrimStart().StartsWith("```", StringComparison.Ordinal)));
            trimmed = trimmed.Trim();
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;

        return trimmed[start..(end + 1)];
    }

    private static bool TryGetProbability(JsonElement root, out double probability)
    {
        foreach (var name in new[] { "aiProbability", "ai_probability", "probability", "score" })
        {
            if (!root.TryGetProperty(name, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out probability))
                return true;

            if (value.ValueKind == JsonValueKind.String
                && double.TryParse(value.GetString(), out probability))
            {
                return true;
            }
        }

        probability = 0;
        return false;
    }

    private static string? TryGetSummary(JsonElement root)
    {
        foreach (var name in new[] { "summary", "reason", "explanation", "message" })
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        return null;
    }
}

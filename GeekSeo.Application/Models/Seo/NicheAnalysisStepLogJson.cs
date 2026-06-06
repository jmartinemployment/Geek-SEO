using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeekSeo.Application.Models.Seo;

public static class NicheAnalysisStepLogJson
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static IReadOnlyList<NicheAnalysisStepLogEntry> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<NicheAnalysisStepLogEntry>>(json, Json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static string Append(string? existingJson, NicheAnalysisStepLogEntry entry)
    {
        var list = Parse(existingJson).ToList();
        var index = list.FindIndex(s => s.StepNumber == entry.StepNumber);
        if (index >= 0)
            list[index] = entry;
        else
            list.Add(entry);

        list.Sort((a, b) => a.StepNumber.CompareTo(b.StepNumber));
        return JsonSerializer.Serialize(list, Json);
    }
}

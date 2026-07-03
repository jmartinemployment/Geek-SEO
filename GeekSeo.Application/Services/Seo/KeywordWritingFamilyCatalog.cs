using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeekSeo.Application.Services.Seo;

public static class KeywordWritingFamilyCatalog
{
    private static readonly Lazy<CatalogRoot> Catalog = new(LoadCatalog);

    public static string DetectFamilyId(string keyword, IEnumerable<string> additionalTerms)
    {
        var haystack = BuildHaystack(keyword, additionalTerms);
        foreach (var family in Catalog.Value.Families.Where(f => !f.IsDefault))
        {
            if (family.MatchTerms.Any(term => haystack.Contains(term, StringComparison.OrdinalIgnoreCase)))
                return family.Id;
        }

        return Catalog.Value.Families.First(f => f.IsDefault).Id;
    }

    public static KeywordWritingFamilyDefinition GetFamily(string familyId) =>
        Catalog.Value.Families.FirstOrDefault(f =>
            string.Equals(f.Id, familyId, StringComparison.OrdinalIgnoreCase))
        ?? Catalog.Value.Families.First(f => f.IsDefault);

    public static IReadOnlyList<KeywordWritingFamilyDefinition> AllFamilies() => Catalog.Value.Families;

    private static string BuildHaystack(string keyword, IEnumerable<string> additionalTerms) =>
        string.Join(' ',
            new[] { keyword }
                .Concat(additionalTerms)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim()));

    private static CatalogRoot LoadCatalog()
    {
        var assembly = typeof(KeywordWritingFamilyCatalog).Assembly;
        const string resourceName = "GeekSeo.Application.Data.keyword-writing-families.json";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource {resourceName}.");

        var root = JsonSerializer.Deserialize<CatalogRoot>(stream, JsonOptions)
            ?? throw new InvalidOperationException("keyword-writing-families.json is empty.");

        if (!root.Families.Any(f => f.IsDefault))
            throw new InvalidOperationException("keyword-writing-families.json must include one default family.");

        return root;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private sealed class CatalogRoot
    {
        [JsonPropertyName("families")]
        public List<KeywordWritingFamilyDefinition> Families { get; init; } = [];
    }
}

public sealed class KeywordWritingFamilyDefinition
{
    public string Id { get; init; } = "general";
    public bool IsDefault { get; init; }
    public List<string> MatchTerms { get; init; } = [];
    public List<string> ToolExamples { get; init; } = [];
    public List<string> CapabilityPhrases { get; init; } = [];
    public string? WritingRecommendation { get; init; }
}

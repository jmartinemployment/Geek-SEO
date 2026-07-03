using System.Text.Json;
using System.Text.Json.Nodes;
using SiteAnalyzer2.Domain.Entities;

namespace SiteAnalyzer2.Services.ProfileAssembly;

public sealed record RecommendedJsonLdSnippet(
    string Id,
    string Title,
    string Description,
    string Json,
    string ScriptTag);

public static class HomepageJsonLdRecommendationBuilder
{
    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

    private static readonly string[] BusinessSchemaTypes =
    [
        "LocalBusiness", "ProfessionalService", "Organization", "Corporation",
        "Store", "Restaurant", "MedicalBusiness", "LegalService",
    ];

    public static IReadOnlyList<RecommendedJsonLdSnippet> Build(SiteProfile profile)
    {
        if (profile.BusinessProfileAt is null
            && string.IsNullOrWhiteSpace(profile.BusinessType)
            && string.IsNullOrWhiteSpace(profile.BusinessSummary)
            && string.IsNullOrWhiteSpace(profile.BusinessDescription)
            && string.IsNullOrWhiteSpace(profile.HomepageBusinessSchemaJson))
        {
            return [];
        }

        var siteUrl = NormalizeSiteUrl(profile.SiteUrl);
        var siteRoot = siteUrl.TrimEnd('/');
        var businessId = $"{siteRoot}/#business";
        var websiteId = $"{siteRoot}/#website";
        var businessName = FirstNonEmpty(profile.PrimaryNiche, profile.DisplayName, HostnameFromUrl(siteUrl));

        var crawledBusiness = TryNormalizeCrawledBusinessBlock(
            profile.HomepageBusinessSchemaJson,
            siteUrl,
            businessId);
        var schemaType = crawledBusiness?.SchemaType ?? ResolveSchemaType(profile.BusinessType);

        string businessJson;
        if (crawledBusiness is not null)
        {
            businessJson = crawledBusiness.Json;
        }
        else
        {
            businessJson = BuildSynthesizedBusinessJson(profile, siteUrl, businessId, schemaType, businessName);
        }

        var crawledWebsite = TryExtractWebsiteBlock(profile.HomepageBusinessSchemaJson, siteUrl, websiteId, businessId);
        var websiteJson = crawledWebsite ?? BuildSynthesizedWebsiteJson(siteUrl, websiteId, businessId, businessName);

        return
        [
            new RecommendedJsonLdSnippet(
                "business-entity",
                $"Block 1 — {schemaType} (homepage)",
                crawledBusiness is not null
                    ? "Imported from your homepage JSON-LD. Keep on the homepage; do not merge article schema into this block."
                    : "Site-wide business entity. Keep on the homepage; do not merge article schema into this block.",
                businessJson,
                WrapScriptTag(businessJson)),
            new RecommendedJsonLdSnippet(
                "website",
                "Block 2 — WebSite (homepage)",
                "Identifies the site and links it to the business entity via publisher @id.",
                websiteJson,
                WrapScriptTag(websiteJson)),
        ];
    }

    private sealed record NormalizedBusinessBlock(string Json, string SchemaType);

    private static NormalizedBusinessBlock? TryNormalizeCrawledBusinessBlock(
        string? rawHomepageJson,
        string siteUrl,
        string businessId)
    {
        if (string.IsNullOrWhiteSpace(rawHomepageJson))
            return null;

        try
        {
            var root = JsonNode.Parse(rawHomepageJson.Trim());
            if (root is null)
                return null;

            var businessNode = SelectBusinessEntityNode(root);
            if (businessNode is null)
                return null;

            if (businessNode is JsonObject businessObject)
            {
                EnsureContextAndBusinessId(businessObject, siteUrl, businessId);
                var schemaType = ReadPrimaryType(businessObject) ?? "ProfessionalService";
                return new NormalizedBusinessBlock(
                    businessObject.ToJsonString(PrettyJson),
                    schemaType);
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryExtractWebsiteBlock(
        string? rawHomepageJson,
        string siteUrl,
        string websiteId,
        string businessId)
    {
        if (string.IsNullOrWhiteSpace(rawHomepageJson))
            return null;

        try
        {
            var root = JsonNode.Parse(rawHomepageJson.Trim());
            var websiteNode = FindNodeByType(root, "WebSite");
            if (websiteNode is not JsonObject websiteObject)
                return null;

            websiteObject["@context"] ??= "https://schema.org";
            websiteObject["@id"] ??= websiteId;
            websiteObject["url"] ??= siteUrl;
            if (websiteObject["publisher"] is null)
            {
                websiteObject["publisher"] = new JsonObject { ["@id"] = businessId };
            }

            return websiteObject.ToJsonString(PrettyJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonNode? SelectBusinessEntityNode(JsonNode root)
    {
        if (root is JsonObject obj)
        {
            if (obj.ContainsKey("@graph") && obj["@graph"] is JsonArray graph)
            {
                JsonObject? best = null;
                var bestScore = -1;
                foreach (var item in graph)
                {
                    if (item is not JsonObject entity)
                        continue;

                    var score = ScoreBusinessNode(entity);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = entity;
                    }
                }

                return bestScore > 0 ? best : null;
            }

            return ScoreBusinessNode(obj) > 0 ? obj : null;
        }

        if (root is JsonArray array)
        {
            JsonObject? best = null;
            var bestScore = -1;
            foreach (var item in array)
            {
                if (item is not JsonObject entity)
                    continue;

                var score = ScoreBusinessNode(entity);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = entity;
                }
            }

            return bestScore > 0 ? best : null;
        }

        return null;
    }

    private static JsonObject? FindNodeByType(JsonNode? root, string type)
    {
        if (root is null)
            return null;

        if (root is JsonObject obj)
        {
            if (NodeHasType(obj, type))
                return obj;

            if (obj["@graph"] is JsonArray graph)
            {
                foreach (var item in graph)
                {
                    if (item is JsonObject entity && NodeHasType(entity, type))
                        return entity;
                }
            }

            foreach (var property in obj)
            {
                var nested = FindNodeByType(property.Value, type);
                if (nested is not null)
                    return nested;
            }
        }
        else if (root is JsonArray array)
        {
            foreach (var item in array)
            {
                var nested = FindNodeByType(item, type);
                if (nested is not null)
                    return nested;
            }
        }

        return null;
    }

    private static int ScoreBusinessNode(JsonObject entity)
    {
        var types = ReadTypes(entity);
        if (types.Count == 0)
            return 0;

        var score = 0;
        foreach (var type in types)
        {
            if (BusinessSchemaTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
                score += 10;
            else if (type.Contains("Business", StringComparison.OrdinalIgnoreCase)
                || type.Contains("Organization", StringComparison.OrdinalIgnoreCase)
                || type.Contains("Service", StringComparison.OrdinalIgnoreCase))
                score += 5;
        }

        if (entity.ContainsKey("name"))
            score += 2;
        if (entity.ContainsKey("description"))
            score += 1;

        return score;
    }

    private static bool NodeHasType(JsonObject entity, string type) =>
        ReadTypes(entity).Any(t => t.Equals(type, StringComparison.OrdinalIgnoreCase));

    private static List<string> ReadTypes(JsonObject entity)
    {
        if (!entity.TryGetPropertyValue("@type", out var typeNode) || typeNode is null)
            return [];

        return typeNode switch
        {
            JsonValue value when value.TryGetValue<string>(out var single) => [single],
            JsonArray array => array
                .OfType<JsonValue>()
                .Select(v => v.TryGetValue<string>(out var s) ? s : null)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .ToList(),
            _ => [],
        };
    }

    private static string? ReadPrimaryType(JsonObject entity)
    {
        var types = ReadTypes(entity);
        return types.FirstOrDefault(t => BusinessSchemaTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            ?? types.FirstOrDefault();
    }

    private static void EnsureContextAndBusinessId(JsonObject businessObject, string siteUrl, string businessId)
    {
        businessObject["@context"] ??= "https://schema.org";
        businessObject["@id"] ??= businessId;
        businessObject["url"] ??= siteUrl;
    }

    private static string BuildSynthesizedBusinessJson(
        SiteProfile profile,
        string siteUrl,
        string businessId,
        string schemaType,
        string businessName)
    {
        var description = FirstNonEmpty(profile.BusinessSummary, profile.BusinessDescription);

        var businessNode = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = schemaType,
            ["@id"] = businessId,
            ["name"] = businessName,
            ["url"] = siteUrl,
        };

        if (!string.IsNullOrWhiteSpace(description))
            businessNode["description"] = description.Trim();

        var areaServed = BuildAreaServed(profile.GeoAnchorNodes, profile.ServiceAreaDescription);
        if (areaServed is not null)
            businessNode["areaServed"] = areaServed;

        var knowsAbout = BuildKnowsAbout(profile.NicheTags);
        if (knowsAbout is not null)
            businessNode["knowsAbout"] = knowsAbout;

        var address = BuildPostalAddress(profile.GeoAnchorNodes, profile.ServiceAreaDescription);
        if (address is not null)
            businessNode["address"] = address;

        return businessNode.ToJsonString(PrettyJson);
    }

    private static string BuildSynthesizedWebsiteJson(
        string siteUrl,
        string websiteId,
        string businessId,
        string businessName) =>
        new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["@id"] = websiteId,
            ["url"] = siteUrl,
            ["name"] = businessName,
            ["publisher"] = new JsonObject { ["@id"] = businessId },
        }.ToJsonString(PrettyJson);

    private static string ResolveSchemaType(string? businessType)
    {
        if (string.IsNullOrWhiteSpace(businessType))
            return "ProfessionalService";

        var lower = businessType.ToLowerInvariant();
        if (lower.Contains("local business", StringComparison.Ordinal)
            || lower.Contains("localbusiness", StringComparison.Ordinal))
        {
            return "LocalBusiness";
        }

        if (lower.Contains("professional service", StringComparison.Ordinal)
            || lower.Contains("professionalservice", StringComparison.Ordinal))
        {
            return "ProfessionalService";
        }

        return "ProfessionalService";
    }

    private static JsonArray? BuildAreaServed(
        IReadOnlyList<string> geoAnchorNodes,
        string? serviceAreaDescription)
    {
        var places = new List<JsonObject>();

        foreach (var node in geoAnchorNodes.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            var place = PlaceFromLabel(node.Trim());
            if (place is not null)
                places.Add(place);
        }

        if (places.Count == 0 && !string.IsNullOrWhiteSpace(serviceAreaDescription))
        {
            foreach (var part in serviceAreaDescription.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var place = PlaceFromLabel(part);
                if (place is not null)
                    places.Add(place);
            }
        }

        return places.Count == 0 ? null : new JsonArray(places.Select(p => (JsonNode)p).ToArray());
    }

    private static JsonObject? PlaceFromLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return null;

        var trimmed = label.Trim();
        var parenIndex = trimmed.IndexOf('(');
        if (parenIndex > 0 && trimmed.EndsWith(')'))
        {
            var name = trimmed[..parenIndex].Trim();
            var region = trimmed[(parenIndex + 1)..^1].Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                return new JsonObject
                {
                    ["@type"] = name.Contains("County", StringComparison.OrdinalIgnoreCase) ? "AdministrativeArea" : "Place",
                    ["name"] = name,
                    ["containedInPlace"] = region,
                };
            }
        }

        if (trimmed.Contains("County", StringComparison.OrdinalIgnoreCase))
        {
            return new JsonObject
            {
                ["@type"] = "AdministrativeArea",
                ["name"] = trimmed,
            };
        }

        return new JsonObject
        {
            ["@type"] = "Place",
            ["name"] = trimmed,
        };
    }

    private static JsonArray? BuildKnowsAbout(IReadOnlyList<string> nicheTags)
    {
        var topics = nicheTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .Select(tag => JsonValue.Create(tag))
            .Cast<JsonNode>()
            .ToArray();

        return topics.Length == 0 ? null : new JsonArray(topics);
    }

    private static JsonObject? BuildPostalAddress(
        IReadOnlyList<string> geoAnchorNodes,
        string? serviceAreaDescription)
    {
        var region = geoAnchorNodes.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))?.Trim();
        if (string.IsNullOrWhiteSpace(region) && !string.IsNullOrWhiteSpace(serviceAreaDescription))
        {
            var first = serviceAreaDescription.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            region = first;
        }

        if (string.IsNullOrWhiteSpace(region))
            return null;

        var address = new JsonObject
        {
            ["@type"] = "PostalAddress",
            ["addressCountry"] = "US",
        };

        if (region.Length is 2 && region.All(char.IsLetter))
            address["addressRegion"] = region.ToUpperInvariant();
        else
            address["addressLocality"] = region;

        return address;
    }

    private static string NormalizeSiteUrl(string siteUrl)
    {
        var trimmed = siteUrl.Trim();
        return trimmed.EndsWith('/') ? trimmed : $"{trimmed}/";
    }

    private static string HostnameFromUrl(string siteUrl)
    {
        try
        {
            var host = new Uri(siteUrl).Host.ToLowerInvariant();
            return host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host;
        }
        catch (UriFormatException)
        {
            return siteUrl;
        }
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string WrapScriptTag(string json) =>
        $"<script type=\"application/ld+json\">\n{json}\n</script>";
}

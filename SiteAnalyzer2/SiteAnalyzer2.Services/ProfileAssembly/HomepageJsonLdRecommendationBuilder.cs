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

    public static IReadOnlyList<RecommendedJsonLdSnippet> Build(SiteProfile profile)
    {
        if (profile.BusinessProfileAt is null
            && string.IsNullOrWhiteSpace(profile.BusinessType)
            && string.IsNullOrWhiteSpace(profile.BusinessSummary)
            && string.IsNullOrWhiteSpace(profile.BusinessDescription))
        {
            return [];
        }

        var siteUrl = NormalizeSiteUrl(profile.SiteUrl);
        var siteRoot = siteUrl.TrimEnd('/');
        var businessId = $"{siteRoot}/#business";
        var websiteId = $"{siteRoot}/#website";
        var schemaType = ResolveSchemaType(profile.BusinessType);
        var businessName = FirstNonEmpty(profile.PrimaryNiche, profile.DisplayName, HostnameFromUrl(siteUrl));
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

        var websiteNode = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["@id"] = websiteId,
            ["url"] = siteUrl,
            ["name"] = businessName,
            ["publisher"] = new JsonObject { ["@id"] = businessId },
        };

        var businessJson = businessNode.ToJsonString(PrettyJson);
        var websiteJson = websiteNode.ToJsonString(PrettyJson);

        return
        [
            new RecommendedJsonLdSnippet(
                "business-entity",
                $"Block 1 — {schemaType} (homepage)",
                "Site-wide business entity. Keep on the homepage; do not merge article schema into this block.",
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

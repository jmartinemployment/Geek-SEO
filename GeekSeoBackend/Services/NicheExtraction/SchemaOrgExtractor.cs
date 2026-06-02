using System.Text.Json;
using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

public sealed class SchemaOrgExtractor(IHttpClientFactory factory, ILogger<SchemaOrgExtractor> logger)
{
    private static readonly HashSet<string> BusinessTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "LocalBusiness", "Service", "ProfessionalService", "Organization",
        "AutoRepair", "HealthAndBeautyBusiness", "HomeAndConstructionBusiness",
        "LegalService", "MedicalBusiness", "Store", "FoodEstablishment",
        "TechCompany", "ITConsultant", "ComputerRepairService",
    };

    public async Task<SchemaOrgData> ExtractAsync(string siteUrl, CancellationToken ct)
    {
        try
        {
            var html = await FetchHomePageAsync(siteUrl, ct);
            if (string.IsNullOrWhiteSpace(html)) return Empty();

            var blocks = ExtractJsonLdBlocks(html);
            var serviceNames = new List<string>();
            string? description = null;
            string? brand = null;
            var areas = new List<string>();

            foreach (var block in blocks)
            {
                try
                {
                    using var doc = JsonDocument.Parse(block);
                    var root = doc.RootElement;

                    if (!IsBusinessType(root)) continue;

                    description ??= TryGetString(root, "description");
                    brand ??= CleanBrandName(TryGetString(root, "name"));

                    ExtractAreaServed(root, areas);
                    ExtractServiceNames(root, serviceNames);
                }
                catch (JsonException)
                {
                    // malformed block — skip
                }
            }

            return new SchemaOrgData(
                serviceNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                description,
                brand,
                areas.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema.org extraction failed for {Url}", siteUrl);
            return Empty();
        }
    }

    private async Task<string> FetchHomePageAsync(string siteUrl, CancellationToken ct)
    {
        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (compatible; GeekSEO/1.0; +https://seo.geekatyourspot.com)");
        var response = await client.GetAsync(siteUrl, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private static IEnumerable<string> ExtractJsonLdBlocks(string html)
    {
        var results = new List<string>();
        var searchFrom = 0;
        const string openTag = "<script type=\"application/ld+json\">";
        const string closeTag = "</script>";

        while (true)
        {
            var start = html.IndexOf(openTag, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (start < 0) break;
            start += openTag.Length;
            var end = html.IndexOf(closeTag, start, StringComparison.OrdinalIgnoreCase);
            if (end < 0) break;
            results.Add(html[start..end].Trim());
            searchFrom = end + closeTag.Length;
        }

        return results;
    }

    private static bool IsBusinessType(JsonElement root)
    {
        if (root.TryGetProperty("@type", out var typeProp))
        {
            if (typeProp.ValueKind == JsonValueKind.String)
                return BusinessTypes.Contains(typeProp.GetString() ?? string.Empty);

            if (typeProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var t in typeProp.EnumerateArray())
                {
                    if (t.ValueKind == JsonValueKind.String && BusinessTypes.Contains(t.GetString() ?? string.Empty))
                        return true;
                }
            }
        }
        return false;
    }

    private static void ExtractServiceNames(JsonElement root, List<string> names)
    {
        // hasOfferCatalog.itemListElement[].itemOffered.name
        if (root.TryGetProperty("hasOfferCatalog", out var catalog))
        {
            if (catalog.TryGetProperty("itemListElement", out var items) &&
                items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("itemOffered", out var offered))
                        AddStringValue(offered, "name", names);
                    else
                        AddStringValue(item, "name", names);
                }
            }
        }

        // makesOffer[].itemOffered.name
        if (root.TryGetProperty("makesOffer", out var offers) &&
            offers.ValueKind == JsonValueKind.Array)
        {
            foreach (var offer in offers.EnumerateArray())
            {
                if (offer.TryGetProperty("itemOffered", out var offered))
                    AddStringValue(offered, "name", names);
            }
        }

        // serviceType (string or array)
        if (root.TryGetProperty("serviceType", out var serviceType))
        {
            if (serviceType.ValueKind == JsonValueKind.String)
                names.Add(serviceType.GetString()!);
            else if (serviceType.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in serviceType.EnumerateArray())
                {
                    if (s.ValueKind == JsonValueKind.String)
                        names.Add(s.GetString()!);
                }
            }
        }
    }

    private static void ExtractAreaServed(JsonElement root, List<string> areas)
    {
        if (!root.TryGetProperty("areaServed", out var area)) return;

        if (area.ValueKind == JsonValueKind.String)
        {
            areas.Add(area.GetString()!);
            return;
        }
        if (area.ValueKind == JsonValueKind.Array)
        {
            foreach (var a in area.EnumerateArray())
            {
                if (a.ValueKind == JsonValueKind.String)
                    areas.Add(a.GetString()!);
                else
                    AddStringValue(a, "name", areas);
            }
        }
        else
        {
            AddStringValue(area, "name", areas);
        }
    }

    private static void AddStringValue(JsonElement element, string property, List<string> target)
    {
        if (element.TryGetProperty(property, out var prop) &&
            prop.ValueKind == JsonValueKind.String)
        {
            var val = prop.GetString();
            if (!string.IsNullOrWhiteSpace(val))
                target.Add(val);
        }
    }

    private static string? TryGetString(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var prop) &&
            prop.ValueKind == JsonValueKind.String)
        {
            var val = prop.GetString();
            return string.IsNullOrWhiteSpace(val) ? null : val;
        }
        return null;
    }

    private static string? CleanBrandName(string? name)
    {
        if (name is null) return null;
        var suffixes = new[] { " LLC", " Inc", " Inc.", " Corp", " Co.", " Ltd", " Ltd." };
        foreach (var s in suffixes)
        {
            if (name.EndsWith(s, StringComparison.OrdinalIgnoreCase))
                return name[..^s.Length].Trim();
        }
        return name;
    }

    private static SchemaOrgData Empty() => new([], null, null, []);
}

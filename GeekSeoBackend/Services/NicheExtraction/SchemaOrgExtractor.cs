using System.Text.Json;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.NicheExtraction;

public sealed partial class SchemaOrgExtractor(IHttpClientFactory factory, ILogger<SchemaOrgExtractor> logger)
{
    private static readonly HashSet<string> BusinessTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "LocalBusiness", "Service", "ProfessionalService", "Organization",
        "AutoRepair", "HealthAndBeautyBusiness", "HomeAndConstructionBusiness",
        "LegalService", "MedicalBusiness", "Store", "FoodEstablishment",
        "TechCompany", "ITConsultant", "ComputerRepairService",
    };

    public async Task<SchemaOrgData> ExtractAsync(
        string siteUrl, IBrowser? browser, CancellationToken ct)
    {
        try
        {
            var blocks = new List<string>();

            if (browser is not null)
            {
                try
                {
                    blocks.AddRange(await ExtractJsonLdWithPlaywrightAsync(siteUrl, browser, ct));
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Playwright schema.org extraction failed for {Url}, falling back to HTTP",
                        siteUrl);
                }
            }

            if (blocks.Count == 0)
            {
                var html = await FetchHomePageAsync(siteUrl, ct);
                if (!string.IsNullOrWhiteSpace(html))
                    blocks.AddRange(ExtractJsonLdBlocks(html));
            }

            var serviceNames = new List<string>();
            var knowsAboutTopics = new List<string>();
            var offerCatalogTopics = new List<string>();
            string? description = null;
            string? brand = null;
            var areas = new List<string>();

            foreach (var block in blocks)
            {
                try
                {
                    using var doc = JsonDocument.Parse(block);
                    ProcessJsonLdNode(
                        doc.RootElement,
                        serviceNames,
                        knowsAboutTopics,
                        offerCatalogTopics,
                        areas,
                        ref description,
                        ref brand);
                }
                catch (JsonException)
                {
                    // malformed block — skip
                }
            }

            var data = new SchemaOrgData(
                serviceNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                knowsAboutTopics.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                offerCatalogTopics.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                description,
                brand,
                areas.Distinct(StringComparer.OrdinalIgnoreCase).ToList());

            logger.LogInformation(
                "Schema.org for {Url}: {TopicCount} topics, brand={HasBrand}, areas={AreaCount}, blocks={BlockCount}",
                siteUrl,
                data.ServiceNames.Count,
                !string.IsNullOrWhiteSpace(data.BrandName),
                data.AreaServed.Count,
                blocks.Count);

            return data;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Schema.org extraction failed for {Url}", siteUrl);
            return Empty();
        }
    }

    private static async Task<IReadOnlyList<string>> ExtractJsonLdWithPlaywrightAsync(
        string siteUrl, IBrowser browser, CancellationToken ct)
    {
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (compatible; GeekSEO/1.0; +https://seo.geekatyourspot.com)",
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(siteUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 20_000,
        });

        var payload = await page.EvaluateAsync<string>(@"() => {
            const scripts = Array.from(
                document.querySelectorAll('script[type=""application/ld+json""]'));
            const blocks = scripts
                .map(s => (s.textContent || '').trim())
                .filter(t => t.length > 0);
            return JSON.stringify(blocks);
        }");

        using var doc = JsonDocument.Parse(payload);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return [];

        var results = new List<string>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var text = item.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    results.Add(text);
            }
        }

        return results;
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

        foreach (Match match in JsonLdScriptRegex().Matches(html))
        {
            var body = match.Groups["body"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(body))
                results.Add(body);
        }

        // Legacy exact tag (in case regex misses an edge case)
        if (results.Count == 0)
        {
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
        }

        return results;
    }

    private static void ProcessJsonLdNode(
        JsonElement node,
        List<string> serviceNames,
        List<string> knowsAboutTopics,
        List<string> offerCatalogTopics,
        List<string> areas,
        ref string? description,
        ref string? brand)
    {
        if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
                ProcessJsonLdNode(
                    item, serviceNames, knowsAboutTopics, offerCatalogTopics, areas, ref description, ref brand);
            return;
        }

        if (node.ValueKind != JsonValueKind.Object)
            return;

        if (node.TryGetProperty("@graph", out var graph) && graph.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in graph.EnumerateArray())
                ProcessJsonLdNode(
                    item, serviceNames, knowsAboutTopics, offerCatalogTopics, areas, ref description, ref brand);
        }

        if (IsSchemaPillarSource(node) || HasServiceSignals(node))
        {
            description ??= TryGetString(node, "description");
            brand ??= CleanBrandName(TryGetString(node, "name"));
            ExtractAreaServed(node, areas);
            ExtractServiceNames(node, offerCatalogTopics, serviceNames);
            ExtractKnowsAbout(node, knowsAboutTopics, serviceNames);
        }
    }

    private static bool HasServiceSignals(JsonElement root) =>
        root.TryGetProperty("knowsAbout", out _) ||
        root.TryGetProperty("hasOfferCatalog", out _) ||
        root.TryGetProperty("makesOffer", out _) ||
        root.TryGetProperty("serviceType", out _);

    private static bool IsSchemaPillarSource(JsonElement root) =>
        IsBusinessType(root) || HasSchemaType(root, "Service");

    private static bool HasSchemaType(JsonElement root, string typeName)
    {
        if (!root.TryGetProperty("@type", out var typeProp))
            return false;

        if (typeProp.ValueKind == JsonValueKind.String)
            return string.Equals(typeProp.GetString(), typeName, StringComparison.OrdinalIgnoreCase);

        if (typeProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in typeProp.EnumerateArray())
            {
                if (t.ValueKind == JsonValueKind.String &&
                    string.Equals(t.GetString(), typeName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static void ExtractKnowsAbout(JsonElement root, List<string> knowsAboutTopics, List<string> serviceNames)
    {
        if (!root.TryGetProperty("knowsAbout", out var knowsAbout))
            return;

        if (knowsAbout.ValueKind == JsonValueKind.String)
        {
            AddName(knowsAbout.GetString(), knowsAboutTopics, serviceNames);
            return;
        }

        if (knowsAbout.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in knowsAbout.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
                AddName(item.GetString(), knowsAboutTopics, serviceNames);
            else
                AddStringValue(item, "name", knowsAboutTopics, serviceNames);
        }
    }

    private static void AddName(string? value, List<string> primary, List<string> serviceNames)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var trimmed = value.Trim();
        primary.Add(trimmed);
        serviceNames.Add(trimmed);
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

    private static void ExtractServiceNames(
        JsonElement root,
        List<string> offerCatalogTopics,
        List<string> serviceNames)
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
                        AddStringValue(offered, "name", offerCatalogTopics, serviceNames);
                    else
                        AddStringValue(item, "name", offerCatalogTopics, serviceNames);
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
                    AddStringValue(offered, "name", offerCatalogTopics, serviceNames);
            }
        }

        // serviceType (string or array)
        if (root.TryGetProperty("serviceType", out var serviceType))
        {
            if (serviceType.ValueKind == JsonValueKind.String)
                AddName(serviceType.GetString(), offerCatalogTopics, serviceNames);
            else if (serviceType.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in serviceType.EnumerateArray())
                {
                    if (s.ValueKind == JsonValueKind.String)
                        AddName(s.GetString(), offerCatalogTopics, serviceNames);
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
                    AddAreaName(a, "name", areas);
            }
        }
        else
        {
            AddAreaName(area, "name", areas);
        }
    }

    private static void AddAreaName(JsonElement element, string property, List<string> areas)
    {
        if (element.TryGetProperty(property, out var prop) &&
            prop.ValueKind == JsonValueKind.String)
        {
            var val = prop.GetString();
            if (!string.IsNullOrWhiteSpace(val))
                areas.Add(val.Trim());
        }
    }

    private static void AddStringValue(
        JsonElement element,
        string property,
        List<string> primary,
        List<string> serviceNames)
    {
        if (element.TryGetProperty(property, out var prop) &&
            prop.ValueKind == JsonValueKind.String)
        {
            AddName(prop.GetString(), primary, serviceNames);
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

    private static SchemaOrgData Empty() => new([], [], [], null, null, []);

    [GeneratedRegex(
        "<script[^>]*type\\s*=\\s*[\"']application/ld\\+json[\"'][^>]*>(?<body>[\\s\\S]*?)</script>",
        RegexOptions.IgnoreCase)]
    private static partial Regex JsonLdScriptRegex();
}

using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Providers.Seo;

public sealed class DataForSEOSerpProvider(IHttpClientFactory httpClientFactory) : ISerpProvider
{
    public string ProviderName => "dataforseo";

    public async Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default)
    {
        if (!DataForSeoClient.TryGetCredentials(out _, out _))
        {
            return Result<SerpResult>.Failure(
                "DATAFORSEO_LOGIN and DATAFORSEO_PASSWORD must be set. Sign up at https://dataforseo.com/");
        }

        var body = new[]
        {
            new
            {
                keyword = request.Keyword,
                location_name = request.Location,
                language_code = request.LanguageCode,
                device = request.Device,
                depth = Math.Clamp(request.ResultCount, 1, 50),
                os = "windows",
            },
        };

        HttpResponseMessage response;
        try
        {
            response = await DataForSeoClient.PostJsonAsync(
                httpClientFactory,
                "/v3/serp/google/organic/live/advanced",
                body,
                ct);
        }
        catch (Exception ex)
        {
            return Result<SerpResult>.Failure($"DataForSEO request failed: {ex.Message}");
        }

        using (response)
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return Result<SerpResult>.Failure($"DataForSEO HTTP {(int)response.StatusCode}: {Truncate(raw)}");

            return ParseResponse(request, raw);
        }
    }

    internal static Result<SerpResult> ParseResponse(SerpRequest request, string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (!DataForSeoClient.IsApiSuccess(root))
                return Result<SerpResult>.Failure(DataForSeoClient.ReadApiErrorMessage(root));

            if (!TryGetSerpItems(root, out var items))
                return Result<SerpResult>.Failure("DataForSEO returned no SERP items for this keyword.");

            var organic = new List<SerpOrganicResult>();
            var paa = new List<PeopleAlsoAskResult>();
            var related = new List<string>();
            string? featuredSnippet = null;
            var features = new SerpFeatures();

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("type", out var typeEl))
                    continue;

                var type = typeEl.GetString() ?? string.Empty;
                switch (type)
                {
                    case "organic":
                        if (!item.TryGetProperty("url", out var urlEl))
                            break;
                        var url = urlEl.GetString();
                        if (string.IsNullOrWhiteSpace(url))
                            break;
                        organic.Add(new SerpOrganicResult
                        {
                            Position = item.TryGetProperty("rank_group", out var rg)
                                ? rg.GetInt32()
                                : organic.Count + 1,
                            Url = url,
                            Title = item.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? string.Empty : string.Empty,
                            Snippet = ReadSnippet(item),
                            Domain = item.TryGetProperty("domain", out var dom) ? dom.GetString() : null,
                        });
                        break;
                    case "people_also_ask":
                        features = features with { HasPeopleAlsoAsk = true };
                        if (item.TryGetProperty("items", out var paaItems)
                            && paaItems.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var paaItem in paaItems.EnumerateArray())
                            {
                                var question = ReadRelatedText(paaItem, "title", "question");
                                if (string.IsNullOrWhiteSpace(question))
                                    continue;
                                paa.Add(new PeopleAlsoAskResult
                                {
                                    Question = question,
                                    Answer = paaItem.TryGetProperty("description", out var ans)
                                        ? ans.GetString()
                                        : null,
                                });
                            }
                        }
                        break;
                    case "featured_snippet":
                        features = features with { HasFeaturedSnippet = true };
                        featuredSnippet = ReadSnippet(item);
                        break;
                    case "local_pack":
                        features = features with { HasLocalPack = true };
                        break;
                    case "images":
                        features = features with { HasImagePack = true };
                        break;
                    case "video":
                        features = features with { HasVideoCarousel = true };
                        break;
                    case "knowledge_graph":
                        features = features with { HasKnowledgePanel = true };
                        break;
                    case "ai_overview":
                        features = features with { HasAiOverview = true };
                        break;
                    case "related_searches":
                        if (item.TryGetProperty("items", out var relItems)
                            && relItems.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var rel in relItems.EnumerateArray())
                            {
                                var title = ReadRelatedText(rel, "title", "query");
                                if (!string.IsNullOrWhiteSpace(title))
                                    related.Add(title);
                            }
                        }
                        break;
                }
            }

            return Result<SerpResult>.Success(new SerpResult
            {
                Keyword = request.Keyword,
                Location = request.Location,
                OrganicResults = organic,
                PeopleAlsoAsk = paa,
                RelatedSearches = related,
                FeaturedSnippetText = featuredSnippet,
                Features = features,
                FetchedAt = DateTimeOffset.UtcNow,
            });
        }
        catch (Exception ex)
        {
            return Result<SerpResult>.Failure($"Failed to parse DataForSEO response: {ex.Message}");
        }
    }

    private static bool TryGetSerpItems(JsonElement root, out JsonElement items)
    {
        items = default;
        if (!root.TryGetProperty("tasks", out var tasks) || tasks.ValueKind != JsonValueKind.Array)
            return false;

        var firstTask = tasks.EnumerateArray().FirstOrDefault();
        if (firstTask.ValueKind == JsonValueKind.Undefined)
            return false;

        if (!firstTask.TryGetProperty("result", out var results) || results.ValueKind != JsonValueKind.Array)
            return false;

        var firstResult = results.EnumerateArray().FirstOrDefault();
        if (firstResult.ValueKind == JsonValueKind.Undefined)
            return false;

        if (!firstResult.TryGetProperty("items", out items) || items.ValueKind != JsonValueKind.Array)
            return false;

        return true;
    }

    private static string ReadSnippet(JsonElement item)
    {
        if (item.TryGetProperty("description", out var description)
            && description.ValueKind == JsonValueKind.String)
        {
            return description.GetString() ?? string.Empty;
        }

        if (item.TryGetProperty("snippet", out var snippet) && snippet.ValueKind == JsonValueKind.String)
            return snippet.GetString() ?? string.Empty;

        return string.Empty;
    }

    private static string? ReadRelatedText(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString();

        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
        }

        return null;
    }

    private static string Truncate(string raw) =>
        raw.Length <= 400 ? raw : raw[..400];
}

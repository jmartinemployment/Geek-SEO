using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Providers.Seo.SerpApi;

public sealed class SerpApiSerpProvider(IHttpClientFactory httpClientFactory) : ISerpProvider
{
    public string ProviderName => "serpapi";

    public async Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default)
    {
        if (!SerpApiClient.TryGetApiKey(out _))
        {
            return Result<SerpResult>.Failure(
                "SERPAPI_API_KEY must be set. Sign up at https://serpapi.com/");
        }

        var query = BuildQuery(request);

        HttpResponseMessage response;
        try
        {
            response = await SerpApiClient.GetSearchAsync(httpClientFactory, query, ct);
        }
        catch (Exception ex)
        {
            return Result<SerpResult>.Failure($"SerpApi request failed: {ex.Message}");
        }

        using (response)
        {
            var body = await SerpApiClient.ReadBodyAsync(response, ct);
            if (!body.IsSuccess)
                return Result<SerpResult>.Failure(body.Error ?? "SerpApi request failed");

            return ParseResponse(request, body.Value!);
        }
    }

    internal static IReadOnlyDictionary<string, string> BuildQuery(SerpRequest request) =>
        new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = request.Keyword,
            ["location"] = request.Location,
            ["hl"] = request.LanguageCode,
            ["gl"] = request.CountryCode.ToLowerInvariant(),
            ["device"] = request.Device,
            ["num"] = Math.Clamp(request.ResultCount, 1, 100).ToString(),
        };

    internal static Result<SerpResult> ParseResponse(SerpRequest request, string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (!SerpApiClient.IsSuccess(root))
                return Result<SerpResult>.Failure(SerpApiClient.ReadErrorMessage(root));

            var organic = ParseOrganicResults(root);
            if (organic.Count == 0)
                return Result<SerpResult>.Failure("SerpApi returned no organic results for this keyword.");

            var paa = ParsePeopleAlsoAsk(root);
            var related = ParseRelatedSearches(root);
            var featuredSnippet = ReadFeaturedSnippet(root);
            var features = BuildFeatures(root, paa.Count > 0, featuredSnippet);

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
            return Result<SerpResult>.Failure($"Failed to parse SerpApi response: {ex.Message}");
        }
    }

    private static List<SerpOrganicResult> ParseOrganicResults(JsonElement root)
    {
        var organic = new List<SerpOrganicResult>();
        if (!root.TryGetProperty("organic_results", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return organic;
        }

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("link", out var linkEl))
                continue;

            var url = linkEl.GetString();
            if (string.IsNullOrWhiteSpace(url))
                continue;

            var position = item.TryGetProperty("position", out var posEl) && posEl.TryGetInt32(out var pos)
                ? pos
                : organic.Count + 1;

            organic.Add(new SerpOrganicResult
            {
                Position = position,
                Url = url,
                Title = item.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? string.Empty : string.Empty,
                Snippet = item.TryGetProperty("snippet", out var snippetEl) ? snippetEl.GetString() ?? string.Empty : string.Empty,
                Domain = item.TryGetProperty("displayed_link", out var displayed)
                    ? displayed.GetString()
                    : SerpApiClient.HostFromUrl(url),
            });
        }

        return organic;
    }

    private static List<PeopleAlsoAskResult> ParsePeopleAlsoAsk(JsonElement root)
    {
        var paa = new List<PeopleAlsoAskResult>();
        if (!root.TryGetProperty("related_questions", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return paa;
        }

        foreach (var item in items.EnumerateArray())
        {
            var question = ReadFirstString(item, "question", "title");
            if (string.IsNullOrWhiteSpace(question))
                continue;

            paa.Add(new PeopleAlsoAskResult
            {
                Question = question,
                Answer = ReadFirstString(item, "snippet", "answer", "description"),
            });
        }

        return paa;
    }

    private static List<string> ParseRelatedSearches(JsonElement root)
    {
        var related = new List<string>();
        if (!root.TryGetProperty("related_searches", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return related;
        }

        foreach (var item in items.EnumerateArray())
        {
            var query = item.ValueKind == JsonValueKind.String
                ? item.GetString()
                : ReadFirstString(item, "query", "title");
            if (!string.IsNullOrWhiteSpace(query))
                related.Add(query);
        }

        return related;
    }

    private static string? ReadFeaturedSnippet(JsonElement root)
    {
        if (root.TryGetProperty("answer_box", out var answerBox))
        {
            var snippet = ReadFirstString(answerBox, "snippet", "answer", "description");
            if (!string.IsNullOrWhiteSpace(snippet))
                return snippet;
        }

        if (root.TryGetProperty("featured_snippet", out var featured))
            return ReadFirstString(featured, "snippet", "description");

        return null;
    }

    private static SerpFeatures BuildFeatures(
        JsonElement root,
        bool hasPaa,
        string? featuredSnippet)
    {
        var hasFeatured = !string.IsNullOrWhiteSpace(featuredSnippet)
            || root.TryGetProperty("answer_box", out _)
            || root.TryGetProperty("featured_snippet", out _);

        return new SerpFeatures
        {
            HasFeaturedSnippet = hasFeatured,
            HasPeopleAlsoAsk = hasPaa || root.TryGetProperty("related_questions", out _),
            HasLocalPack = root.TryGetProperty("local_results", out _)
                || root.TryGetProperty("local_map", out _),
            HasImagePack = root.TryGetProperty("inline_images", out _)
                || root.TryGetProperty("images_results", out _),
            HasVideoCarousel = root.TryGetProperty("video_results", out _)
                || root.TryGetProperty("short_videos", out _),
            HasKnowledgePanel = root.TryGetProperty("knowledge_graph", out _),
            HasAiOverview = root.TryGetProperty("ai_overview", out _),
        };
    }

    private static string? ReadFirstString(JsonElement element, params string[] propertyNames)
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
}

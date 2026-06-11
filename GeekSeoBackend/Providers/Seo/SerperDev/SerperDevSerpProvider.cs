using System.Text;
using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Providers.Seo.SerperDev;

public sealed class SerperDevSerpProvider(IHttpClientFactory httpClientFactory) : ISerpProvider
{
    public string ProviderName => "serpdev";

    public static bool TryGetApiKey(out string apiKey)
    {
        apiKey = Environment.GetEnvironmentVariable("SERPER_DEV_API_KEY") ?? string.Empty;
        return !string.IsNullOrWhiteSpace(apiKey);
    }

    public async Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default)
    {
        if (!TryGetApiKey(out var apiKey))
            return Result<SerpResult>.Failure("SERPER_DEV_API_KEY must be set.");

        var body = new
        {
            q = request.Keyword,
            gl = request.CountryCode.ToLowerInvariant(),
            hl = request.LanguageCode.ToLowerInvariant(),
            location = request.Location,
            num = Math.Clamp(request.ResultCount, 1, 100),
        };

        var client = httpClientFactory.CreateClient("SerperDev");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "search")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Add("X-API-KEY", apiKey);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(httpRequest, ct);
        }
        catch (Exception ex)
        {
            return Result<SerpResult>.Failure($"Serper.dev request failed: {ex.Message}");
        }

        using (response)
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return Result<SerpResult>.Failure($"Serper.dev HTTP {(int)response.StatusCode}: {Truncate(raw)}");

            return ParseResponse(request, raw);
        }
    }

    internal static Result<SerpResult> ParseResponse(SerpRequest request, string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var organic = new List<SerpOrganicResult>();
            var paa = new List<PeopleAlsoAskResult>();
            var related = new List<string>();
            string? featuredSnippet = null;
            var features = new SerpFeatures();

            if (root.TryGetProperty("organic", out var organicArr) && organicArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in organicArr.EnumerateArray())
                {
                    var url = item.TryGetProperty("link", out var l) ? l.GetString() : null;
                    if (string.IsNullOrWhiteSpace(url)) continue;

                    organic.Add(new SerpOrganicResult
                    {
                        Position = item.TryGetProperty("position", out var pos) ? pos.GetInt32() : organic.Count + 1,
                        Url = url,
                        Title = item.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty,
                        Snippet = item.TryGetProperty("snippet", out var s) ? s.GetString() ?? string.Empty : string.Empty,
                        Domain = item.TryGetProperty("domain", out var d) ? d.GetString() : null,
                    });
                }
            }

            if (root.TryGetProperty("peopleAlsoAsk", out var paaArr) && paaArr.ValueKind == JsonValueKind.Array)
            {
                features = features with { HasPeopleAlsoAsk = true };
                foreach (var item in paaArr.EnumerateArray())
                {
                    var question = item.TryGetProperty("question", out var q) ? q.GetString() : null;
                    if (string.IsNullOrWhiteSpace(question)) continue;

                    paa.Add(new PeopleAlsoAskResult
                    {
                        Question = question,
                        Answer = item.TryGetProperty("snippet", out var ans) ? ans.GetString() : null,
                        SourceUrl = item.TryGetProperty("link", out var link) ? link.GetString() : null,
                        SourceTitle = item.TryGetProperty("title", out var title) ? title.GetString() : null,
                    });
                }
            }

            if (root.TryGetProperty("relatedSearches", out var relArr) && relArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in relArr.EnumerateArray())
                {
                    var query = item.TryGetProperty("query", out var qp) ? qp.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(query))
                        related.Add(query);
                }
            }

            if (root.TryGetProperty("answerBox", out var ab) && ab.ValueKind == JsonValueKind.Object)
            {
                features = features with { HasFeaturedSnippet = true };
                featuredSnippet = ab.TryGetProperty("snippet", out var snip) ? snip.GetString() : null;
            }

            if (root.TryGetProperty("knowledgeGraph", out _))
                features = features with { HasKnowledgePanel = true };

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
            return Result<SerpResult>.Failure($"Failed to parse Serper.dev response: {ex.Message}");
        }
    }

    private static string Truncate(string raw) => raw.Length <= 400 ? raw : raw[..400];
}

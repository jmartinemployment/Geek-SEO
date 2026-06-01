using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Providers.Seo;

public sealed class DataForSEOKeywordDiscoveryProvider(IHttpClientFactory httpClientFactory) : IKeywordDiscoveryProvider
{
    public string ProviderName => "dataforseo";

    public async Task<Result<IReadOnlyList<KeywordResult>>> GetRelatedKeywordsAsync(
        string seedKeyword, string location, int count, CancellationToken ct = default)
    {
        if (!DataForSeoClient.TryGetCredentials(out _, out _))
        {
            return Result<IReadOnlyList<KeywordResult>>.Failure(
                "DATAFORSEO_LOGIN and DATAFORSEO_PASSWORD must be set for keyword discovery.");
        }

        var body = new[]
        {
            new
            {
                keywords = new[] { seedKeyword },
                location_name = location,
                language_code = "en",
            },
        };

        HttpResponseMessage response;
        try
        {
            response = await DataForSeoClient.PostJsonAsync(
                httpClientFactory,
                "/v3/keywords_data/google_ads/adsense_keyword_ideas/live",
                body,
                ct);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<KeywordResult>>.Failure($"DataForSEO keyword discovery failed: {ex.Message}");
        }

        using (response)
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return Result<IReadOnlyList<KeywordResult>>.Failure($"DataForSEO HTTP {(int)response.StatusCode}: {raw}");

            return ParseDiscoveredKeywords(raw, count);
        }
    }

    private static Result<IReadOnlyList<KeywordResult>> ParseDiscoveredKeywords(string raw, int count)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("status_code", out var statusCode) && statusCode.GetInt32() != 20000)
            {
                var msg = root.TryGetProperty("status_message", out var sm) ? sm.GetString() : "DataForSEO error";
                return Result<IReadOnlyList<KeywordResult>>.Failure(msg ?? "DataForSEO error");
            }

            var results = root.GetProperty("tasks")[0].GetProperty("result");
            var list = new List<KeywordResult>();
            foreach (var item in results.EnumerateArray())
            {
                if (list.Count >= count)
                    break;

                var keyword = item.GetProperty("keyword").GetString();
                if (string.IsNullOrWhiteSpace(keyword))
                    continue;

                var searchVolume = item.TryGetProperty("monthly_searches", out var ms)
                    ? (int?)ms.GetInt32() ?? 0
                    : 0;
                var difficulty = item.TryGetProperty("competition_level", out var cl)
                    ? cl.GetInt32() * 10.0
                    : 0.0;
                var cpc = item.TryGetProperty("suggested_bid", out var sb)
                    ? sb.GetDouble()
                    : 0.0;

                list.Add(new KeywordResult
                {
                    Keyword = keyword,
                    SearchVolume = searchVolume,
                    KeywordDifficulty = difficulty,
                    CpcUsd = cpc,
                    Competition = GetCompetitionLabel(difficulty),
                    MonthlyTrend = [],
                });
            }

            return Result<IReadOnlyList<KeywordResult>>.Success(list);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<KeywordResult>>.Failure($"Failed to parse keyword discovery response: {ex.Message}");
        }
    }

    private static string GetCompetitionLabel(double difficulty)
    {
        return difficulty switch
        {
            < 10 => "Low",
            < 30 => "Medium",
            < 60 => "High",
            _ => "Very High",
        };
    }
}

using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Providers.Seo.SerpApi;

public sealed class SerpApiRankSnapshotProvider(IHttpClientFactory httpClientFactory) : IRankSnapshotProvider
{
    public string ProviderName => "serpapi";

    public async Task<Result<RankSnapshot>> GetRankAsync(
        string keyword,
        string domain,
        string location,
        CancellationToken ct = default)
    {
        if (!SerpApiClient.TryGetApiKey(out _))
        {
            return Result<RankSnapshot>.Failure(
                "SERPAPI_API_KEY must be set. Sign up at https://serpapi.com/");
        }

        var query = new Dictionary<string, string>
        {
            ["engine"] = "google",
            ["q"] = keyword,
            ["location"] = location,
            ["hl"] = "en",
            ["device"] = "desktop",
            ["num"] = "100",
        };

        HttpResponseMessage response;
        try
        {
            response = await SerpApiClient.GetSearchAsync(httpClientFactory, query, ct);
        }
        catch (Exception ex)
        {
            return Result<RankSnapshot>.Failure($"SerpApi request failed: {ex.Message}");
        }

        using (response)
        {
            var body = await SerpApiClient.ReadBodyAsync(response, ct);
            if (!body.IsSuccess)
                return Result<RankSnapshot>.Failure(body.Error ?? "SerpApi request failed");

            return ParseResponse(keyword, domain, body.Value!);
        }
    }

    internal static Result<RankSnapshot> ParseResponse(string keyword, string domain, string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (!SerpApiClient.IsSuccess(root))
                return Result<RankSnapshot>.Failure(SerpApiClient.ReadErrorMessage(root));

            if (!root.TryGetProperty("organic_results", out var items)
                || items.ValueKind != JsonValueKind.Array)
            {
                return NotRanked(keyword);
            }

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("link", out var linkEl))
                    continue;

                var url = linkEl.GetString();
                if (string.IsNullOrWhiteSpace(url) || !SerpApiClient.DomainMatchesUrl(url, domain))
                    continue;

                var position = item.TryGetProperty("position", out var posEl) && posEl.TryGetInt32(out var pos)
                    ? pos
                    : (int?)null;

                return Result<RankSnapshot>.Success(new RankSnapshot
                {
                    Keyword = keyword,
                    Position = position,
                    PageUrl = url,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow),
                });
            }

            return NotRanked(keyword);
        }
        catch (Exception ex)
        {
            return Result<RankSnapshot>.Failure($"Failed to parse SerpApi response: {ex.Message}");
        }
    }

    private static Result<RankSnapshot> NotRanked(string keyword) =>
        Result<RankSnapshot>.Success(new RankSnapshot
        {
            Keyword = keyword,
            Position = null,
            PageUrl = null,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
        });
}

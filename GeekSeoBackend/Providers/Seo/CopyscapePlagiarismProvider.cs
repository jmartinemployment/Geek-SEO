using System.Globalization;
using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Providers.Seo;

public sealed class CopyscapePlagiarismProvider(IHttpClientFactory httpClientFactory, CopyscapeOptions options)
    : IPlagiarismProvider
{
    private const string ApiBase = "https://www.copyscape.com/api/";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string ProviderName => "copyscape";

    public bool IsConfigured => options.IsConfigured;

    public async Task<Result<PlagiarismProviderResult>> CheckTextAsync(string plainText, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return Result<PlagiarismProviderResult>.Failure("Copyscape is not configured.");

        var client = httpClientFactory.CreateClient("Copyscape");
        using var content = new FormUrlEncodedContent(BuildFormFields(plainText));
        using var response = await client.PostAsync(ApiBase, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return Result<PlagiarismProviderResult>.Failure($"Copyscape HTTP {(int)response.StatusCode}: {body}");

        CopyscapeSearchResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<CopyscapeSearchResponse>(body, JsonOptions);
        }
        catch (Exception ex)
        {
            return Result<PlagiarismProviderResult>.Failure($"Copyscape response parse error: {ex.Message}");
        }

        if (parsed is null)
            return Result<PlagiarismProviderResult>.Failure("Copyscape returned an empty response.");

        if (!string.IsNullOrWhiteSpace(parsed.Error))
            return Result<PlagiarismProviderResult>.Failure(parsed.Error);

        var matches = MapMatches(parsed);
        var matchPercent = ComputeMatchPercent(parsed, matches);
        var cost = parsed.Cost is null ? (decimal?)null : Convert.ToDecimal(parsed.Cost, CultureInfo.InvariantCulture);

        return Result<PlagiarismProviderResult>.Success(
            new PlagiarismProviderResult(matchPercent, matches, parsed.Querywords ?? 0, cost));
    }

    private IEnumerable<KeyValuePair<string, string>> BuildFormFields(string plainText)
    {
        yield return new KeyValuePair<string, string>("u", options.Username.Trim());
        yield return new KeyValuePair<string, string>("k", options.ApiKey.Trim());
        yield return new KeyValuePair<string, string>("o", "csearch");
        yield return new KeyValuePair<string, string>("e", "UTF-8");
        yield return new KeyValuePair<string, string>("t", plainText);
        yield return new KeyValuePair<string, string>("f", "json");
        yield return new KeyValuePair<string, string>("c", "3");
        yield return new KeyValuePair<string, string>(
            "l",
            options.SpendLimitUsd.ToString("0.00", CultureInfo.InvariantCulture));
    }

    private static IReadOnlyList<PlagiarismMatch> MapMatches(CopyscapeSearchResponse response)
    {
        if (response.Result is null || response.Result.Count == 0)
            return [];

        return response.Result
            .Where(r => !string.IsNullOrWhiteSpace(r.Url))
            .Select(r =>
            {
                var words = r.Wordsmatched ?? r.Minwordsmatched ?? 0;
                var percent = r.Percentmatched is null
                    ? 0m
                    : Convert.ToDecimal(r.Percentmatched, CultureInfo.InvariantCulture);
                if (percent <= 0 && response.Querywords is > 0 && words > 0)
                    percent = Math.Round(words * 100m / response.Querywords.Value, 1);

                return new PlagiarismMatch(
                    r.Url!,
                    r.Title,
                    percent,
                    words,
                    r.Viewurl);
            })
            .OrderByDescending(m => m.MatchPercent)
            .ThenByDescending(m => m.WordsMatched)
            .ToList();
    }

    private static decimal ComputeMatchPercent(CopyscapeSearchResponse response, IReadOnlyList<PlagiarismMatch> matches)
    {
        if (response.Allpercentmatched is not null)
            return Math.Round(Convert.ToDecimal(response.Allpercentmatched, CultureInfo.InvariantCulture), 1);

        if (matches.Count == 0)
            return 0m;

        return Math.Round(matches.Max(m => m.MatchPercent), 1);
    }

    private sealed class CopyscapeSearchResponse
    {
        public string? Error { get; set; }
        public int? Querywords { get; set; }
        public double? Cost { get; set; }
        public int? Count { get; set; }
        public double? Allpercentmatched { get; set; }
        public List<CopyscapeResultItem>? Result { get; set; }
    }

    private sealed class CopyscapeResultItem
    {
        public string? Url { get; set; }
        public string? Title { get; set; }
        public int? Minwordsmatched { get; set; }
        public int? Wordsmatched { get; set; }
        public double? Percentmatched { get; set; }
        public string? Viewurl { get; set; }
    }
}

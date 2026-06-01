using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class SerpAnalysisService(ISerpProvider serp, ISerpDeepCacheRepository deepCache) : ISerpAnalysisService
{
    private const int DeepResultCount = 50;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    public async Task<Result<DeepSerpResult>> AnalyzeAsync(
        Guid userId, DeepSerpRequest request, CancellationToken ct = default)
    {
        _ = userId;
        var keyword = request.Keyword.Trim();
        var location = request.Location.Trim();

        var cached = await deepCache.GetAsync(keyword, location, DeepResultCount, ct);
        if (cached.IsSuccess && cached.Value is not null && cached.Value.ExpiresAt > DateTimeOffset.UtcNow)
        {
            var fromCache = DeserializeCache(cached.Value);
            if (fromCache is not null)
                return Result<DeepSerpResult>.Success(fromCache);
        }

        var serpResult = await serp.GetSerpResultsAsync(new SerpRequest
        {
            Keyword = keyword,
            Location = location,
            LanguageCode = request.LanguageCode,
            ResultCount = DeepResultCount,
        }, ct);

        if (!serpResult.IsSuccess || serpResult.Value is null)
            return Result<DeepSerpResult>.Failure(serpResult.Error ?? "SERP fetch failed");

        var value = serpResult.Value;
        var organic = value.OrganicResults.Select(o => new DeepSerpOrganic
        {
            Position = o.Position,
            Url = o.Url,
            Title = o.Title,
            Snippet = o.Snippet,
            Domain = o.Domain,
        }).ToList();

        var snippetLengths = organic
            .Select(o => o.Snippet?.Length ?? 0)
            .Where(len => len > 0)
            .ToList();

        var termMatrix = SerpTermMatrixBuilder.Build(organic);
        var now = DateTimeOffset.UtcNow;

        var result = new DeepSerpResult
        {
            Keyword = value.Keyword,
            Location = value.Location,
            Provider = serp.ProviderName,
            Organic = organic,
            PeopleAlsoAsk = value.PeopleAlsoAsk.Select(p => p.Question).ToList(),
            RelatedSearches = value.RelatedSearches.ToList(),
            Intent = InferIntent(keyword, value, snippetLengths),
            TermMatrix = termMatrix,
            CachedAt = now.ToString("O"),
        };

        _ = await deepCache.UpsertAsync(new SeoSerpDeepCache
        {
            Keyword = keyword,
            Location = location,
            ResultCount = DeepResultCount,
            ResultsJson = JsonSerializer.Serialize(result, JsonOptions),
            TermMatrixJson = JsonSerializer.Serialize(termMatrix, JsonOptions),
            FetchedAt = now,
            ExpiresAt = now.Add(CacheTtl),
        }, ct);

        if (organic.Count == 0)
        {
            return Result<DeepSerpResult>.Failure(
                "DataForSEO returned no organic results for this keyword and location. Try another keyword or check DataForSEO credentials.");
        }

        return Result<DeepSerpResult>.Success(result);
    }

    private static DeepSerpResult? DeserializeCache(SeoSerpDeepCache row)
    {
        try
        {
            return JsonSerializer.Deserialize<DeepSerpResult>(row.ResultsJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static SerpIntentSummary InferIntent(string keyword, SerpResult serp, IReadOnlyList<int> snippetLengths)
    {
        var lower = keyword.ToLowerInvariant();
        var formats = new List<string>();

        if (serp.Features.HasFeaturedSnippet) formats.Add("featured_snippet");
        if (serp.Features.HasPeopleAlsoAsk) formats.Add("faq");
        if (serp.Features.HasVideoCarousel) formats.Add("video");
        if (serp.Features.HasLocalPack) formats.Add("local");
        if (formats.Count == 0) formats.Add("article");

        var primary = lower.Contains("how to", StringComparison.Ordinal) || lower.StartsWith("how ", StringComparison.Ordinal)
            ? "informational"
            : lower.Contains("buy", StringComparison.Ordinal) || lower.Contains("price", StringComparison.Ordinal)
                ? "commercial"
                : lower.Contains("near me", StringComparison.Ordinal)
                    ? "local"
                    : "informational";

        return new SerpIntentSummary
        {
            PrimaryIntent = primary,
            ContentFormats = formats,
            AvgSnippetLength = snippetLengths.Count > 0 ? snippetLengths.Average() : 0,
        };
    }
}

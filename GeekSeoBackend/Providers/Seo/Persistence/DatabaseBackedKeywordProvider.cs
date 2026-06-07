using System.Text.Json;
using GeekSeo.Application.Configuration;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Providers.Seo.Persistence;

/// <summary>
/// Reads keyword suggestions from <c>seo_keyword_vendor_snapshots</c> when a non-expired row exists; calls vendor only on miss or expiry.
/// </summary>
public sealed class DatabaseBackedKeywordProvider(
    IKeywordProvider inner,
    IKeywordVendorSnapshotRepository snapshots) : IKeywordProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public string ProviderName => inner.ProviderName;

    public async Task<Result<IReadOnlyList<KeywordResult>>> GetKeywordSuggestionsAsync(
        string seedKeyword,
        string location,
        int count,
        CancellationToken ct = default)
    {
        var seed = seedKeyword.Trim();
        var loc = location.Trim();
        const string languageCode = "en";

        var snapshot = await snapshots.GetAsync(seed, loc, languageCode, ct);
        if (snapshot.IsSuccess && snapshot.Value is not null && snapshot.Value.ExpiresAt > DateTimeOffset.UtcNow)
        {
            var fromDb = Deserialize(snapshot.Value.ResultsJson);
            if (fromDb.Count > 0)
                return Result<IReadOnlyList<KeywordResult>>.Success(fromDb.Take(count).ToList());
        }

        var live = await inner.GetKeywordSuggestionsAsync(seed, loc, count, ct);
        if (!live.IsSuccess || live.Value is null || live.Value.Count == 0)
            return live;

        var now = DateTimeOffset.UtcNow;
        _ = await snapshots.UpsertAsync(new SeoKeywordVendorSnapshot
        {
            SeedKeyword = seed,
            Location = loc,
            LanguageCode = languageCode,
            Provider = inner.ProviderName,
            ResultsJson = JsonSerializer.Serialize(live.Value, JsonOptions),
            FetchedAt = now,
            ExpiresAt = now.AddDays(VendorPersistenceSettings.KeywordRetentionDays),
        }, ct);

        return live;
    }

    private static IReadOnlyList<KeywordResult> Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<KeywordResult>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

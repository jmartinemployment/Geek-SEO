using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Extensions;

namespace GeekSeoBackend.Providers.Seo;

/// <summary>
/// No-network stubs when <see cref="SeoProviderRegistration.VendorApisEnabledEnv"/> is false.
/// Tier 1 (crawl/fusion) still runs; Tier 2 skips without vendor spend.
/// </summary>
internal static class DisabledVendorMessage
{
    public const string Text =
        "SEO vendor APIs disabled (SEO_VENDOR_APIS_ENABLED=false). No DataForSEO or SerpApi calls are made.";
}

internal sealed class DisabledSerpProvider : ISerpProvider
{
    public string ProviderName => "disabled";

    public Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default) =>
        Task.FromResult(Result<SerpResult>.Failure(DisabledVendorMessage.Text));
}

internal sealed class DisabledKeywordProvider : IKeywordProvider
{
    public string ProviderName => "disabled";

    public Task<Result<IReadOnlyList<KeywordResult>>> GetKeywordSuggestionsAsync(
        string seedKeyword,
        string location,
        int count,
        CancellationToken ct = default) =>
        Task.FromResult(Result<IReadOnlyList<KeywordResult>>.Failure(DisabledVendorMessage.Text));
}

internal sealed class DisabledRankSnapshotProvider : IRankSnapshotProvider
{
    public string ProviderName => "disabled";

    public Task<Result<RankSnapshot>> GetRankAsync(
        string keyword,
        string domain,
        string location,
        CancellationToken ct = default) =>
        Task.FromResult(Result<RankSnapshot>.Failure(DisabledVendorMessage.Text));
}

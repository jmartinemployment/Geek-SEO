using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Providers.Seo;

/// <summary>Keyword enrichment disabled (KEYWORD_PROVIDER=none). Niche analyzer uses SERP PAA + related searches instead.</summary>
public sealed class NoOpKeywordProvider : IKeywordProvider
{
    public string ProviderName => "none";

    public Task<Result<IReadOnlyList<KeywordResult>>> GetKeywordSuggestionsAsync(
        string seedKeyword, string location, int count, CancellationToken ct = default) =>
        Task.FromResult(Result<IReadOnlyList<KeywordResult>>.Failure("Keyword provider disabled (KEYWORD_PROVIDER=none)."));
}

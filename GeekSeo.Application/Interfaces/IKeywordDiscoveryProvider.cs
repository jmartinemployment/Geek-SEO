using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IKeywordDiscoveryProvider
{
    string ProviderName { get; }

    Task<Result<IReadOnlyList<KeywordResult>>> GetRelatedKeywordsAsync(
        string seedKeyword, string location, int count, CancellationToken ct = default);
}

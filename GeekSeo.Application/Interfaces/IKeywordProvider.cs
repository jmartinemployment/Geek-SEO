using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IKeywordProvider
{
    string ProviderName { get; }

    Task<Result<IReadOnlyList<KeywordResult>>> GetKeywordSuggestionsAsync(
        string seedKeyword, string location, int count, CancellationToken ct = default);
}

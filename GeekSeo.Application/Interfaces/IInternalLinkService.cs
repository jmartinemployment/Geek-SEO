using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IInternalLinkService
{
    Task<Result<IReadOnlyList<InternalLinkSuggestion>>> SuggestAsync(
        Guid userId, InternalLinkSuggestRequest request, CancellationToken ct = default);

    Task<Result<InternalLinkAutoInsertResult>> AutoInsertAsync(
        Guid userId, InternalLinkAutoInsertRequest request, CancellationToken ct = default);
}

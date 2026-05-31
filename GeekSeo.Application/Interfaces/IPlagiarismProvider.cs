using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IPlagiarismProvider
{
    string ProviderName { get; }

    bool IsConfigured { get; }

    Task<Result<PlagiarismProviderResult>> CheckTextAsync(string plainText, CancellationToken ct = default);
}

public sealed record PlagiarismProviderResult(
    decimal MatchPercent,
    IReadOnlyList<PlagiarismMatch> Matches,
    int QueryWordCount,
    decimal? CostUsd);

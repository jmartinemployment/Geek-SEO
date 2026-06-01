namespace GeekSeo.Application.Interfaces;

using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

public interface IRankSnapshotProvider
{
    string ProviderName { get; }
    Task<Result<RankSnapshot>> GetRankAsync(
        string keyword, string domain, string location, CancellationToken ct = default);
}

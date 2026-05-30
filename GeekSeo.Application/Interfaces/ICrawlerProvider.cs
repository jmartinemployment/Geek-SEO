using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ICrawlerProvider
{
    string ProviderName { get; }
    Task<Result<PageContent>> CrawlPageAsync(string url, CancellationToken ct = default);
    Task<bool> IsAllowedByRobotsTxtAsync(string url, CancellationToken ct = default);
}

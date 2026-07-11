using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IWordPressPublishService
{
    Task<Result> ConnectAsync(Guid userId, Guid projectId, WordPressConnectRequest request, CancellationToken ct = default);

    Task<Result<WordPressPublishResult>> PublishDocumentAsync(
        Guid userId, Guid documentId, WordPressPublishOptions options, CancellationToken ct = default);

    Task<Result> DisconnectAsync(Guid userId, Guid projectId, CancellationToken ct = default);

    Task<Result<WordPressConnectionStatus>> GetStatusAsync(
        Guid userId, Guid projectId, CancellationToken ct = default);
}

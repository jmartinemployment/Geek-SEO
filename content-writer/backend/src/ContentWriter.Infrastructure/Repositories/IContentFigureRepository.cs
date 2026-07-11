using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;

namespace ContentWriter.Infrastructure.Repositories;

public interface IContentFigureRepository
{
    Task<IReadOnlyList<ContentFigure>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContentFigure>> ListTrackedByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<ContentFigure?> GetByHeadingSlugAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        CancellationToken cancellationToken = default);

    Task<(int Ready, int Published)> CountReadyAndPublishedAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task StampAfterTextPublishAsync(
        Guid projectId,
        string sourceType,
        string geekApiSlug,
        int geekPostId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ContentFigure figure, CancellationToken cancellationToken = default);

    Task UpdateAsync(ContentFigure figure, CancellationToken cancellationToken = default);

    Task DeleteAsync(ContentFigure figure, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

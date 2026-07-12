using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;

namespace ContentWriter.Infrastructure.Repositories;

public interface IProjectPublicationRepository
{
    Task UpsertAsync(ProjectPublication publication, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectPublication>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<ProjectPublication?> FindAsync(
        Guid projectId,
        GeneratedContentType contentType,
        string geekApiSlug,
        CancellationToken cancellationToken = default);
}

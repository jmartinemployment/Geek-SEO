using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContentWriter.Infrastructure.Repositories;

public class ProjectPublicationRepository : IProjectPublicationRepository
{
    private readonly ContentWriterDbContext _context;

    public ProjectPublicationRepository(ContentWriterDbContext context)
    {
        _context = context;
    }

    public async Task UpsertAsync(ProjectPublication publication, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ProjectPublications.FirstOrDefaultAsync(
            p => p.ProjectId == publication.ProjectId
                 && p.ContentType == publication.ContentType
                 && p.GeekApiSlug == publication.GeekApiSlug,
            cancellationToken);

        if (existing is null)
        {
            await _context.ProjectPublications.AddAsync(publication, cancellationToken);
            return;
        }

        existing.GeekPostId = publication.GeekPostId;
        existing.PublishedAtUtc = publication.PublishedAtUtc;
    }

    public async Task<IReadOnlyList<ProjectPublication>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default) =>
        await _context.ProjectPublications
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.PublishedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<ProjectPublication?> FindAsync(
        Guid projectId,
        GeneratedContentType contentType,
        string geekApiSlug,
        CancellationToken cancellationToken = default) =>
        await _context.ProjectPublications.FirstOrDefaultAsync(
            p => p.ProjectId == projectId
                 && p.ContentType == contentType
                 && p.GeekApiSlug == geekApiSlug,
            cancellationToken);
}

using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContentWriter.Infrastructure.Repositories;

public sealed class ContentFigureRepository : IContentFigureRepository
{
    private readonly ContentWriterDbContext _db;

    public ContentFigureRepository(ContentWriterDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ContentFigure>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ContentFigures
            .AsNoTracking()
            .Where(f => f.ProjectId == projectId)
            .OrderBy(f => f.SourceType)
            .ThenBy(f => f.SectionOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContentFigure>> ListTrackedByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ContentFigures
            .Where(f => f.ProjectId == projectId)
            .OrderBy(f => f.SourceType)
            .ThenBy(f => f.SectionOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<ContentFigure?> GetByHeadingSlugAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        CancellationToken cancellationToken = default)
    {
        return await _db.ContentFigures
            .FirstOrDefaultAsync(
                f => f.ProjectId == projectId
                     && f.SourceType == sourceType
                     && f.HeadingSlug == headingSlug,
                cancellationToken);
    }

    public async Task<(int Ready, int Published)> CountReadyAndPublishedAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var counts = await _db.ContentFigures
            .AsNoTracking()
            .Where(f => f.ProjectId == projectId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Ready = g.Count(f => f.Status == FigureStatus.Ready),
                Published = g.Count(f => f.Status == FigureStatus.Published),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return counts is null ? (0, 0) : (counts.Ready, counts.Published);
    }

    public async Task StampAfterTextPublishAsync(
        Guid projectId,
        string sourceType,
        string geekApiSlug,
        int geekPostId,
        CancellationToken cancellationToken = default)
    {
        var figures = await _db.ContentFigures
            .Where(f => f.ProjectId == projectId && f.SourceType == sourceType)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var figure in figures)
        {
            figure.GeekApiSlug = geekApiSlug;
            figure.GeekPostId = geekPostId;
            figure.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task AddAsync(ContentFigure figure, CancellationToken cancellationToken = default)
    {
        _db.ContentFigures.Add(figure);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ContentFigure figure, CancellationToken cancellationToken = default)
    {
        _db.ContentFigures.Update(figure);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ContentFigure figure, CancellationToken cancellationToken = default)
    {
        _db.ContentFigures.Remove(figure);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}

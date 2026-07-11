using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Data;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ContentFigures.Services;

public static class FigureSkipService
{
    public static async Task SkipAsync(
        ContentWriterDbContext db,
        Guid projectId,
        string sourceType,
        string headingSlug,
        CancellationToken cancellationToken = default)
    {
        FigureAttachService.ValidateSourceType(sourceType);

        var figure = await db.ContentFigures.FirstOrDefaultAsync(
            f => f.ProjectId == projectId
                 && f.SourceType == sourceType
                 && f.HeadingSlug == headingSlug,
            cancellationToken);

        if (figure is null)
        {
            throw new InvalidOperationException(
                $"No figure row for project {projectId}, source={sourceType}, headingSlug={headingSlug}.");
        }

        figure.Status = FigureStatus.Skipped;
        figure.SkipReason = FigureSkipReason.UserSkipped;
        figure.NeedsFigureMerge = false;
        figure.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }
}

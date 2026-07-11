using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Repositories;

namespace ContentWriter.Application.Services.Figures;

public sealed class ContentFigureSyncService
{
    private readonly IContentFigureRepository _figures;

    public ContentFigureSyncService(IContentFigureRepository figures)
    {
        _figures = figures;
    }

    public async Task SyncAsync(
        Guid projectId,
        IReadOnlyList<FigureSyncSectionInput> sections,
        CancellationToken cancellationToken = default)
    {
        var existing = await _figures.ListTrackedByProjectAsync(projectId, cancellationToken);
        var existingByKey = existing.ToDictionary(
            f => (f.SourceType, f.HeadingSlug),
            f => f);

        var incomingSlugs = new HashSet<(string SourceType, string HeadingSlug)>();
        var usedSlugsBySource = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var section in sections.OrderBy(s => s.SourceType, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.SectionOrder))
        {
            if (!usedSlugsBySource.TryGetValue(section.SourceType, out var used))
            {
                used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                usedSlugsBySource[section.SourceType] = used;
            }

            var slug = FigureHeadingSlugResolver.ResolveUniqueSlug(section.Heading, section.SectionOrder, used);
            used.Add(slug);
            incomingSlugs.Add((section.SourceType, slug));

            var key = (section.SourceType, slug);
            if (existingByKey.TryGetValue(key, out var figure))
            {
                ApplyIncomingToExisting(figure, section, slug);
                figure.UpdatedAtUtc = DateTime.UtcNow;
                await _figures.UpdateAsync(figure, cancellationToken);
            }
            else
            {
                var created = new ContentFigure
                {
                    ProjectId = projectId,
                    SourceType = section.SourceType,
                    SectionOrder = section.SectionOrder,
                    HeadingSlug = slug,
                    Heading = section.Heading,
                    BriefText = section.BriefText,
                    ImagePromptContentId = section.ImagePromptContentId,
                    Status = FigureStatus.Pending,
                    ImageAlt = FigureHeadingSlugResolver.DefaultImageAlt(section.Heading),
                };
                await _figures.AddAsync(created, cancellationToken);
            }
        }

        foreach (var figure in existing)
        {
            var key = (figure.SourceType, figure.HeadingSlug);
            if (incomingSlugs.Contains(key))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(figure.ImageUrl))
            {
                figure.Status = FigureStatus.Skipped;
                figure.SkipReason = FigureSkipReason.SectionRemoved;
                figure.UpdatedAtUtc = DateTime.UtcNow;
                await _figures.UpdateAsync(figure, cancellationToken);
                continue;
            }

            if (figure.Status == FigureStatus.Pending)
            {
                await _figures.DeleteAsync(figure, cancellationToken);
            }
        }

        await _figures.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyIncomingToExisting(
        ContentFigure figure,
        FigureSyncSectionInput section,
        string slug)
    {
        figure.SectionOrder = section.SectionOrder;
        figure.Heading = section.Heading;
        figure.HeadingSlug = slug;
        figure.BriefText = section.BriefText;
        figure.ImagePromptContentId = section.ImagePromptContentId;

        if (string.IsNullOrWhiteSpace(figure.ImageAlt))
        {
            figure.ImageAlt = FigureHeadingSlugResolver.DefaultImageAlt(section.Heading);
        }

        if (figure.Status is FigureStatus.Ready or FigureStatus.Published or FigureStatus.Skipped)
        {
            return;
        }

        figure.Status = FigureStatus.Pending;
        figure.SkipReason = null;
    }
}

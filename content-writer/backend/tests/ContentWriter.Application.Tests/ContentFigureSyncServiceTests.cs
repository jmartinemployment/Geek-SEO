using ContentWriter.Application.Services.Figures;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Repositories;

namespace ContentWriter.Application.Tests;

public class ContentFigureSyncServiceTests
{
    [Fact]
    public async Task SyncAsync_creates_pending_rows_for_new_sections()
    {
        var repo = new InMemoryContentFigureRepository();
        var sync = new ContentFigureSyncService(repo);
        var projectId = Guid.NewGuid();

        await sync.SyncAsync(projectId, [
            new FigureSyncSectionInput(FigureSourceType.Pillar, "Intro", 1, "Brief one", Guid.NewGuid()),
        ]);

        var rows = await repo.ListByProjectAsync(projectId);
        Assert.Single(rows);
        Assert.Equal(FigureStatus.Pending, rows[0].Status);
        Assert.Equal("intro", rows[0].HeadingSlug);
        Assert.Equal("Brief one", rows[0].BriefText);
    }

    [Fact]
    public async Task SyncAsync_preserves_ready_row_image_url_when_heading_unchanged()
    {
        var repo = new InMemoryContentFigureRepository();
        var sync = new ContentFigureSyncService(repo);
        var projectId = Guid.NewGuid();
        var promptId = Guid.NewGuid();

        await repo.AddAsync(new ContentFigure
        {
            ProjectId = projectId,
            SourceType = FigureSourceType.Pillar,
            SectionOrder = 1,
            HeadingSlug = "intro",
            Heading = "Intro",
            BriefText = "Old brief",
            Status = FigureStatus.Ready,
            ImageUrl = "https://blob.example/intro.webp",
            ImageAlt = "Diagram: Intro",
        });
        await repo.SaveChangesAsync();

        await sync.SyncAsync(projectId, [
            new FigureSyncSectionInput(FigureSourceType.Pillar, "Intro", 1, "New brief", promptId),
        ]);

        var row = (await repo.ListByProjectAsync(projectId)).Single();
        Assert.Equal(FigureStatus.Ready, row.Status);
        Assert.Equal("https://blob.example/intro.webp", row.ImageUrl);
        Assert.Equal("New brief", row.BriefText);
        Assert.Equal(promptId, row.ImagePromptContentId);
    }

    [Fact]
    public async Task SyncAsync_updates_section_order_without_swapping_art_between_slugs()
    {
        var repo = new InMemoryContentFigureRepository();
        var sync = new ContentFigureSyncService(repo);
        var projectId = Guid.NewGuid();

        await repo.AddAsync(new ContentFigure
        {
            ProjectId = projectId,
            SourceType = FigureSourceType.Pillar,
            SectionOrder = 1,
            HeadingSlug = "alpha",
            Heading = "Alpha",
            BriefText = "A",
            Status = FigureStatus.Ready,
            ImageUrl = "https://blob.example/alpha.webp",
            ImageAlt = "Diagram: Alpha",
        });
        await repo.AddAsync(new ContentFigure
        {
            ProjectId = projectId,
            SourceType = FigureSourceType.Pillar,
            SectionOrder = 2,
            HeadingSlug = "beta",
            Heading = "Beta",
            BriefText = "B",
            Status = FigureStatus.Pending,
        });
        await repo.SaveChangesAsync();

        await sync.SyncAsync(projectId, [
            new FigureSyncSectionInput(FigureSourceType.Pillar, "Beta", 1, "B2", Guid.NewGuid()),
            new FigureSyncSectionInput(FigureSourceType.Pillar, "Alpha", 2, "A2", Guid.NewGuid()),
        ]);

        var rows = await repo.ListByProjectAsync(projectId);
        var alpha = rows.Single(r => r.HeadingSlug == "alpha");
        var beta = rows.Single(r => r.HeadingSlug == "beta");
        Assert.Equal(2, alpha.SectionOrder);
        Assert.Equal("https://blob.example/alpha.webp", alpha.ImageUrl);
        Assert.Equal(1, beta.SectionOrder);
        Assert.Null(beta.ImageUrl);
    }

    private sealed class InMemoryContentFigureRepository : IContentFigureRepository
    {
        private readonly List<ContentFigure> _figures = [];

        public Task<IReadOnlyList<ContentFigure>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ContentFigure>>(_figures.Where(f => f.ProjectId == projectId).OrderBy(f => f.SourceType).ThenBy(f => f.SectionOrder).ToList());

        public Task<IReadOnlyList<ContentFigure>> ListTrackedByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) =>
            ListByProjectAsync(projectId, cancellationToken);

        public Task<ContentFigure?> GetByHeadingSlugAsync(Guid projectId, string sourceType, string headingSlug, CancellationToken cancellationToken = default) =>
            Task.FromResult(_figures.FirstOrDefault(f =>
                f.ProjectId == projectId && f.SourceType == sourceType && f.HeadingSlug == headingSlug));

        public Task<(int Ready, int Published)> CountReadyAndPublishedAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            var ready = _figures.Count(f => f.ProjectId == projectId && f.Status == FigureStatus.Ready);
            var published = _figures.Count(f => f.ProjectId == projectId && f.Status == FigureStatus.Published);
            return Task.FromResult((ready, published));
        }

        public Task StampAfterTextPublishAsync(Guid projectId, string sourceType, string geekApiSlug, int geekPostId, CancellationToken cancellationToken = default)
        {
            foreach (var figure in _figures.Where(f => f.ProjectId == projectId && f.SourceType == sourceType))
            {
                figure.GeekApiSlug = geekApiSlug;
                figure.GeekPostId = geekPostId;
                if (figure.Status is FigureStatus.Ready or FigureStatus.Published)
                {
                    figure.NeedsFigureMerge = true;
                }
            }

            return Task.CompletedTask;
        }

        public Task AddAsync(ContentFigure figure, CancellationToken cancellationToken = default)
        {
            _figures.Add(figure);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ContentFigure figure, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(ContentFigure figure, CancellationToken cancellationToken = default)
        {
            _figures.Remove(figure);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

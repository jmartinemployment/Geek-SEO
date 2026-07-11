namespace ContentFigures.Services;

public static class FigureSkipService
{
    public static Task SkipAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        CancellationToken cancellationToken = default) =>
        Infrastructure.ContentFigureAttachRunner.SkipAsync(
            projectId,
            sourceType,
            headingSlug,
            cancellationToken);
}

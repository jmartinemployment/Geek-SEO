using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentBlogSpokeMigrator
{
    /// <summary>
    /// When pillar has BlogSpokeJson and no child spokes, creates one migrated child document.
    /// Returns the child id when one exists or was created; null when migration does not apply.
    /// </summary>
    Task<Result<Guid?>> EnsureMigratedChildAsync(
        Guid userId,
        Guid pillarDocumentId,
        CancellationToken ct = default);
}

using ContentWriter.Domain.Entities;

namespace ContentWriter.Application.Services.Publish;

internal static class GeneratedContentPresentation
{
    public static string PublishTitle(GeneratedContent row) =>
        string.IsNullOrWhiteSpace(row.DisplayTitle) ? row.Title : row.DisplayTitle.Trim();

    public static string ListingExcerpt(GeneratedContent row) =>
        string.IsNullOrWhiteSpace(row.ListingExcerpt) ? string.Empty : row.ListingExcerpt.Trim();
}

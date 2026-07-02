using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentScoringService
{
    Task<Result<ContentScoreHubResult>> ProcessContentChangedAsync(
        Guid userId, Guid documentId, string contentHtml, string targetKeyword,
        CancellationToken ct = default);

    Task<Result<ContentScoreHubResult>> ScoreSavedDocumentAsync(
        Guid userId, Guid documentId, string? targetKeyword = null, CancellationToken ct = default);

    Task<Result<ContentScoreHubResult>> ProcessKeywordChangedAsync(
        Guid userId, Guid documentId, string contentHtml, string targetKeyword, string targetLocation,
        CancellationToken ct = default);

    Task<Result<AutoOptimizeResult>> AutoOptimizeAsync(
        Guid userId, Guid documentId, CancellationToken ct = default);

    Task<Result<ApplySuggestionResponse>> ApplySuggestionAsync(
        Guid userId, Guid documentId, string suggestionId, string? contentHtml = null, CancellationToken ct = default);

    Task<Result<ApplySuggestionResult>> InsertResearchCitationAsync(
        Guid userId,
        Guid documentId,
        string url,
        string? title = null,
        string? contentHtml = null,
        CancellationToken ct = default);
}

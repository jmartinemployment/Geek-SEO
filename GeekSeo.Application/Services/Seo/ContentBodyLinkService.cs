using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentBodyLinkService(IContentDocumentService documents) : IContentBodyLinkService
{
    public async Task<Result<ApplyBodyLinksResponse>> ApplyAsync(
        Guid userId,
        Guid documentId,
        ApplyBodyLinksRequest request,
        CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ApplyBodyLinksResponse>.Failure(access.Error ?? "Access denied");

        var doc = access.Value;
        if (string.IsNullOrWhiteSpace(doc.ContentHtml))
            return Result<ApplyBodyLinksResponse>.Failure("Document has no body HTML to update.");

        if (request.Instructions.Count == 0)
            return Result<ApplyBodyLinksResponse>.Failure("At least one body link instruction is required.");

        var activeInstructions = request.Instructions
            .Where(i => i.IsTargetActive)
            .ToList();

        var updatedHtml = ContentBodyLinkInserter.ApplyBodyLinks(doc.ContentHtml, request.Instructions);
        var changed = !string.Equals(updatedHtml, doc.ContentHtml, StringComparison.Ordinal);

        if (!changed)
        {
            return Result<ApplyBodyLinksResponse>.Success(new ApplyBodyLinksResponse
            {
                ContentHtml = doc.ContentHtml,
                AppliedCount = 0,
                Changed = false,
            });
        }

        var saved = await documents.UpdateContentAsync(
            userId,
            documentId,
            new UpdateContentRequest { ContentHtml = updatedHtml },
            ct);

        if (!saved.IsSuccess || saved.Value is null)
            return Result<ApplyBodyLinksResponse>.Failure(saved.Error ?? "Failed to save updated content.");

        return Result<ApplyBodyLinksResponse>.Success(new ApplyBodyLinksResponse
        {
            ContentHtml = saved.Value.ContentHtml,
            AppliedCount = activeInstructions.Count,
            Changed = true,
        });
    }
}

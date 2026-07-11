using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentFeaturedImageService(
    IContentDocumentService documents,
    IContentDocumentRepository documentRepo,
    IOpenAIImageGenerator imageGenerator) : IContentFeaturedImageService
{
    public async Task<Result<FeaturedImageResult>> GenerateForDocumentAsync(
        Guid userId,
        Guid documentId,
        GenerateFeaturedImageRequest request,
        CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<FeaturedImageResult>.Failure(access.Error ?? "Access denied");

        var doc = access.Value;
        if (!request.Regenerate && !string.IsNullOrWhiteSpace(doc.FeaturedImageUrl))
        {
            return Result<FeaturedImageResult>.Success(new FeaturedImageResult
            {
                DataUrl = doc.FeaturedImageUrl,
                Prompt = FeaturedImagePromptBuilder.BuildForDocument(doc),
                MimeType = InferMimeType(doc.FeaturedImageUrl),
            });
        }

        var prompt = FeaturedImagePromptBuilder.BuildForDocument(doc);
        var generated = await imageGenerator.GenerateAsync(prompt, ct);
        if (!generated.IsSuccess || generated.Value is null)
            return Result<FeaturedImageResult>.Failure(generated.Error ?? "Image generation failed");

        var saved = await documentRepo.UpdateFeaturedImageAsync(
            documentId,
            generated.Value.DataUrl,
            ct);
        if (!saved.IsSuccess)
            return Result<FeaturedImageResult>.Failure(saved.Error ?? "Failed to save featured image");

        return generated;
    }

    private static string InferMimeType(string dataUrl)
    {
        if (dataUrl.StartsWith("data:image/webp", StringComparison.OrdinalIgnoreCase))
            return "image/webp";
        if (dataUrl.StartsWith("data:image/jpeg", StringComparison.OrdinalIgnoreCase))
            return "image/jpeg";
        return "image/png";
    }
}

using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class ArticleRenderService(
    IContentDocumentService documents,
    IContentBriefService briefs) : IArticleRenderService
{
    public async Task<Result<RenderedArticleResult>> RenderAsync(
        Guid userId,
        Guid documentId,
        CancellationToken ct = default)
    {
        var document = await documents.GetAsync(userId, documentId, ct);
        if (!document.IsSuccess || document.Value is null)
            return Result<RenderedArticleResult>.Failure(document.Error ?? "Document not found");

        var doc = document.Value;
        var brief = await briefs.GenerateBriefAsync(userId, new GenerateBriefRequest
        {
            ProjectId = doc.ProjectId,
            Keyword = doc.TargetKeyword,
            Location = doc.TargetLocation ?? "United States",
        }, ct);

        if (!brief.IsSuccess || brief.Value is null)
            return Result<RenderedArticleResult>.Failure(brief.Error ?? "Could not build content brief for rendering");

        var schemaScripts = ArticleSchemaBuilder.BuildScripts(brief.Value, doc.Title, doc.ContentHtml);
        var renderedHtml = schemaScripts.Count == 0
            ? doc.ContentHtml
            : $"{doc.ContentHtml}\n{string.Join("\n", schemaScripts)}";

        return Result<RenderedArticleResult>.Success(new RenderedArticleResult
        {
            BodyHtml = doc.ContentHtml,
            RenderedHtml = renderedHtml,
            SchemaScripts = schemaScripts,
            SchemaTypes = [brief.Value.SchemaBlueprint.PrimaryType, .. brief.Value.SchemaBlueprint.AdditionalTypes],
        });
    }
}

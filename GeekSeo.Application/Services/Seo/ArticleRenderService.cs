using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class ArticleRenderService(
    IContentDocumentService documents,
    IContentBriefService briefs,
    IUrlResearchService urlResearch) : IArticleRenderService
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
        IReadOnlyList<string> schemaScripts;
        IReadOnlyList<string> schemaTypes;

        if (ResearchBackedWriteGate.IsResearchBacked(doc))
        {
            var research = await urlResearch.GetFullAsync(userId, doc.UrlResearchId!.Value, ct);
            if (!research.IsSuccess || research.Value is null)
                return Result<RenderedArticleResult>.Failure(research.Error ?? "Page research not found");

            var gate = ResearchBackedWriteGate.EnsureResearchReady(doc, research.Value);
            if (!gate.IsSuccess)
                return Result<RenderedArticleResult>.Failure(gate.Error ?? "Research not ready");

            var context = WritingResearchContextMapper.FromEntity(research.Value);
            schemaScripts = ArticleSchemaBuilder.BuildScripts(context, doc.Title, doc.ContentHtml);
            schemaTypes = ["TechArticle", "FAQPage"];
        }
        else
        {
            var brief = await briefs.GenerateBriefAsync(userId, new GenerateBriefRequest
            {
                ProjectId = doc.ProjectId,
                Keyword = doc.TargetKeyword,
                Location = doc.TargetLocation ?? "United States",
            }, ct);

            if (!brief.IsSuccess || brief.Value is null)
                return Result<RenderedArticleResult>.Failure(brief.Error ?? "Could not build content brief for rendering");

            schemaScripts = ArticleSchemaBuilder.BuildScripts(brief.Value, doc.Title, doc.ContentHtml);
            schemaTypes = [brief.Value.SchemaBlueprint.PrimaryType, .. brief.Value.SchemaBlueprint.AdditionalTypes];
        }

        var renderedHtml = schemaScripts.Count == 0
            ? PrependFeaturedImage(doc)
            : $"{PrependFeaturedImage(doc)}\n{string.Join("\n", schemaScripts)}";

        return Result<RenderedArticleResult>.Success(new RenderedArticleResult
        {
            BodyHtml = PrependFeaturedImage(doc),
            RenderedHtml = renderedHtml,
            SchemaScripts = schemaScripts,
            SchemaTypes = schemaTypes,
            FeaturedImageUrl = doc.FeaturedImageUrl,
        });
    }

    private static string PrependFeaturedImage(SeoContentDocument doc)
    {
        if (string.IsNullOrWhiteSpace(doc.FeaturedImageUrl))
            return doc.ContentHtml;

        var alt = System.Net.WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(doc.Title) ? doc.TargetKeyword : doc.Title);
        var src = doc.FeaturedImageUrl;
        return $"<figure class=\"featured-image\"><img src=\"{src}\" alt=\"{alt}\" /></figure>\n{doc.ContentHtml}";
    }
}

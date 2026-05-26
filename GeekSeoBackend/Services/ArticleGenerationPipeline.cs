using GeekApplication.Interfaces.Seo;
using GeekApplication.Models.Seo;
using GeekApplication.Results;

namespace GeekSeoBackend.Services;

/// <summary>Brief → outline → draft pipeline used by background article workers.</summary>
public static class ArticleGenerationPipeline
{
    public static async Task<Result<string>> GenerateHtmlAsync(
        Guid userId,
        Guid projectId,
        string keyword,
        string location,
        string title,
        IContentBriefService briefs,
        IAIWritingService writing,
        CancellationToken ct)
    {
        var briefResult = await briefs.GenerateBriefAsync(userId, new GenerateBriefRequest
        {
            ProjectId = projectId,
            Keyword = keyword,
            Location = location,
        }, ct);

        if (!briefResult.IsSuccess || briefResult.Value is null)
            return Result<string>.Failure(briefResult.Error ?? "Brief generation failed");

        var brief = briefResult.Value;

        var outlineResult = await writing.GenerateOutlineAsync(userId, new WritingOutlineRequest
        {
            Keyword = keyword,
            Brief = brief,
        }, ct);

        if (!outlineResult.IsSuccess || outlineResult.Value is null)
            return Result<string>.Failure(outlineResult.Error ?? "Outline generation failed");

        var draftResult = await writing.GenerateDraftAsync(userId, new WritingDraftRequest
        {
            Keyword = keyword,
            Brief = brief,
            Outline = outlineResult.Value.Content,
            TargetWordCount = brief.TargetWordCount,
        }, ct);

        if (!draftResult.IsSuccess || draftResult.Value is null)
            return Result<string>.Failure(draftResult.Error ?? "Draft generation failed");

        var html = draftResult.Value.Content.Trim();
        if (!html.Contains("<h1", StringComparison.OrdinalIgnoreCase))
            html = $"<h1>{title}</h1>\n{html}";

        return Result<string>.Success(html);
    }
}

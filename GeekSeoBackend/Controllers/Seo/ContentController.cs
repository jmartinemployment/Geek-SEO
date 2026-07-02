using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/content")]
public sealed class ContentController(
    IContentDocumentService content,
    IContentBlogSpokeService blogSpoke,
    IContentClusterPlanService clusterPlan,
    IContentSpokeService spokes,
    IContentLinkedFaqService linkedFaqs,
    IContentBodyLinkService bodyLinks,
    IContentResearchWritingService researchWriting,
    IContentDraftJobService draftJobs,
    ContentDraftJobChannel draftJobChannel,
    IContentFeaturedImageService featuredImages,
    IArticleRenderService renderer,
    ICompetitorInsightsService competitors,
    IContentScoringService scoring,
    IContentSocialPostService socialPosts,
    IAnalysisRunRepository analysisRuns,
    WritingResearchContextLoader researchLoader,
    ICurrentUserContext user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid projectId, CancellationToken ct)
    {
        var result = await content.ListByProjectAsync(user.RequireUserId(), projectId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await content.GetAsync(user.RequireUserId(), id, ct);
        if (!result.IsSuccess)
            return result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ? NotFound() : BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/research-pack")]
    public async Task<IActionResult> GetResearchPack(Guid id, CancellationToken ct)
    {
        var access = await content.EnsureAccessAsync(user.RequireUserId(), id, ct);
        if (!access.IsSuccess || access.Value is null)
            return access.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ? NotFound() : BadRequest(access.Error);

        var doc = access.Value;
        if (doc.AnalysisRunId is not Guid runId || runId == Guid.Empty)
            return NotFound(new { error = "Document has no analysis run." });

        var result = await researchLoader.LoadEnrichedExportAsync(doc, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/rendered-html")]
    public async Task<IActionResult> GetRenderedHtml(Guid id, CancellationToken ct)
    {
        var result = await renderer.RenderAsync(user.RequireUserId(), id, ct);
        if (!result.IsSuccess)
            return result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ? NotFound() : BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/blog-spoke")]
    public async Task<IActionResult> GetBlogSpoke(Guid id, CancellationToken ct)
    {
        var result = await blogSpoke.GetAsync(user.RequireUserId(), id, ct);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound();
            if (result.Error?.Contains("No blog version yet", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(new { spoke = result.Value!.Spoke, clusterDocumentId = result.Value.ClusterDocumentId });
    }

    [HttpPut("{id:guid}/blog-spoke")]
    public async Task<IActionResult> SaveBlogSpoke(
        Guid id, [FromBody] ContentBlogSpoke spoke, CancellationToken ct)
    {
        var result = await blogSpoke.SaveAsync(user.RequireUserId(), id, spoke, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/blog-spoke/generate")]
    public async Task<IActionResult> GenerateBlogSpoke(
        Guid id, [FromBody] GenerateBlogSpokeRequest request, CancellationToken ct)
    {
        var result = await blogSpoke.GenerateAsync(user.RequireUserId(), id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/blog-spoke/add-faqs")]
    public async Task<IActionResult> AddBlogSpokeFaqs(Guid id, CancellationToken ct)
    {
        var result = await blogSpoke.AddFaqsAsync(user.RequireUserId(), id, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/faqs/generate-linked")]
    public async Task<IActionResult> GenerateLinkedFaqs(Guid id, CancellationToken ct)
    {
        var result = await linkedFaqs.GenerateLinkedFaqsAsync(user.RequireUserId(), id, ct);
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/body-links/apply")]
    public async Task<IActionResult> ApplyBodyLinks(
        Guid id, [FromBody] ApplyBodyLinksRequest? request, CancellationToken ct)
    {
        var result = await bodyLinks.ApplyAsync(
            user.RequireUserId(),
            id,
            request ?? new ApplyBodyLinksRequest(),
            ct);
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/cluster/plan")]
    public async Task<IActionResult> GetClusterPlan(Guid id, CancellationToken ct)
    {
        var result = await clusterPlan.GetAsync(user.RequireUserId(), id, ct);
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/cluster/plan")]
    public async Task<IActionResult> SaveClusterPlan(
        Guid id, [FromBody] ContentLinkPlan plan, CancellationToken ct)
    {
        if (plan is null)
            return BadRequest(new { error = "Plan body is required" });

        var result = await clusterPlan.SaveAsync(user.RequireUserId(), id, plan, ct);
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/cluster/plan")]
    public async Task<IActionResult> BuildClusterPlan(Guid id, CancellationToken ct)
    {
        var result = await clusterPlan.BuildAsync(user.RequireUserId(), id, ct);
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/spokes")]
    public async Task<IActionResult> ListSpokes(Guid id, CancellationToken ct)
    {
        var result = await spokes.ListAsync(user.RequireUserId(), id, ct);
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/spokes")]
    public async Task<IActionResult> CreateSpoke(
        Guid id, [FromBody] CreateContentSpokeRequest request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required" });

        var result = await spokes.CreateAsync(user.RequireUserId(), id, request, ct);
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPost("{id:guid}/spokes/{spokeId:guid}/generate")]
    public async Task<IActionResult> GenerateSpoke(
        Guid id,
        Guid spokeId,
        [FromBody] GenerateContentSpokeRequest? request,
        CancellationToken ct)
    {
        var result = await spokes.GenerateAsync(user.RequireUserId(), id, spokeId, request, ct);
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/spokes/generate-all")]
    public async Task<IActionResult> GenerateAllSpokes(Guid id, CancellationToken ct)
    {
        var result = await spokes.GenerateAllAsync(user.RequireUserId(), id, ct);
        if (!result.IsSuccess)
        {
            return result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContentDocumentRequest request, CancellationToken ct)
    {
        var result = await content.CreateAsync(user.RequireUserId(), request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}/content")]
    public async Task<IActionResult> UpdateContent(Guid id, [FromBody] UpdateContentRequest request, CancellationToken ct)
    {
        var result = await content.UpdateContentAsync(user.RequireUserId(), id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateDocumentStatusBody body, CancellationToken ct)
    {
        var result = await content.UpdateStatusAsync(user.RequireUserId(), id, body.Status, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPatch("{id:guid}/analysis-run")]
    public async Task<IActionResult> AttachAnalysisRun(Guid id, [FromBody] AttachAnalysisRunRequest request, CancellationToken ct)
    {
        var result = await researchWriting.AttachResearchAsync(user.RequireUserId(), id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/draft")]
    public async Task<IActionResult> DraftFromResearch(Guid id, CancellationToken ct)
    {
        var result = await researchWriting.DraftFromResearchAsync(user.RequireUserId(), id, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/draft-job/keyword")]
    public async Task<IActionResult> EnqueueKeywordDraft(
        Guid id, [FromBody] KeywordContentDraftRequest request, CancellationToken ct)
    {
        var result = await draftJobs.EnqueueKeywordDraftAsync(user.RequireUserId(), id, request, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        draftJobChannel.Notify();
        return Accepted(result.Value);
    }

    [HttpPost("{id:guid}/draft-job/research")]
    public async Task<IActionResult> EnqueueResearchDraft(Guid id, CancellationToken ct)
    {
        var result = await draftJobs.EnqueueResearchDraftAsync(user.RequireUserId(), id, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        draftJobChannel.Notify();
        return Accepted(result.Value);
    }

    [HttpPost("{id:guid}/featured-image")]
    public async Task<IActionResult> GenerateFeaturedImage(
        Guid id,
        [FromBody] GenerateFeaturedImageRequest? request,
        CancellationToken ct)
    {
        var result = await featuredImages.GenerateForDocumentAsync(
            user.RequireUserId(),
            id,
            request ?? new GenerateFeaturedImageRequest(),
            ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await content.DeleteAsync(user.RequireUserId(), id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}/competitors")]
    public async Task<IActionResult> GetCompetitors(Guid id, CancellationToken ct)
    {
        var result = await competitors.GetForDocumentAsync(user.RequireUserId(), id, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/competitors/crawl")]
    public async Task<IActionResult> RefreshCompetitorCrawl(Guid id, CancellationToken ct)
    {
        var result = await competitors.RefreshCrawlForDocumentAsync(user.RequireUserId(), id, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/score")]
    public async Task<IActionResult> Score(Guid id, [FromBody] ScoreContentRequest? request, CancellationToken ct)
    {
        var userId = user.RequireUserId();
        Result<ContentScoreHubResult> result;

        if (string.IsNullOrWhiteSpace(request?.ContentHtml))
        {
            result = await scoring.ScoreSavedDocumentAsync(userId, id, request?.TargetKeyword, ct);
        }
        else
        {
            var doc = await content.GetAsync(userId, id, ct);
            if (!doc.IsSuccess || doc.Value is null)
                return doc.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ? NotFound() : BadRequest(doc.Error);

            var keyword = string.IsNullOrWhiteSpace(request.TargetKeyword)
                ? doc.Value.TargetKeyword
                : request.TargetKeyword;
            result = await scoring.ProcessContentChangedAsync(userId, id, request.ContentHtml, keyword, ct);
        }

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        if (result.Value?.PendingReason is not null)
            return Accepted(new { pendingReason = result.Value.PendingReason });

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/auto-optimize")]
    public async Task<IActionResult> AutoOptimize(Guid id, CancellationToken ct)
    {
        var result = await scoring.AutoOptimizeAsync(user.RequireUserId(), id, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/apply-suggestion")]
    public async Task<IActionResult> ApplySuggestion(Guid id, [FromBody] ApplySuggestionRequest request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required" });

        var result = await scoring.ApplySuggestionAsync(
            user.RequireUserId(),
            id,
            request.SuggestionId,
            request.ContentHtml,
            ct);

        if (!result.IsSuccess || result.Value is null)
            return BadRequest(new { error = result.Error, suggestionId = request.SuggestionId });

        return Ok(result.Value.Result);
    }

    [HttpPost("{id:guid}/insert-citation")]
    public async Task<IActionResult> InsertCitation(Guid id, [FromBody] InsertResearchCitationRequest request, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new { error = "url is required" });

        var result = await scoring.InsertResearchCitationAsync(
            user.RequireUserId(),
            id,
            request.Url,
            request.Title,
            request.ContentHtml,
            ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/social/generate")]
    public async Task<IActionResult> GenerateSocialPosts(
        Guid id, [FromBody] GenerateSocialPostRequest request, CancellationToken ct)
    {
        var result = await socialPosts.GenerateAsync(user.RequireUserId(), id, request ?? new GenerateSocialPostRequest(), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}

public sealed record UpdateDocumentStatusBody
{
    public required string Status { get; init; }
}


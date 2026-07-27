using ContentWriterV3.Infrastructure.Data;
using ContentWriterV3.Infrastructure.Jobs;
using ContentWriterV3.Infrastructure.Jobs.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ContentWriterV3.Api.Controllers;

[ApiController]
[Route("api/content-writer/v3")]
public class V3DraftingController : ControllerBase
{
    private readonly ContentWriterV3DbContext _db;

    public V3DraftingController(ContentWriterV3DbContext db)
    {
        _db = db;
    }

    [HttpPost("drafts/initiate")]
    public async Task<IActionResult> InitiateDraft([FromBody] InitiateDraftRequest request)
    {
        // Get campaign ID from strategy brief
        var brief = await _db.StrategyBriefs.FirstOrDefaultAsync(sb => sb.Id == request.StrategyBriefId);
        if (brief == null) return BadRequest("Strategy brief not found");

        // Create job to draft content
        var payload = new DraftContentPayload
        {
            StrategyBriefId = request.StrategyBriefId,
            ContentAssetId = request.ContentAssetId,
            ResearchRunId = request.ResearchRunId
        };

        var job = new Job(
            brief.CampaignId,
            "DraftContent",
            JsonSerializer.Serialize(payload),
            $"draft-{request.StrategyBriefId}-{request.ContentAssetId}"
        );

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();

        return AcceptedAtAction(nameof(GetDraftStatus), new { jobId = job.Id }, new { jobId = job.Id });
    }

    [HttpGet("drafts/status/{jobId}")]
    public async Task<IActionResult> GetDraftStatus(Guid jobId)
    {
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null) return NotFound();

        return Ok(new
        {
            jobId = job.Id,
            status = job.Status.ToString(),
            completedAt = job.CompletedAt,
            errorCode = job.ErrorCode,
            errorMessage = job.ErrorMessage
        });
    }

    [HttpGet("drafts/{assetId}/latest")]
    public async Task<IActionResult> GetLatestDraft(Guid assetId)
    {
        var draft = await _db.ContentAssetVersions
            .Where(cav => cav.AssetId == assetId)
            .OrderByDescending(cav => cav.CreatedAt)
            .FirstOrDefaultAsync();

        if (draft == null) return NotFound();

        var warnings = JsonSerializer.Deserialize<List<string>>(draft.ValidationWarningsJson) ?? new();
        var recommendations = JsonSerializer.Deserialize<List<string>>(draft.ValidationRecommendationsJson) ?? new();

        return Ok(new
        {
            id = draft.Id,
            assetId = draft.AssetId,
            status = draft.Status,
            content = draft.BodyDocumentJson,
            warnings = warnings,
            recommendations = recommendations,
            createdAt = draft.CreatedAt
        });
    }
}

public class InitiateDraftRequest
{
    public Guid StrategyBriefId { get; set; }
    public Guid ContentAssetId { get; set; }
    public Guid ResearchRunId { get; set; }
}

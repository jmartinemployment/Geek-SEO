using Microsoft.AspNetCore.Mvc;
using ContentWriterV3.Domain.Entities;
using ContentWriterV3.Infrastructure.Data;
using ContentWriterV3.Api.Dtos;
using ContentWriterV3.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace ContentWriterV3.Api.Controllers;

[ApiController]
[Route("api/content-writer/v3/strategy-briefs")]
public class V3StrategyBriefsController : ControllerBase
{
    private readonly ContentWriterV3DbContext _dbContext;
    private readonly IStrategyBriefApprovalValidator _validator;
    private readonly IContentPlanService _planService;

    public V3StrategyBriefsController(
        ContentWriterV3DbContext dbContext,
        IStrategyBriefApprovalValidator validator,
        IContentPlanService planService)
    {
        _dbContext = dbContext;
        _validator = validator;
        _planService = planService;
    }

    [HttpPost]
    public async Task<ActionResult<StrategyBriefResponse>> CreateBrief(
        CreateStrategyBriefRequest request,
        CancellationToken cancellationToken)
    {
        var brief = new StrategyBrief(
            request.CampaignId,
            request.PainPointId,
            request.ProfileVersionId,
            request.AudienceProfile,
            request.BuyingStage,
            request.Angle,
            request.CallToAction);

        // Link evidence if provided
        foreach (var evidenceId in request.LinkedEvidenceIds)
        {
            brief.EvidenceLinks.Add(new StrategyBriefEvidenceLink(brief.Id, evidenceId));
        }

        _dbContext.StrategyBriefs.Add(brief);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetBrief), new { id = brief.Id }, MapToResponse(brief));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StrategyBriefResponse>> GetBrief(
        Guid id,
        CancellationToken cancellationToken)
    {
        var brief = await _dbContext.StrategyBriefs
            .Include(b => b.EvidenceLinks)
            .Include(b => b.ApprovalHistory)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brief == null)
            return NotFound();

        return Ok(MapToResponse(brief));
    }

    [HttpGet]
    public async Task<ActionResult<List<StrategyBriefResponse>>> ListBriefs(
        [FromQuery] Guid? campaignId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.StrategyBriefs
            .Include(b => b.EvidenceLinks)
            .Include(b => b.ApprovalHistory)
            .AsQueryable();

        if (campaignId.HasValue)
            query = query.Where(b => b.CampaignId == campaignId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BriefStatus>(status, out var briefStatus))
            query = query.Where(b => b.Status == briefStatus);

        var briefs = await query.ToListAsync(cancellationToken);
        return Ok(briefs.Select(MapToResponse).ToList());
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<StrategyBriefResponse>> UpdateBrief(
        Guid id,
        UpdateStrategyBriefRequest request,
        CancellationToken cancellationToken)
    {
        var brief = await _dbContext.StrategyBriefs
            .Include(b => b.EvidenceLinks)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brief == null)
            return NotFound();

        if (!string.IsNullOrEmpty(request.AudienceProfile))
            brief.AudienceProfile = request.AudienceProfile;

        if (!string.IsNullOrEmpty(request.BuyingStage))
            brief.BuyingStage = request.BuyingStage;

        if (!string.IsNullOrEmpty(request.Angle))
            brief.Angle = request.Angle;

        if (!string.IsNullOrEmpty(request.CallToAction))
            brief.CallToAction = request.CallToAction;

        brief.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToResponse(brief));
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveBrief(
        Guid id,
        ApproveBriefRequest request,
        CancellationToken cancellationToken)
    {
        var brief = await _dbContext.StrategyBriefs
            .Include(b => b.EvidenceLinks)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brief == null)
            return NotFound();

        // Validate before approval
        var validation = _validator.ValidateForApproval(brief);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });

        brief.Approve(request.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Brief approved successfully" });
    }

    [HttpPost("{id}/return-to-research")]
    public async Task<IActionResult> ReturnToResearch(
        Guid id,
        ReturnToResearchRequest request,
        CancellationToken cancellationToken)
    {
        var brief = await _dbContext.StrategyBriefs
            .Include(b => b.ApprovalHistory)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brief == null)
            return NotFound();

        brief.ReturnToResearch(request.UserId, request.Notes ?? string.Empty);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Brief returned to research" });
    }

    private static StrategyBriefResponse MapToResponse(StrategyBrief brief) => new()
    {
        Id = brief.Id,
        CampaignId = brief.CampaignId,
        PainPointId = brief.PainPointId,
        AudienceProfile = brief.AudienceProfile,
        BuyingStage = brief.BuyingStage,
        Angle = brief.Angle,
        CallToAction = brief.CallToAction,
        Status = brief.Status.ToString(),
        EvidenceLinkCount = brief.EvidenceLinks.Count,
        CreatedAt = brief.CreatedAt,
        UpdatedAt = brief.UpdatedAt
    };
}

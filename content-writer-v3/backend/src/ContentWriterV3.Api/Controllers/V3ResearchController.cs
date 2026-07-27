using Microsoft.AspNetCore.Mvc;
using ContentWriterV3.Domain.Entities;
using ContentWriterV3.Infrastructure.Data;
using ContentWriterV3.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ContentWriterV3.Api.Controllers;

[ApiController]
[Route("api/content-writer/v3/research")]
public class V3ResearchController : ControllerBase
{
    private readonly ContentWriterV3DbContext _dbContext;

    public V3ResearchController(ContentWriterV3DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("runs")]
    public async Task<ActionResult<ResearchRunResponse>> CreateResearchRun(
        CreateResearchRunRequest request,
        CancellationToken cancellationToken)
    {
        var researchRun = new ResearchRun(request.CampaignId, request.Keyword, request.MaxBudget);
        _dbContext.ResearchRuns.Add(researchRun);

        // Create job to initiate research
        var job = new Job(
            request.CampaignId,
            "InitiateResearch",
            JsonSerializer.Serialize(new { CampaignId = request.CampaignId, Keyword = request.Keyword, MaxBudget = request.MaxBudget }),
            $"research-{request.CampaignId}-{DateTime.UtcNow.Ticks}");

        _dbContext.Jobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetResearchRun), new { id = researchRun.Id }, MapToResponse(researchRun));
    }

    [HttpGet("runs/{id}")]
    public async Task<ActionResult<ResearchRunResponse>> GetResearchRun(
        Guid id,
        CancellationToken cancellationToken)
    {
        var run = await _dbContext.ResearchRuns.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (run == null)
            return NotFound();

        return Ok(MapToResponse(run));
    }

    [HttpGet("pain-points")]
    public async Task<ActionResult<List<PainPointResponse>>> GetPainPoints(
        [FromQuery] Guid? clientId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.PainPoints.AsQueryable();

        if (clientId.HasValue)
            query = query.Where(pp => pp.ClientId == clientId.Value);

        var painPoints = await query.ToListAsync(cancellationToken);
        return Ok(painPoints.Select(MapToResponse).ToList());
    }

    [HttpGet("pain-points/{id}")]
    public async Task<ActionResult<PainPointResponse>> GetPainPoint(
        Guid id,
        CancellationToken cancellationToken)
    {
        var painPoint = await _dbContext.PainPoints.FirstOrDefaultAsync(pp => pp.Id == id, cancellationToken);
        if (painPoint == null)
            return NotFound();

        return Ok(MapToResponse(painPoint));
    }

    [HttpGet("reconciliation/proposals")]
    public async Task<ActionResult<List<ReconciliationProposalResponse>>> GetReconciliationProposals(
        [FromQuery] Guid? researchRunId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ReconciliationProposals.AsQueryable();

        if (researchRunId.HasValue)
            query = query.Where(p => p.ResearchRunId == researchRunId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ProposalStatus>(status, out var proposalStatus))
            query = query.Where(p => p.Status == proposalStatus);

        var proposals = await query.ToListAsync(cancellationToken);
        return Ok(proposals.Select(MapToResponse).ToList());
    }

    [HttpPost("reconciliation/proposals/{id}/approve")]
    public async Task<IActionResult> ApproveProposal(
        Guid id,
        CancellationToken cancellationToken)
    {
        var proposal = await _dbContext.ReconciliationProposals.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (proposal == null)
            return NotFound();

        var userId = Guid.Empty; // TODO: Get from user context
        proposal.Approve(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    [HttpPost("reconciliation/proposals/{id}/dismiss")]
    public async Task<IActionResult> DismissProposal(
        Guid id,
        CancellationToken cancellationToken)
    {
        var proposal = await _dbContext.ReconciliationProposals.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (proposal == null)
            return NotFound();

        var userId = Guid.Empty; // TODO: Get from user context
        proposal.Dismiss(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    private static ResearchRunResponse MapToResponse(ResearchRun run) => new()
    {
        Id = run.Id,
        CampaignId = run.CampaignId,
        Keyword = run.Keyword,
        Status = run.Status.ToString(),
        DiscoveredSourceCount = run.DiscoveredSourceCount,
        SpentBudget = run.SpentBudget,
        MaxBudget = run.MaxBudget,
        ErrorMessage = run.ErrorMessage,
        CreatedAt = run.CreatedAt
    };

    private static PainPointResponse MapToResponse(PainPoint pp) => new()
    {
        Id = pp.Id,
        Name = pp.Name,
        Description = pp.Description,
        ReaderSymptom = pp.ReaderSymptom,
        CostOfInaction = pp.CostOfInaction,
        OfferTerminology = pp.OfferTerminology,
        Objections = pp.Objections,
        Confidence = pp.Confidence,
        StaleSince = pp.StaleSince,
        CreatedAt = pp.CreatedAt
    };

    private static ReconciliationProposalResponse MapToResponse(ReconciliationProposal proposal)
    {
        var proposedData = JsonSerializer.Deserialize<object>(proposal.ProposedDataJson) ?? new { };
        return new()
        {
            Id = proposal.Id,
            ProposalType = proposal.ProposalType.ToString(),
            Status = proposal.Status.ToString(),
            ProposedData = proposedData,
            CreatedAt = proposal.CreatedAt
        };
    }
}

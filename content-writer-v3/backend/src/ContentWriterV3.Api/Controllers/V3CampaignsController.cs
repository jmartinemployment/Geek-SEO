using Microsoft.AspNetCore.Mvc;
using ContentWriterV3.Domain.Entities;
using ContentWriterV3.Infrastructure.Data;
using ContentWriterV3.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ContentWriterV3.Api.Controllers;

[ApiController]
[Route("api/content-writer/v3/campaigns")]
public class V3CampaignsController : ControllerBase
{
    private readonly ContentWriterV3DbContext _dbContext;

    public V3CampaignsController(ContentWriterV3DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<ActionResult<CampaignResponse>> CreateCampaign(
        CreateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var campaign = new ContentCampaign(request.ClientId, request.ProfileVersionId, request.Name, request.Keyword);
        _dbContext.ContentCampaigns.Add(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, MapToResponse(campaign));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CampaignResponse>> GetCampaign(
        Guid id,
        CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.ContentCampaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (campaign == null)
            return NotFound();

        return Ok(MapToResponse(campaign));
    }

    [HttpGet]
    public async Task<ActionResult<List<CampaignResponse>>> ListCampaigns(
        [FromQuery] Guid? clientId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ContentCampaigns.AsQueryable();

        if (clientId.HasValue)
            query = query.Where(c => c.ClientId == clientId.Value);

        var campaigns = await query.ToListAsync(cancellationToken);
        return Ok(campaigns.Select(MapToResponse).ToList());
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<CampaignResponse>> UpdateCampaign(
        Guid id,
        UpdateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.ContentCampaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (campaign == null)
            return NotFound();

        if (!string.IsNullOrEmpty(request.Name))
            campaign.Name = request.Name;

        if (Enum.TryParse<CampaignStatus>(request.Status, out var status))
            campaign.Status = status;

        campaign.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToResponse(campaign));
    }

    private static CampaignResponse MapToResponse(ContentCampaign campaign) => new()
    {
        Id = campaign.Id,
        ClientId = campaign.ClientId,
        Name = campaign.Name,
        Keyword = campaign.Keyword,
        Status = campaign.Status.ToString(),
        CreatedAt = campaign.CreatedAt,
        UpdatedAt = campaign.UpdatedAt
    };
}

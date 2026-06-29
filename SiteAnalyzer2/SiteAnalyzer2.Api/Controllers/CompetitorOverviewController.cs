using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.CompetitorCrawl;

namespace SiteAnalyzer2.Api.Controllers;

[ApiController]
[Route("runs/{runId:guid}/competitor-overview")]
public class CompetitorOverviewController(
    AppDbContext db,
    CompetitorOverviewLiteService overview) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid runId, CancellationToken ct)
    {
        if (!await db.AnalysisRuns.AnyAsync(r => r.Id == runId, ct))
            return NotFound();

        var report = await overview.GetAsync(runId, ct);
        return report is null ? NotFound() : Ok(report);
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(Guid runId, CancellationToken ct)
    {
        if (!await db.AnalysisRuns.AnyAsync(r => r.Id == runId, ct))
            return NotFound();

        try
        {
            var result = await overview.AnalyzeAsync(runId, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

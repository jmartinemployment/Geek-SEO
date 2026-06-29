using Microsoft.AspNetCore.Mvc;
using SiteAnalyzer2.Services.CompetitorCrawl;

namespace SiteAnalyzer2.Api.Controllers;

[ApiController]
[Route("domain-overview")]
public class DomainOverviewController(DomainOverviewService overview) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string domain, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return BadRequest(new { error = "domain is required" });

        var report = await overview.GetAsync(domain, ct);
        return report is null ? BadRequest(new { error = "Invalid domain or URL" }) : Ok(report);
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] DomainOverviewAnalyzeRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Domain))
            return BadRequest(new { error = "domain is required" });

        var report = await overview.AnalyzeAsync(body.Domain, ct);
        return report is null ? BadRequest(new { error = "Invalid domain or URL" }) : Ok(report);
    }
}

public sealed record DomainOverviewAnalyzeRequest(string Domain);

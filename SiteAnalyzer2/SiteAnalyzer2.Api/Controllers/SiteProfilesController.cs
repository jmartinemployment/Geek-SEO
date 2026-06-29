using Microsoft.AspNetCore.Mvc;
using SiteAnalyzer2.Services.Integrations;

namespace SiteAnalyzer2.Api.Controllers;

/// <summary>Site profile bundle for Content Writer freeze (by profile id — not URL).</summary>
[ApiController]
[Route("site-profiles")]
public sealed class SiteProfilesController(ContentWriterSiteBundleService siteBundles) : ControllerBase
{
    [HttpGet("{siteProfileId:guid}/content-writer-bundle")]
    public async Task<ActionResult<ContentWriterSiteBundleDto>> GetContentWriterBundle(
        Guid siteProfileId,
        CancellationToken ct)
    {
        if (siteProfileId == Guid.Empty)
            return BadRequest(new { error = "siteProfileId is required." });

        var bundle = await siteBundles.GetByProfileIdAsync(siteProfileId, ct);
        return bundle is null
            ? NotFound(new { error = $"No site profile exists for id {siteProfileId}." })
            : Ok(bundle);
    }

    [HttpGet("by-project/{geekSeoProjectId:guid}/content-writer-bundle")]
    public async Task<ActionResult<ContentWriterSiteBundleDto>> GetContentWriterBundleByProject(
        Guid geekSeoProjectId,
        CancellationToken ct)
    {
        if (geekSeoProjectId == Guid.Empty)
            return BadRequest(new { error = "geekSeoProjectId is required." });

        var bundle = await siteBundles.GetByGeekSeoProjectIdAsync(geekSeoProjectId, ct);
        return bundle is null
            ? NotFound(new { error = $"No site profile linked to project {geekSeoProjectId}." })
            : Ok(bundle);
    }
}

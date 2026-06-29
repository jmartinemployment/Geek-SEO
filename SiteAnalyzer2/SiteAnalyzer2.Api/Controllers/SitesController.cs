using Microsoft.AspNetCore.Mvc;
using SiteAnalyzer2.Services.Integrations;
using SiteAnalyzer2.Services.ProfileAssembly;

namespace SiteAnalyzer2.Api.Controllers;

[ApiController]
[Route("sites")]
public sealed class SitesController(
    SiteProfileService siteProfiles,
    SiteProfileAssemblerService siteProfileAssembler,
    OperatorResearchService operatorResearch) : ControllerBase
{
    public sealed record CreateSiteRequest(string SiteUrl, string? DisplayName);

    public sealed record UpsertSiteResponse(SiteProfileDto Profile, bool Created);

    [HttpGet]
    public async Task<ActionResult<SiteProfileDto>> GetByUrl([FromQuery] string? siteUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(siteUrl))
            return BadRequest(new { error = "siteUrl query parameter is required." });

        var normalized = TargetSiteUrlNormalizer.Normalize(siteUrl);
        if (string.IsNullOrEmpty(normalized) || !TargetSiteUrlNormalizer.IsValidStoredFormat(normalized))
        {
            return BadRequest(new
            {
                error = "Invalid project URL. Required format: https://www.{domain}/ (lowercase, trailing slash).",
            });
        }

        var profile = await siteProfiles.GetDetailByUrlAsync(normalized, ct);
        return profile is null
            ? NotFound(new { error = $"No site profile exists for {normalized}." })
            : Ok(profile);
    }

    [HttpGet("content-pillars")]
    public async Task<ActionResult<IReadOnlyList<ContentPillarDto>>> ListContentPillars(
        [FromQuery] string? siteUrl,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(siteUrl))
            return BadRequest(new { error = "siteUrl query parameter is required." });

        var normalized = TargetSiteUrlNormalizer.Normalize(siteUrl);
        if (string.IsNullOrEmpty(normalized) || !TargetSiteUrlNormalizer.IsValidStoredFormat(normalized))
        {
            return BadRequest(new
            {
                error = "Invalid project URL. Required format: https://www.{domain}/ (lowercase, trailing slash).",
            });
        }

        var pillars = await operatorResearch.ListContentPillarsAsync(normalized, ct);
        return Ok(pillars);
    }

    [HttpPost]
    public async Task<ActionResult<UpsertSiteResponse>> Create(
        [FromBody] CreateSiteRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SiteUrl))
            return BadRequest(new { error = "siteUrl is required." });

        try
        {
            var result = await siteProfiles.CreateOrGetAsync(request.SiteUrl, request.DisplayName, ct);
            await siteProfileAssembler.AssembleFromHomepageAsync(result.Id, ct);
            var profile = await siteProfiles.GetDetailByUrlAsync(result.SiteUrl, ct)
                ?? throw new InvalidOperationException($"Site profile {result.SiteUrl} was not found after save.");

            return Ok(new UpsertSiteResponse(profile, result.Created));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

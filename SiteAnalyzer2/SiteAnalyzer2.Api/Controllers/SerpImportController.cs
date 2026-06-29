using Microsoft.AspNetCore.Mvc;
using SiteAnalyzer2.Services.Integrations;

namespace SiteAnalyzer2.Api.Controllers;

/// <summary>
/// Operator import: Chrome-saved HTML only — keyword read from the page, no pre-created run.
/// </summary>
[ApiController]
[Route("imports")]
public sealed class SerpImportController(KeywordWorkflowService workflow) : ControllerBase
{
    [HttpPost("serp-html")]
    public async Task<ActionResult<SerpHtmlImportResultDto>> ImportSerpHtml(
        [FromQuery] Guid projectId,
        [FromQuery] string? targetSiteUrl,
        CancellationToken ct)
    {
        if (projectId == Guid.Empty)
            return BadRequest(new { error = "projectId query parameter is required (use your Geek-SEO project id)." });

        using var reader = new StreamReader(Request.Body);
        var html = await reader.ReadToEndAsync(ct);
        if (string.IsNullOrWhiteSpace(html))
            return BadRequest(new { error = "Request body must contain saved Google SERP HTML." });

        try
        {
            var normalizedUrl = TargetSiteUrlNormalizer.Normalize(targetSiteUrl);
            if (string.IsNullOrEmpty(normalizedUrl) || !TargetSiteUrlNormalizer.IsValidStoredFormat(normalizedUrl))
            {
                return BadRequest(new
                {
                    error = "Invalid project URL. Required format: https://www.{domain}/ (lowercase, trailing slash).",
                });
            }

            var result = await workflow.ImportKeywordPageAsync(projectId, normalizedUrl, html, ct);
            if (!result.KeywordSaved)
                return UnprocessableEntity(result);

            return Ok(new SerpHtmlImportResultDto
            {
                RunId = result.KeywordProjectId,
                ProjectId = projectId,
                Keyword = result.Keyword,
                TargetSiteUrl = normalizedUrl,
                OrganicCount = result.OrganicCount,
                OrganicOnlyCount = result.OrganicOnlyCount,
                PaidCount = result.PaidCount,
                AiOverviewCount = result.AiOverviewCount,
                AiOverviewAvailable = result.AiOverviewAvailable,
                PaaCount = result.PaaCount,
                CompetitorCrawlSeedCount = result.CompetitorCrawlSeedCount,
                GatePassed = true,
                GateMessage = result.Message ?? string.Empty,
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

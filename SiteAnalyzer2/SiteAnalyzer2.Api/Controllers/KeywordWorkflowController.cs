using Microsoft.AspNetCore.Mvc;
using SiteAnalyzer2.Services.Integrations;

namespace SiteAnalyzer2.Api.Controllers;

/// <summary>Operator keyword import workflow: transactional save/verify; commit on success only.</summary>
[ApiController]
[Route("imports")]
public sealed class KeywordWorkflowController(KeywordWorkflowService workflow) : ControllerBase
{
    [HttpPost("keyword-page")]
    public async Task<ActionResult<KeywordPageImportResultDto>> ImportKeywordPage(
        [FromQuery] string? targetSiteUrl,
        [FromQuery] string? topic,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(targetSiteUrl))
            return BadRequest(new { error = "targetSiteUrl query parameter is required (your Geek-SEO site URL)." });

        var normalizedUrl = TargetSiteUrlNormalizer.Normalize(targetSiteUrl);
        if (string.IsNullOrEmpty(normalizedUrl) || !TargetSiteUrlNormalizer.IsValidStoredFormat(normalizedUrl))
        {
            return BadRequest(new
            {
                error = "Invalid project URL. Required format: https://www.{domain}/ (lowercase, trailing slash).",
            });
        }

        using var reader = new StreamReader(Request.Body);
        var html = await reader.ReadToEndAsync(ct);
        if (string.IsNullOrWhiteSpace(html))
            return BadRequest(new { error = "Request body must contain saved Google SERP HTML." });

        try
        {
            var result = await workflow.ImportKeywordPageAsync(normalizedUrl, html, topic, ct);
            if (!result.KeywordSaved)
                return UnprocessableEntity(result);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("keyword-page/summary")]
    public async Task<ActionResult<KeywordPageImportResultDto>> GetKeywordImportSummary(
        [FromQuery] Guid keywordProjectId,
        CancellationToken ct)
    {
        if (keywordProjectId == Guid.Empty)
            return BadRequest(new { error = "keywordProjectId query parameter is required." });

        var summary = await workflow.GetKeywordImportSummaryAsync(keywordProjectId, ct);
        if (summary is null)
            return NotFound(new { error = "No keyword import data found for this run." });

        return Ok(summary);
    }
}

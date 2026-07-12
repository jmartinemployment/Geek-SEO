using ContentWriter.Application.DTOs;
using ContentWriter.Application.Providers;
using ContentWriter.Application.Services;
using ContentWriter.Application.Services.Figures;
using Microsoft.AspNetCore.Mvc;

namespace ContentWriter.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/generate")]
public class GenerateController : ControllerBase
{
    private readonly IContentGenerationOrchestrator _orchestrator;
    private readonly ILogger<GenerateController> _logger;

    public GenerateController(IContentGenerationOrchestrator orchestrator, ILogger<GenerateController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpPost("pillar/plan")]
    public Task<IActionResult> GeneratePillarPlan(Guid projectId, CancellationToken cancellationToken) =>
        RunStep(projectId, _orchestrator.GeneratePillarPlanAsync(projectId, cancellationToken), "pillar-plan");

    [HttpPost("pillar/body")]
    public Task<IActionResult> GeneratePillarBody(Guid projectId, CancellationToken cancellationToken) =>
        RunStep(projectId, _orchestrator.GeneratePillarBodyAsync(projectId, cancellationToken), "pillar-body");

    [HttpPost("pillar")]
    public Task<IActionResult> GeneratePillar(Guid projectId, CancellationToken cancellationToken) =>
        RunStep(projectId, _orchestrator.GeneratePillarAsync(projectId, cancellationToken), "pillar");

    [HttpPost("blog")]
    public Task<IActionResult> GenerateBlog(Guid projectId, CancellationToken cancellationToken) =>
        RunStep(projectId, _orchestrator.GenerateBlogAsync(projectId, cancellationToken), "blog");

    [HttpPost("tools")]
    public Task<IActionResult> GenerateTools(Guid projectId, CancellationToken cancellationToken) =>
        RunStep(projectId, _orchestrator.GenerateToolPagesAsync(projectId, cancellationToken), "tools");

    [HttpPost("images")]
    public Task<IActionResult> GenerateImages(Guid projectId, CancellationToken cancellationToken) =>
        RunStep(projectId, _orchestrator.GenerateImagesAsync(projectId, cancellationToken), "images");

    [HttpPost("social")]
    public Task<IActionResult> GenerateSocial(Guid projectId, CancellationToken cancellationToken) =>
        RunStep(projectId, _orchestrator.GenerateSocialAsync(projectId, cancellationToken), "social");

    [HttpPost("email-cold-outreach")]
    public Task<IActionResult> GenerateColdOutreach(Guid projectId, CancellationToken cancellationToken) =>
        RunStep(projectId, _orchestrator.GenerateColdOutreachAsync(projectId, cancellationToken), "email-cold-outreach");

    [HttpPost("image-prompts")]
    public Task<IActionResult> GenerateImagePrompts(
        Guid projectId,
        [FromQuery] bool confirmRegenerateWithArt = false,
        CancellationToken cancellationToken = default) =>
        RunStep(
            projectId,
            _orchestrator.GenerateImagePromptsAsync(projectId, confirmRegenerateWithArt, cancellationToken),
            "image-prompts");

    [HttpPost]
    public Task<IActionResult> GenerateAll(
        Guid projectId,
        [FromQuery] bool confirmRegenerateWithArt = false,
        CancellationToken cancellationToken = default) =>
        RunStep(
            projectId,
            _orchestrator.GenerateAllAsync(projectId, confirmRegenerateWithArt, cancellationToken),
            "all");

    private async Task<IActionResult> RunStep(Guid projectId, Task<GeneratedContentSet> action, string step)
    {
        try
        {
            var result = await action;
            return Ok(result);
        }
        catch (FigureRegenerationBlockedException ex)
        {
            _logger.LogWarning(
                "Figure brief regeneration blocked for project {ProjectId}: Ready={ReadyCount} Published={PublishedCount}",
                projectId,
                ex.ReadyCount,
                ex.PublishedCount);
            return Conflict(new
            {
                title = "Figure art would be affected",
                detail = ex.Message,
                readyCount = ex.ReadyCount,
                publishedCount = ex.PublishedCount,
                confirmQueryParam = "confirmRegenerateWithArt",
            });
        }
        catch (ContentGenerationException ex)
        {
            _logger.LogWarning(ex, "Content generation step {Step} failed for project {ProjectId}", step, projectId);
            var title = ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                ? "Generation timed out"
                : "Content generation failed";
            return Problem(ex.Message, statusCode: 502, title: title);
        }
    }
}

using ContentWriter.Application.Providers;
using ContentWriter.Application.Services.Publish;
using Microsoft.AspNetCore.Mvc;

namespace ContentWriter.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/publish")]
public class PublishController : ControllerBase
{
    private readonly IGeekBlogPublishService _publishService;
    private readonly ILogger<PublishController> _logger;

    public PublishController(IGeekBlogPublishService publishService, ILogger<PublishController> logger)
    {
        _publishService = publishService;
        _logger = logger;
    }

    [HttpPost("site")]
    public async Task<IActionResult> PublishToSite(
        Guid projectId,
        [FromBody] PublishToSiteRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _publishService.PublishAsync(
                projectId,
                request?.Department,
                cancellationToken);

            return Ok(new PublishToSiteResponse(
                result.Department,
                result.Posts.Select(p => new PublishedGeekPostResponse(
                    p.PostType,
                    p.Slug,
                    p.PostId,
                    p.Created,
                    p.PublicPath)).ToList()));
        }
        catch (ContentGenerationException ex)
        {
            _logger.LogWarning(ex, "Publish failed for project {ProjectId}", projectId);
            return Problem(ex.Message, statusCode: 400, title: "Publish failed");
        }
    }
}

public sealed record PublishToSiteRequest(string? Department);

public sealed record PublishToSiteResponse(string Department, IReadOnlyList<PublishedGeekPostResponse> Posts);

public sealed record PublishedGeekPostResponse(
    string PostType,
    string Slug,
    int PostId,
    bool Created,
    string PublicPath);

using ContentWriterV3.Application.Services;
using ContentWriterV3.Domain.Entities;
using ContentWriterV3.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentWriterV3.Api.Controllers;

[ApiController]
[Route("api/content-writer/v3/publications")]
public class V3PublicationController : ControllerBase
{
    private readonly ContentWriterV3DbContext _db;
    private readonly IPublicationService _publicationService;

    public V3PublicationController(ContentWriterV3DbContext db, IPublicationService publicationService)
    {
        _db = db;
        _publicationService = publicationService;
    }

    [HttpPost("queue")]
    public async Task<IActionResult> QueueForPublishing([FromBody] QueuePublicationRequest request)
    {
        var review = await _db.ContentReviews.FirstOrDefaultAsync(r => r.Id == request.ReviewId);
        if (review == null) return NotFound("Review not found");

        if (review.Status != ReviewStatus.Approved)
            return BadRequest("Only approved reviews can be published");

        var publication = await _publicationService.QueueForPublishing(
            request.ReviewId,
            request.PublicationTarget,
            request.QueuedByUserId
        );

        publication.ContentTitle = request.ContentTitle;

        _db.Publications.Add(publication);
        await _db.SaveChangesAsync();

        return AcceptedAtAction(nameof(GetPublication), new { publicationId = publication.Id }, new { publicationId = publication.Id });
    }

    [HttpGet("{publicationId}")]
    public async Task<IActionResult> GetPublication(Guid publicationId)
    {
        var publication = await _db.Publications
            .Include(p => p.PublicationEvents ?? new List<PublicationEvent>())
            .FirstOrDefaultAsync(p => p.Id == publicationId);

        if (publication == null) return NotFound();

        return Ok(new
        {
            id = publication.Id,
            assetVersionId = publication.AssetVersionId,
            reviewId = publication.ReviewId,
            contentTitle = publication.ContentTitle,
            status = publication.Status.ToString(),
            publishedUrl = publication.PublishedUrl,
            publishedAt = publication.PublishedAt,
            targetPlatform = publication.PublicationTarget,
            scheduledPublishAt = publication.ScheduledPublishAt,
            retryCount = publication.RetryCount,
            failureReason = publication.FailureReason,
            events = publication.PublicationEvents?.Select(e => new
            {
                id = e.Id,
                eventType = e.EventType.ToString(),
                occurredAt = e.OccurredAt,
                details = e.Details
            })
        });
    }

    [HttpPost("{publicationId}/publish-now")]
    public async Task<IActionResult> PublishNow(Guid publicationId)
    {
        var publication = await _db.Publications.FirstOrDefaultAsync(p => p.Id == publicationId);
        if (publication == null) return NotFound("Publication not found");

        if (publication.Status != PublicationStatus.Queued && publication.Status != PublicationStatus.Failed)
            return BadRequest("Can only publish Queued or Failed publications");

        var result = await _publicationService.PublishNow(publicationId);

        if (result.Success)
        {
            publication.MarkPublished(result.PublishedUrl ?? "", "");
            var publishEvent = new PublicationEvent(publicationId, PublicationEventType.PublishedSuccessfully);
            _db.PublicationEvents.Add(publishEvent);
        }
        else
        {
            publication.MarkFailed(result.ErrorMessage ?? "Unknown error");
            var failEvent = new PublicationEvent(publicationId, PublicationEventType.PublishFailed, result.ErrorMessage);
            _db.PublicationEvents.Add(failEvent);
        }

        _db.Publications.Update(publication);
        await _db.SaveChangesAsync();

        return Ok(new { status = result.Success ? "Published" : "Failed", url = result.PublishedUrl });
    }

    [HttpPost("{publicationId}/schedule")]
    public async Task<IActionResult> SchedulePublishing(Guid publicationId, [FromBody] SchedulePublicationRequest request)
    {
        var publication = await _db.Publications.FirstOrDefaultAsync(p => p.Id == publicationId);
        if (publication == null) return NotFound("Publication not found");

        await _publicationService.ScheduleForPublishing(publicationId, request.ScheduledAt);

        publication.MarkScheduled(request.ScheduledAt);
        var scheduleEvent = new PublicationEvent(publicationId, PublicationEventType.ScheduledForPublish);
        _db.PublicationEvents.Add(scheduleEvent);

        _db.Publications.Update(publication);
        await _db.SaveChangesAsync();

        return Ok(new { status = "Scheduled", scheduledAt = request.ScheduledAt });
    }

    [HttpPost("{publicationId}/retry")]
    public async Task<IActionResult> RetryPublish(Guid publicationId)
    {
        var publication = await _db.Publications.FirstOrDefaultAsync(p => p.Id == publicationId);
        if (publication == null) return NotFound("Publication not found");

        if (publication.Status != PublicationStatus.Failed)
            return BadRequest("Can only retry failed publications");

        var result = await _publicationService.Retry(publicationId);

        publication.Status = PublicationStatus.Queued;
        var retryEvent = new PublicationEvent(publicationId, PublicationEventType.PublishRetried);
        _db.PublicationEvents.Add(retryEvent);

        _db.Publications.Update(publication);
        await _db.SaveChangesAsync();

        return Ok(new { status = "Queued for retry" });
    }

    [HttpPost("{publicationId}/unpublish")]
    public async Task<IActionResult> UnpublishContent(Guid publicationId)
    {
        var publication = await _db.Publications.FirstOrDefaultAsync(p => p.Id == publicationId);
        if (publication == null) return NotFound("Publication not found");

        if (publication.Status != PublicationStatus.Published)
            return BadRequest("Can only unpublish published content");

        publication.Status = PublicationStatus.Unpublished;
        var unpublishEvent = new PublicationEvent(publicationId, PublicationEventType.UnpublishedSuccessfully);
        _db.PublicationEvents.Add(unpublishEvent);

        _db.Publications.Update(publication);
        await _db.SaveChangesAsync();

        return Ok(new { status = "Unpublished" });
    }
}

public class QueuePublicationRequest
{
    public Guid ReviewId { get; set; }
    public string ContentTitle { get; set; } = string.Empty;
    public string PublicationTarget { get; set; } = string.Empty;
    public Guid QueuedByUserId { get; set; }
}

public class SchedulePublicationRequest
{
    public DateTime ScheduledAt { get; set; }
}

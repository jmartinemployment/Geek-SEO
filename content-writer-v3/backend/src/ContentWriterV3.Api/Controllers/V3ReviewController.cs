using ContentWriterV3.Application.Services;
using ContentWriterV3.Domain.Entities;
using ContentWriterV3.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ContentWriterV3.Api.Controllers;

[ApiController]
[Route("api/content-writer/v3/reviews")]
public class V3ReviewController : ControllerBase
{
    private readonly ContentWriterV3DbContext _db;
    private readonly IReviewService _reviewService;

    public V3ReviewController(ContentWriterV3DbContext db, IReviewService reviewService)
    {
        _db = db;
        _reviewService = reviewService;
    }

    [HttpPost("{assetVersionId}/initiate")]
    public async Task<IActionResult> InitiateReview(Guid assetVersionId, [FromBody] InitiateReviewRequest request)
    {
        var assetVersion = await _db.ContentAssetVersions.FirstOrDefaultAsync(cav => cav.Id == assetVersionId);
        if (assetVersion == null) return NotFound("Asset version not found");

        var review = await _reviewService.InitiateReview(assetVersionId, request.ReviewerUserId);
        _db.ContentReviews.Add(review);
        await _db.SaveChangesAsync();

        return AcceptedAtAction(nameof(GetReview), new { reviewId = review.Id }, new { reviewId = review.Id });
    }

    [HttpGet("{reviewId}")]
    public async Task<IActionResult> GetReview(Guid reviewId)
    {
        var review = await _db.ContentReviews
            .Include(r => r.Comments)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null) return NotFound();

        return Ok(new
        {
            id = review.Id,
            assetVersionId = review.AssetVersionId,
            reviewedByUserId = review.ReviewedByUserId,
            status = review.Status.ToString(),
            accuracyScore = review.AccuracyScore,
            strengthScore = review.StrengthScore,
            alignmentScore = review.AlignmentScore,
            editorSummary = review.EditorSummary,
            comments = review.Comments.Select(c => new
            {
                id = c.Id,
                section = c.Section,
                lineNumber = c.LineNumber,
                type = c.Type.ToString(),
                severity = c.Severity.ToString(),
                text = c.CommentText,
                resolved = c.Resolved,
                resolution = c.Resolution
            }),
            reviewedAt = review.ReviewedAt,
            approvedAt = review.ApprovedAt,
            rejectionReason = review.RejectionReason
        });
    }

    [HttpPost("{reviewId}/comment")]
    public async Task<IActionResult> AddComment(Guid reviewId, [FromBody] AddCommentRequest request)
    {
        var review = await _db.ContentReviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review == null) return NotFound("Review not found");

        var comment = await _reviewService.AddComment(
            reviewId,
            request.AuthorUserId,
            request.Section,
            request.CommentText,
            Enum.Parse<CommentType>(request.Type),
            Enum.Parse<CommentSeverity>(request.Severity)
        );

        _db.ReviewComments.Add(comment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetReview), new { reviewId }, new { commentId = comment.Id });
    }

    [HttpPatch("{reviewId}/score")]
    public async Task<IActionResult> UpdateScores(Guid reviewId, [FromBody] UpdateScoresRequest request)
    {
        var review = await _db.ContentReviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review == null) return NotFound("Review not found");

        await _reviewService.UpdateScores(reviewId, request.AccuracyScore, request.StrengthScore, request.AlignmentScore);

        review.AccuracyScore = request.AccuracyScore;
        review.StrengthScore = request.StrengthScore;
        review.AlignmentScore = request.AlignmentScore;

        _db.ContentReviews.Update(review);
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{reviewId}/approve")]
    public async Task<IActionResult> ApproveReview(Guid reviewId, [FromBody] ApproveReviewRequest request)
    {
        var review = await _db.ContentReviews
            .Include(r => r.Comments)
            .FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review == null) return NotFound("Review not found");

        // Check for unresolved Blocker comments
        var blockers = review.Comments
            .Where(c => c.Severity == CommentSeverity.Blocker && !c.Resolved)
            .ToList();

        if (blockers.Any())
        {
            return BadRequest(new
            {
                error = "Cannot approve with unresolved Blocker comments",
                blockerCount = blockers.Count,
                blockers = blockers.Select(b => new { section = b.Section, text = b.CommentText })
            });
        }

        var decision = await _reviewService.ApproveReview(reviewId, request.EditorSummary);

        review.Approve(request.EditorSummary);
        _db.ContentReviews.Update(review);
        await _db.SaveChangesAsync();

        return Ok(new { status = "Approved", approvedAt = review.ApprovedAt });
    }

    [HttpPost("{reviewId}/request-changes")]
    public async Task<IActionResult> RequestChanges(Guid reviewId, [FromBody] RequestChangesRequest request)
    {
        var review = await _db.ContentReviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review == null) return NotFound("Review not found");

        review.RequestChanges();
        _db.ContentReviews.Update(review);
        await _db.SaveChangesAsync();

        return Ok(new { status = "ChangesRequested" });
    }

    [HttpPost("{reviewId}/reject")]
    public async Task<IActionResult> RejectReview(Guid reviewId, [FromBody] RejectReviewRequest request)
    {
        var review = await _db.ContentReviews.FirstOrDefaultAsync(r => r.Id == reviewId);
        if (review == null) return NotFound("Review not found");

        review.Reject(request.Reason);
        _db.ContentReviews.Update(review);
        await _db.SaveChangesAsync();

        return Ok(new { status = "Rejected", reason = request.Reason });
    }
}

public class InitiateReviewRequest
{
    public Guid ReviewerUserId { get; set; }
}

public class AddCommentRequest
{
    public Guid AuthorUserId { get; set; }
    public string Section { get; set; } = string.Empty;
    public int? LineNumber { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public string Type { get; set; } = "Factual";
    public string Severity { get; set; } = "Minor";
}

public class UpdateScoresRequest
{
    public int AccuracyScore { get; set; }
    public int StrengthScore { get; set; }
    public int AlignmentScore { get; set; }
}

public class ApproveReviewRequest
{
    public string? EditorSummary { get; set; }
}

public class RequestChangesRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class RejectReviewRequest
{
    public string Reason { get; set; } = string.Empty;
}

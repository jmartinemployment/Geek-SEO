using ContentWriterV3.Domain.Entities;

namespace ContentWriterV3.Application.Services;

public interface IReviewService
{
    Task<ContentReview> InitiateReview(Guid assetVersionId, Guid reviewerId);
    Task<ReviewComment> AddComment(Guid reviewId, Guid authorId, string section, string text, CommentType type, CommentSeverity severity);
    Task UpdateScores(Guid reviewId, int accuracy, int strength, int alignment);
    Task<ReviewDecision> ApproveReview(Guid reviewId, string? summary = null);
    Task<ReviewDecision> RequestChanges(Guid reviewId, string reason);
    Task<ReviewDecision> RejectReview(Guid reviewId, string reason);
    Task<bool> CanApprove(Guid reviewId); // Check for blockers
}

public class ReviewService : IReviewService
{
    public Task<ContentReview> InitiateReview(Guid assetVersionId, Guid reviewerId)
    {
        var review = new ContentReview(assetVersionId, reviewerId);
        // Will be persisted by caller
        return Task.FromResult(review);
    }

    public Task<ReviewComment> AddComment(Guid reviewId, Guid authorId, string section, string text, CommentType type, CommentSeverity severity)
    {
        var comment = new ReviewComment(reviewId, authorId, section, text, type, severity);
        // Will be persisted by caller
        return Task.FromResult(comment);
    }

    public Task UpdateScores(Guid reviewId, int accuracy, int strength, int alignment)
    {
        // Validation: 1-10 range
        if (accuracy < 1 || accuracy > 10) throw new ArgumentException("Accuracy must be 1-10");
        if (strength < 1 || strength > 10) throw new ArgumentException("Strength must be 1-10");
        if (alignment < 1 || alignment > 10) throw new ArgumentException("Alignment must be 1-10");

        // Will be persisted by caller
        return Task.CompletedTask;
    }

    public Task<ReviewDecision> ApproveReview(Guid reviewId, string? summary = null)
    {
        // Check for unresolved blockers first (caller should do this)
        return Task.FromResult(new ReviewDecision
        {
            ReviewId = reviewId,
            Decision = ReviewOutcome.Approved,
            Timestamp = DateTime.UtcNow
        });
    }

    public Task<ReviewDecision> RequestChanges(Guid reviewId, string reason)
    {
        return Task.FromResult(new ReviewDecision
        {
            ReviewId = reviewId,
            Decision = ReviewOutcome.ChangesRequested,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        });
    }

    public Task<ReviewDecision> RejectReview(Guid reviewId, string reason)
    {
        return Task.FromResult(new ReviewDecision
        {
            ReviewId = reviewId,
            Decision = ReviewOutcome.Rejected,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task<bool> CanApprove(Guid reviewId)
    {
        // Check for unresolved Blocker severity comments
        // Will be implemented by caller with DB access
        return await Task.FromResult(true);
    }
}

public class ReviewDecision
{
    public Guid ReviewId { get; set; }
    public ReviewOutcome Decision { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum ReviewOutcome
{
    Approved,
    ChangesRequested,
    Rejected
}

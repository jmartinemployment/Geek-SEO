namespace ContentWriterV3.Domain.Entities;

public class ContentReview : BaseEntity
{
    public Guid AssetVersionId { get; set; }
    public Guid ReviewedByUserId { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public List<ReviewComment> Comments { get; set; } = new();
    public int AccuracyScore { get; set; } // 1-10: how fact-checked?
    public int StrengthScore { get; set; } // 1-10: how compelling/differentiated?
    public int AlignmentScore { get; set; } // 1-10: how well aligned with positioning?
    public string EditorSummary { get; set; } = string.Empty; // Overall assessment
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    public ContentReview() { }

    public ContentReview(Guid assetVersionId, Guid reviewedByUserId)
    {
        AssetVersionId = assetVersionId;
        ReviewedByUserId = reviewedByUserId;
    }

    public void Approve(string? summary = null)
    {
        Status = ReviewStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ReviewedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(summary))
            EditorSummary = summary;
    }

    public void Reject(string reason)
    {
        Status = ReviewStatus.Rejected;
        RejectionReason = reason;
        ReviewedAt = DateTime.UtcNow;
    }

    public void RequestChanges()
    {
        Status = ReviewStatus.ChangesRequested;
        ReviewedAt = DateTime.UtcNow;
    }
}

public enum ReviewStatus
{
    Pending,
    ChangesRequested,
    Approved,
    Rejected
}

public class ReviewComment : BaseEntity
{
    public Guid ReviewId { get; set; }
    public Guid AuthorId { get; set; }
    public int? LineNumber { get; set; }
    public string Section { get; set; } = string.Empty; // Which insight/section
    public string CommentText { get; set; } = string.Empty;
    public CommentType Type { get; set; } // Factual, Tone, Clarity, Structure, CTA
    public CommentSeverity Severity { get; set; } // Blocker, Major, Minor, Suggestion
    public bool Resolved { get; set; }
    public string? Resolution { get; set; }

    public ReviewComment() { }

    public ReviewComment(Guid reviewId, Guid authorId, string section, string text, CommentType type, CommentSeverity severity)
    {
        ReviewId = reviewId;
        AuthorId = authorId;
        Section = section;
        CommentText = text;
        Type = type;
        Severity = severity;
    }
}

public enum CommentType
{
    Factual,     // Claim needs verification
    Tone,        // Voice/brand misalignment
    Clarity,     // Unclear or confusing
    Structure,   // Flow/outline issue
    CTA,         // Call-to-action concern
    Evidence,    // Claim needs better support
    Offering,    // Doesn't align with what we sell
    Competitive  // Misses competitive angle
}

public enum CommentSeverity
{
    Blocker,     // Can't publish as-is
    Major,       // Should fix before publishing
    Minor,       // Nice to fix
    Suggestion   // Feedback but not required
}

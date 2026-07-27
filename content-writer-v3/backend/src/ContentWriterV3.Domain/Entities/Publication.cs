namespace ContentWriterV3.Domain.Entities;

public class Publication : BaseEntity
{
    public Guid AssetVersionId { get; set; }
    public Guid ReviewId { get; set; }
    public string ContentTitle { get; set; } = string.Empty;
    public string PublishedUrl { get; set; } = string.Empty;
    public PublicationStatus Status { get; set; } = PublicationStatus.Queued;
    public DateTime PublishedAt { get; set; }
    public Guid PublishedByUserId { get; set; }
    public string PublicationTarget { get; set; } = string.Empty; // e.g., "WordPress", "Custom CMS", "Supabase"
    public string? PublicationMetadata { get; set; } // JSON: WordPress post ID, canonical URL, etc.
    public DateTime? ScheduledPublishAt { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public List<PublicationEvent> PublicationEvents { get; set; } = new();

    public Publication() { }

    public Publication(Guid assetVersionId, Guid reviewId, string contentTitle, Guid publishedByUserId)
    {
        AssetVersionId = assetVersionId;
        ReviewId = reviewId;
        ContentTitle = contentTitle;
        PublishedByUserId = publishedByUserId;
    }

    public void MarkPublished(string url, string metadata)
    {
        Status = PublicationStatus.Published;
        PublishedUrl = url;
        PublishedAt = DateTime.UtcNow;
        PublicationMetadata = metadata;
    }

    public void MarkFailed(string reason)
    {
        Status = PublicationStatus.Failed;
        FailureReason = reason;
        RetryCount++;
    }

    public void MarkScheduled(DateTime scheduledTime)
    {
        Status = PublicationStatus.Scheduled;
        ScheduledPublishAt = scheduledTime;
    }
}

public enum PublicationStatus
{
    Queued,        // Approved, waiting to be published
    Scheduled,     // Scheduled for future publish
    Publishing,    // Currently being published
    Published,     // Successfully published
    Failed,        // Failed to publish
    Unpublished    // Was published, then removed
}

public class PublicationEvent : BaseEntity
{
    public Guid PublicationId { get; set; }
    public PublicationEventType EventType { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public string Details { get; set; } = string.Empty; // JSON: what changed, why, etc.

    public PublicationEvent() { }

    public PublicationEvent(Guid publicationId, PublicationEventType eventType, string details = "")
    {
        PublicationId = publicationId;
        EventType = eventType;
        Details = details;
        OccurredAt = DateTime.UtcNow;
    }
}

public enum PublicationEventType
{
    PublishedSuccessfully,
    PublishFailed,
    PublishRetried,
    UnpublishedSuccessfully,
    ScheduledForPublish,
    ScheduleChanged,
    MetadataUpdated
}

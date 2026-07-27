namespace ContentWriterV3.Domain.Entities;

public class Job : BaseEntity
{
    public Guid CampaignId { get; set; }
    public string JobType { get; set; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public string PayloadJson { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public string? LeaseOwner { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? RequeuedByUserId { get; set; }
    public DateTime? RequeuedAt { get; set; }
    public int InputVersion { get; set; }
    public int OutputVersion { get; set; }

    public Job() { }

    public Job(Guid campaignId, string jobType, string payloadJson, string idempotencyKey)
    {
        CampaignId = campaignId;
        JobType = jobType;
        PayloadJson = payloadJson;
        IdempotencyKey = idempotencyKey;
        AttemptCount = 0;
    }

    public void MarkRunning(string workerId, TimeSpan leaseDuration)
    {
        Status = JobStatus.Running;
        LeaseOwner = workerId;
        LeaseExpiresAt = DateTime.UtcNow.Add(leaseDuration);
        AttemptCount++;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompleted()
    {
        Status = JobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        LeaseOwner = null;
        LeaseExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorCode, string errorMessage)
    {
        Status = JobStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        LeaseOwner = null;
        LeaseExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Requeue(Guid requeuedByUserId)
    {
        Status = JobStatus.Queued;
        RequeuedByUserId = requeuedByUserId;
        RequeuedAt = DateTime.UtcNow;
        LeaseOwner = null;
        LeaseExpiresAt = null;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum JobStatus
{
    Queued,
    Running,
    Completed,
    Failed
}

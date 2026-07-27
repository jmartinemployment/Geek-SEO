using ContentWriterV3.Domain.Entities;

namespace ContentWriterV3.Application.Services;

public interface IPublicationService
{
    Task<Publication> QueueForPublishing(Guid reviewId, string targetPlatform, Guid queuedByUserId);
    Task<PublicationResult> PublishNow(Guid publicationId);
    Task ScheduleForPublishing(Guid publicationId, DateTime publishAt);
    Task<PublicationResult> Retry(Guid publicationId);
}

public interface IPublishAdapter
{
    string PlatformName { get; }
    Task<PublishResult> PublishAsync(string title, string content, PublishMetadata metadata);
    Task UnpublishAsync(string platformId);
    Task<PublishResult> UpdateAsync(string platformId, string title, string content);
}

public class PublishResult
{
    public bool Success { get; set; }
    public string? PublishedUrl { get; set; }
    public string? PlatformId { get; set; }
    public string? Metadata { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PublishMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public string? CanonicalUrl { get; set; }
    public string? MetaDescription { get; set; }
}

public class PublicationResult
{
    public bool Success { get; set; }
    public string? PublishedUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PublicationService : IPublicationService
{
    public Task<Publication> QueueForPublishing(Guid reviewId, string targetPlatform, Guid queuedByUserId)
    {
        var publication = new Publication(Guid.NewGuid(), reviewId, "Draft Content", queuedByUserId)
        {
            PublicationTarget = targetPlatform,
            Status = PublicationStatus.Queued
        };

        return Task.FromResult(publication);
    }

    public Task<PublicationResult> PublishNow(Guid publicationId)
    {
        // Would be called by a job handler
        // Caller is responsible for persisting
        return Task.FromResult(new PublicationResult { Success = true });
    }

    public Task ScheduleForPublishing(Guid publicationId, DateTime publishAt)
    {
        // Caller updates Publication.ScheduledPublishAt
        return Task.CompletedTask;
    }

    public Task<PublicationResult> Retry(Guid publicationId)
    {
        // Requeue failed publication for retry
        return Task.FromResult(new PublicationResult { Success = true });
    }
}

// Mock implementation for demonstration
public class MockPublishAdapter : IPublishAdapter
{
    public string PlatformName => "MockCMS";

    public Task<PublishResult> PublishAsync(string title, string content, PublishMetadata metadata)
    {
        return Task.FromResult(new PublishResult
        {
            Success = true,
            PublishedUrl = $"https://example.com/{metadata.Slug}",
            PlatformId = Guid.NewGuid().ToString(),
            Metadata = System.Text.Json.JsonSerializer.Serialize(metadata)
        });
    }

    public Task UnpublishAsync(string platformId)
    {
        return Task.CompletedTask;
    }

    public Task<PublishResult> UpdateAsync(string platformId, string title, string content)
    {
        return Task.FromResult(new PublishResult
        {
            Success = true,
            PublishedUrl = $"https://example.com/updated"
        });
    }
}

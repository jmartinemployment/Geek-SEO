namespace GeekSeoBackend.Services;

public sealed record GoogleOAuthStatePayload
{
    public required Guid UserId { get; init; }
    public required Guid ProjectId { get; init; }
    public string? PropertyId { get; init; }
    public string? SiteUrl { get; init; }
};

public interface IGoogleOAuthStateStore
{
    (string State, DateTimeOffset ExpiresAt) Create(GoogleOAuthStatePayload payload);
    GoogleOAuthStatePayload Consume(string state);
}

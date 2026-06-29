using GeekSeoBackend.Services;

namespace GeekSeoBackend.Tests;

public sealed class SignedGoogleOAuthStateStoreTests : IDisposable
{
    private readonly string? _previousKey;

    public SignedGoogleOAuthStateStoreTests()
    {
        _previousKey = Environment.GetEnvironmentVariable("GEEK_SEO_ENCRYPTION_KEY");
        Environment.SetEnvironmentVariable(
            "GEEK_SEO_ENCRYPTION_KEY",
            Convert.ToBase64String(new byte[32]));
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("GEEK_SEO_ENCRYPTION_KEY", _previousKey);
    }

    [Fact]
    public void CreateAndConsume_round_trips_payload()
    {
        var store = new SignedGoogleOAuthStateStore();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var input = new GoogleOAuthStatePayload
        {
            UserId = userId,
            ProjectId = projectId,
            PropertyId = "properties/123",
            SiteUrl = "https://example.com/",
        };

        var (state, expiresAt) = store.Create(input);

        Assert.False(string.IsNullOrWhiteSpace(state));
        Assert.True(expiresAt > DateTimeOffset.UtcNow);

        var output = store.Consume(state);

        Assert.Equal(userId, output.UserId);
        Assert.Equal(projectId, output.ProjectId);
        Assert.Equal("properties/123", output.PropertyId);
        Assert.Equal("https://example.com/", output.SiteUrl);
    }

    [Fact]
    public void Consume_throws_for_tampered_state()
    {
        var store = new SignedGoogleOAuthStateStore();
        var (state, _) = store.Create(new GoogleOAuthStatePayload
        {
            UserId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        });

        var tampered = state[..^4] + "abcd";

        var ex = Assert.Throws<GoogleIntegrationException>(() => store.Consume(tampered));
        Assert.Contains("invalid or expired", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryPeek_returns_project_for_valid_state()
    {
        var store = new SignedGoogleOAuthStateStore();
        var projectId = Guid.NewGuid();
        var (state, _) = store.Create(new GoogleOAuthStatePayload
        {
            UserId = Guid.NewGuid(),
            ProjectId = projectId,
        });

        var ok = store.TryPeek(state, out var payload);

        Assert.True(ok);
        Assert.NotNull(payload);
        Assert.Equal(projectId, payload!.ProjectId);
    }
}

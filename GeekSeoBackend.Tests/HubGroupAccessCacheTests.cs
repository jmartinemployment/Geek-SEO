using GeekSeoBackend.Services;

namespace GeekSeoBackend.Tests;

public sealed class HubGroupAccessCacheTests
{
    [Fact]
    public void TryGet_returns_null_when_missing()
    {
        var cache = new HubGroupAccessCache();
        Assert.Null(cache.TryGet("missing"));
    }

    [Fact]
    public void Set_and_TryGet_round_trip_allowed()
    {
        var cache = new HubGroupAccessCache();
        var key = HubGroupAccessCache.Key(Guid.NewGuid(), "document", Guid.NewGuid());

        cache.Set(key, true);

        Assert.True(cache.TryGet(key));
    }

    [Fact]
    public void Key_is_stable_for_same_inputs()
    {
        var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var entityId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var a = HubGroupAccessCache.Key(userId, "niche", entityId);
        var b = HubGroupAccessCache.Key(userId, "niche", entityId);

        Assert.Equal(a, b);
        Assert.NotEqual(HubGroupAccessCache.Key(userId, "document", entityId), a);
    }
}

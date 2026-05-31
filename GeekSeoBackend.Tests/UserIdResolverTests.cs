using System.Security.Claims;
using GeekSeoBackend.Auth;

namespace GeekSeoBackend.Tests;

public sealed class UserIdResolverTests
{
    private static readonly Guid WorkerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid JwtId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HeaderId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void TryResolve_prefers_worker_context()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, JwtId.ToString()),
        ]));

        var ok = UserIdResolver.TryResolve(WorkerId, user, HeaderId.ToString(), out var resolved);

        Assert.True(ok);
        Assert.Equal(WorkerId, resolved);
    }

    [Fact]
    public void TryResolve_uses_jwt_sub_when_worker_empty()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", JwtId.ToString()),
        ]));

        var ok = UserIdResolver.TryResolve(Guid.Empty, user, null, out var resolved);

        Assert.True(ok);
        Assert.Equal(JwtId, resolved);
    }

    [Fact]
    public void TryResolve_falls_back_to_header()
    {
        var ok = UserIdResolver.TryResolve(Guid.Empty, null, HeaderId.ToString(), out var resolved);

        Assert.True(ok);
        Assert.Equal(HeaderId, resolved);
    }

    [Fact]
    public void TryResolve_returns_false_when_unauthenticated()
    {
        var ok = UserIdResolver.TryResolve(Guid.Empty, null, null, out var resolved);

        Assert.False(ok);
        Assert.Equal(Guid.Empty, resolved);
    }
}

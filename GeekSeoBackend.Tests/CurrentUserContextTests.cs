using GeekSeoBackend.Auth;
using Microsoft.AspNetCore.Http;

namespace GeekSeoBackend.Tests;

public sealed class CurrentUserContextTests
{
    [Fact]
    public void UserId_uses_worker_context_without_touching_http_context()
    {
        var workerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var worker = new WorkerUserContext { UserId = workerId };
        var accessor = new ThrowingHttpContextAccessor();
        var context = new CurrentUserContext(accessor, worker);

        Assert.Equal(workerId, context.UserId);
        Assert.True(context.IsAuthenticated);
        Assert.False(accessor.WasAccessed);
    }

    private sealed class ThrowingHttpContextAccessor : IHttpContextAccessor
    {
        public bool WasAccessed { get; private set; }

        public HttpContext? HttpContext
        {
            get
            {
                WasAccessed = true;
                throw new ObjectDisposedException("Collection", "IFeatureCollection has been disposed.");
            }
            set => throw new NotSupportedException();
        }
    }
}

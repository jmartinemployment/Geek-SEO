using System.Net;
using System.Net.Http;
using GeekSeoBackend.Auth;
using GeekSeoBackend.HttpClients.Repo;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.Tests;

public sealed class HttpTopicalMapRepositoryTests
{
    [Theory]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task GetByProjectAsync_EmptyOrMissingResponse_ReturnsNullSuccess(HttpStatusCode statusCode)
    {
        var projectId = Guid.Parse("e6275e97-6568-4e48-9bab-ee788de8fe77");
        var repo = CreateRepository(statusCode, string.Empty);

        var result = await repo.GetByProjectAsync(projectId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetByProjectAsync_Empty200Body_ReturnsNullSuccess()
    {
        var projectId = Guid.Parse("e6275e97-6568-4e48-9bab-ee788de8fe77");
        var repo = CreateRepository(HttpStatusCode.OK, "   ");

        var result = await repo.GetByProjectAsync(projectId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    private static HttpTopicalMapRepository CreateRepository(HttpStatusCode statusCode, string body)
    {
        var handler = new StubHandler(statusCode, body);
        var factory = new StubHttpClientFactory(handler);
        var user = new StubUserContext(Guid.Parse("92b274f5-2fcb-4935-ba2d-cd8c03e1b21b"));
        return new HttpTopicalMapRepository(factory, user);
    }

    private sealed class StubHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body),
            };
            return Task.FromResult(response);
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            if (!string.Equals(name, GeekDataGateway.HttpClientName, StringComparison.Ordinal))
                throw new InvalidOperationException($"Unexpected client name: {name}");

            return new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        }
    }

    private sealed class StubUserContext(Guid userId) : ICurrentUserContext
    {
        public Guid UserId => userId;
        public string? Email => "test@example.com";
        public bool IsAuthenticated => true;
    }
}

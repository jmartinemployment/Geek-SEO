using GeekSeoBackend.Auth;

namespace GeekSeoBackend.Tests;

public sealed class WorkerUserContextTests
{
    [Fact]
    public async Task UserId_is_isolated_per_async_flow()
    {
        var worker = new WorkerUserContext();
        var firstId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var secondId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var firstTask = Task.Run(async () =>
        {
            worker.SetUserId(firstId);
            await Task.Delay(50);
            return worker.UserId;
        });

        var secondTask = Task.Run(async () =>
        {
            worker.SetUserId(secondId);
            await Task.Delay(50);
            return worker.UserId;
        });

        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(firstId, results[0]);
        Assert.Equal(secondId, results[1]);
    }
}

using ContentWriterV3.Domain.Entities;

namespace ContentWriterV3.Infrastructure.Jobs;

public interface IJobHandler
{
    string JobType { get; }
    Task ExecuteAsync(Job job, CancellationToken cancellationToken);
}

public abstract class JobHandler<TPayload> : IJobHandler
{
    public abstract string JobType { get; }

    public async Task ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        var payload = System.Text.Json.JsonSerializer.Deserialize<TPayload>(job.PayloadJson)
            ?? throw new InvalidOperationException($"Failed to deserialize job payload for {JobType}");

        await ExecuteAsync(job, payload, cancellationToken);
    }

    protected abstract Task ExecuteAsync(Job job, TPayload payload, CancellationToken cancellationToken);
}

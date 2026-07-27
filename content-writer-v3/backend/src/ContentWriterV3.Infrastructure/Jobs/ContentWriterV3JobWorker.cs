using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ContentWriterV3.Infrastructure.Data;
using ContentWriterV3.Domain.Entities;

namespace ContentWriterV3.Infrastructure.Jobs;

public class ContentWriterV3JobWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContentWriterV3JobWorker> _logger;
    private readonly Dictionary<string, IJobHandler> _handlers;
    private readonly string _workerId;
    private readonly TimeSpan _leaseDuration = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 10;

    public ContentWriterV3JobWorker(IServiceProvider serviceProvider, ILogger<ContentWriterV3JobWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _workerId = Environment.MachineName;
        _handlers = new Dictionary<string, IJobHandler>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ContentWriterV3JobWorker starting with ID: {WorkerId}", _workerId);

        // Register handlers
        RegisterHandlers();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessJobsAsync(stoppingToken);
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in job worker poll loop");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("ContentWriterV3JobWorker stopped");
    }

    private async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var claimer = scope.ServiceProvider.GetRequiredService<IJobQueueClaimer>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ContentWriterV3DbContext>();

        try
        {
            var jobs = await claimer.ClaimJobsAsync(_workerId, _batchSize, _leaseDuration, cancellationToken);

            foreach (var job in jobs)
            {
                if (!_handlers.TryGetValue(job.JobType, out var handler))
                {
                    _logger.LogWarning("No handler registered for job type: {JobType}", job.JobType);
                    job.MarkFailed("HANDLER_NOT_FOUND", $"No handler registered for job type: {job.JobType}");
                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue;
                }

                try
                {
                    await handler.ExecuteAsync(job, cancellationToken);
                    job.MarkCompleted();
                    _logger.LogInformation("Job {JobId} completed successfully", job.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Job {JobId} failed", job.Id);
                    job.MarkFailed("JOB_EXECUTION_ERROR", ex.Message);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing jobs");
        }
    }

    private void RegisterHandlers()
    {
        // Handlers will be registered via DI container
        using var scope = _serviceProvider.CreateScope();
        var allHandlers = scope.ServiceProvider.GetServices<IJobHandler>();

        foreach (var handler in allHandlers)
        {
            _handlers[handler.JobType] = handler;
            _logger.LogInformation("Registered handler for job type: {JobType}", handler.JobType);
        }
    }
}

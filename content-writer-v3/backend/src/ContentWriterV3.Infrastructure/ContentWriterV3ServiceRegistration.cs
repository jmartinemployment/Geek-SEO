using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ContentWriterV3.Infrastructure.Data;
using ContentWriterV3.Infrastructure.Jobs;

namespace ContentWriterV3.Infrastructure;

public static class ContentWriterV3ServiceRegistration
{
    public static IServiceCollection AddContentWriterV3(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ContentWriterV3DbContext>(
            options => options.UseContentWriterV3(connectionString));

        services.AddScoped<IJobQueueClaimer>(sp =>
            new JobQueueClaimer(connectionString));

        services.AddHostedService<ContentWriterV3JobWorker>();

        return services;
    }

    public static async Task InitializeContentWriterV3DatabaseAsync(
        this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ContentWriterV3DbContext>();

        // Apply pending migrations
        await dbContext.Database.MigrateAsync();
    }
}

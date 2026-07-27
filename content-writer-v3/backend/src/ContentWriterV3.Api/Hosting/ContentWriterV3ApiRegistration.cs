using Microsoft.Extensions.DependencyInjection;
using ContentWriterV3.Application.Services;
using ContentWriterV3.Infrastructure.Jobs;
using ContentWriterV3.Infrastructure.Jobs.Handlers;

namespace ContentWriterV3.Api.Hosting;

public static class ContentWriterV3ApiRegistration
{
    public static IServiceCollection AddContentWriterV3Api(this IServiceCollection services)
    {
        // Application services
        services.AddScoped<IEvidenceSupportLevelClassifier, EvidenceSupportLevelClassifier>();
        services.AddScoped<IReconciliationService, ReconciliationService>();

        // Job handlers
        services.AddScoped<IJobHandler, InitiateResearchHandler>();

        // Controllers
        services.AddControllers();

        return services;
    }
}

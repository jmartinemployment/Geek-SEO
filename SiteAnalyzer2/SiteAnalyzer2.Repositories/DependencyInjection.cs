using Microsoft.Extensions.DependencyInjection;

namespace SiteAnalyzer2.Repositories;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAnalysisRunRepository, AnalysisRunRepository>();
        services.AddScoped<ISiteProfileAssemblerRepository, SiteProfileAssemblerRepository>();
        return services;
    }
}

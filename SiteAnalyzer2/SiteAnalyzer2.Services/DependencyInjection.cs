using Microsoft.Extensions.DependencyInjection;
using SiteAnalyzer2.Services.BusinessFocus;
using SiteAnalyzer2.Services.Filtering;
using SiteAnalyzer2.Services.Parsing;
using SiteAnalyzer2.Services.CompetitorCrawl;
using SiteAnalyzer2.Services.Integrations;
using SiteAnalyzer2.Services.Pipeline;
using SiteAnalyzer2.Services.ProfileAssembly;
using SiteAnalyzer2.Services.Rankings;
using SiteAnalyzer2.Services.SiteAudit;

namespace SiteAnalyzer2.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddSiteAnalyzerServices(this IServiceCollection services)
    {
        services.AddHttpClient<PageFetchService>();
        services.AddHttpClient(nameof(SiteProfileAssemblerService));
        services.AddHttpClient(nameof(RobotsTxtChecker));
        services.AddHttpClient(nameof(CompetitorCrawlService));
        services.AddHttpClient(nameof(CompetitorOverviewLiteService));
        services.AddHttpClient(nameof(DomainOverviewService))
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Accept.ParseAdd(
                    "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            });
        services.AddHttpClient<OpenAiBusinessFocusClassifier>();
        services.AddHttpClient<AnthropicBusinessFocusClassifier>();

        services.AddScoped<StubBusinessFocusClassifier>();
        services.AddScoped<OpenAiBusinessFocusClassifier>();
        services.AddScoped<AnthropicBusinessFocusClassifier>();
        services.AddScoped<IBusinessFocusClassifier>(sp =>
        {
            var provider = BusinessFocusProviderConfiguration.ResolveEffectiveProvider();
            return provider switch
            {
                BusinessFocusProvider.OpenAi => sp.GetRequiredService<OpenAiBusinessFocusClassifier>(),
                BusinessFocusProvider.Anthropic => sp.GetRequiredService<AnthropicBusinessFocusClassifier>(),
                _ => sp.GetRequiredService<StubBusinessFocusClassifier>()
            };
        });

        services.AddScoped<PageExtractionService>();
        services.AddScoped<BusinessFocusClassificationService>();
        services.AddScoped<RelevanceFilterService>();
        services.AddScoped<SerpDiscoveryService>();
        services.AddScoped<SerpHtmlImportService>();
        services.AddScoped<PageFetchService>();
        services.AddScoped<LinkGraphBuilderService>();
        services.AddScoped<BoundedPageRankService>();
        services.AddScoped<ComparisonService>();
        services.AddScoped<RunGateService>();
        services.AddScoped<SerpExternalCompletionService>();
        services.AddScoped<AnalysisRunOrchestrator>();
        services.AddScoped<RobotsTxtChecker>();
        services.AddScoped<CompetitorStructuralExtractService>();
        services.AddScoped<CompetitorCrawlService>();
        services.AddScoped<CompetitorOverviewLiteService>();
        services.AddScoped<OwnedDomainIndexService>();
        services.AddScoped<DomainOverviewService>();
        services.AddSingleton<CompetitorCrawlProgressPublisher>();
        services.AddScoped<CompetitorCrawlProgressLogService>();
        services.AddSingleton<CompetitorCrawlJobService>();
        services.AddScoped<ContentWriterExportService>();
        services.AddScoped<ContentWriterSiteBundleService>();
        services.AddScoped<SerpAutoImportService>();
        services.AddScoped<SiteProfileService>();
        services.AddScoped<OperatorProjectResolver>();
        services.AddScoped<SiteProfileAssemblerService>();
        services.AddScoped<OperatorRunFocusService>();
        services.AddScoped<OperatorResearchService>();
        services.AddScoped<SerpRankHistoryService>();
        services.AddScoped<KeywordWorkflowService>();
        services.AddScoped<ManualLaneImportService>();
        services.AddScoped<SiteAuditCheckService>();
        services.AddScoped<SiteAuditRollupService>();
        services.AddScoped<SiteAuditJobService>();

        return services;
    }
}

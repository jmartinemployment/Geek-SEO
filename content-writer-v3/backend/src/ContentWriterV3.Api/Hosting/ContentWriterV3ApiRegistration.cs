using Microsoft.Extensions.DependencyInjection;
using ContentWriterV3.Application.Services;
using ContentWriterV3.Infrastructure.Jobs;
using ContentWriterV3.Infrastructure.Jobs.Handlers;

namespace ContentWriterV3.Api.Hosting;

public static class ContentWriterV3ApiRegistration
{
    public static IServiceCollection AddContentWriterV3Api(this IServiceCollection services)
    {
        // Phase 1: Research & Intelligence
        services.AddScoped<IEvidenceSupportLevelClassifier, EvidenceSupportLevelClassifier>();
        services.AddScoped<IReconciliationService, ReconciliationService>();

        // Phase 1B: Insight Extraction (independent reasoning, not template-filling)
        services.AddScoped<IInsightExtractor, InsightExtractor>();

        // Phase 2: Strategy Brief & Human Decision
        services.AddScoped<IStrategyBriefApprovalValidator, StrategyBriefApprovalValidator>();
        services.AddScoped<IContentPlanService, ContentPlanService>();

        // Phase 3: Intelligent Drafting with Site Context
        services.AddScoped<IContentIntelligenceValidator, ContentIntelligenceValidator>();
        services.AddScoped<IContentGenerator, MockContentGenerator>();

        // Phase 4: Review & Approval
        services.AddScoped<IReviewService, ReviewService>();

        // Phase 5: Publication
        services.AddScoped<IPublicationService, PublicationService>();
        services.AddScoped<IPublishAdapter, MockPublishAdapter>();

        // Job handlers
        services.AddScoped<IJobHandler, InitiateResearchHandler>();
        services.AddScoped<IJobHandler, ExtractInsightsHandler>();
        services.AddScoped<IJobHandler, DraftContentHandler>();

        // Controllers
        services.AddControllers();

        return services;
    }
}

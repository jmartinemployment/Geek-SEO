using System.Text;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeoBackend.Jobs;
using GeekSeoBackend.Services.NicheExtraction;
using GeekSeo.Application.Services.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.HttpClients.Repo;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Providers.Seo;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SubscriptionService = GeekSeo.Application.Services.Seo.SubscriptionService;

namespace GeekSeoBackend.Extensions;

public static class SeoBackendExtensions
{
    public static IServiceCollection AddGeekSeoBackend(
        this IServiceCollection services,
        IConfiguration configuration,
        PlaywrightBrowserHolder? playwrightHolder)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<WorkerUserContext>();
        services.AddSingleton<IBackgroundUserContext>(sp => sp.GetRequiredService<WorkerUserContext>());
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        // Persistence via GeekAPI → GeekRepository (dumb data pipe)
        services.AddScoped<IProjectRepository, HttpProjectRepository>();
        services.AddScoped<IContentDocumentRepository, HttpContentDocumentRepository>();
        services.AddScoped<IBackgroundJobRepository, HttpBackgroundJobRepository>();
        services.AddScoped<ISerpCacheRepository, HttpSerpCacheRepository>();
        services.AddScoped<ICompetitorPageRepository, HttpCompetitorPageRepository>();
        services.AddScoped<ISubscriptionRepository, HttpSubscriptionRepository>();
        services.AddScoped<IUsageMeteringRepository, HttpUsageMeteringRepository>();
        services.AddScoped<IKeywordRepository, HttpKeywordRepository>();
        services.AddScoped<IWordPressConnectionRepository, HttpWordPressConnectionRepository>();
        services.AddScoped<IWordPressPublishRepository, HttpWordPressPublishRepository>();
        services.AddScoped<IBrandVoiceRepository, HttpBrandVoiceRepository>();
        services.AddScoped<ISiteAuditRepository, HttpSiteAuditRepository>();
        services.AddScoped<IPlagiarismRepository, HttpPlagiarismRepository>();
        services.AddScoped<IGoogleIntegrationRepository, HttpGoogleIntegrationRepository>();
        services.AddScoped<ITopicalMapRepository, HttpTopicalMapRepository>();
        services.AddScoped<ISerpDeepCacheRepository, HttpSerpDeepCacheRepository>();
        services.AddScoped<IPublishedPageRepository, HttpPublishedPageRepository>();
        services.AddScoped<IGeoTrackingRepository, HttpGeoTrackingRepository>();
        services.AddScoped<IContentGuardRepository, HttpContentGuardRepository>();
        services.AddScoped<IRankTrackingRepository, HttpRankTrackingRepository>();

        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IContentDocumentService, ContentDocumentService>();
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddScoped<SubscriptionService>();
        services.AddScoped<ISubscriptionService, FullAccessSubscriptionService>();
        services.AddScoped<IUsageMeteringService, UsageMeteringService>();

        // External providers + scoring (product host only)
        services.AddScoped<IRichTextProvider, HtmlRichTextProvider>();
        services.AddSeoDataProviders();
        services.AddHttpClient("Anthropic", client => client.BaseAddress = new Uri("https://api.anthropic.com"));
        services.AddHttpClient("WordPress");
        services.AddScoped<IAIProvider, ClaudeProvider>();
        services.AddScoped<IWordPressProvider, WordPressRestProvider>();

        if (playwrightHolder?.Browser is not null)
            services.AddSingleton<ICrawlerProvider>(_ => new PlaywrightCrawlerProvider(playwrightHolder.Browser));
        else
            services.AddSingleton<ICrawlerProvider, NoOpCrawlerProvider>();

        services.AddScoped<CompetitorCrawlService>();
        services.AddScoped<IContentScoringService, ContentScoringService>();
        services.AddScoped<ICompetitorInsightsService, CompetitorInsightsService>();
        services.AddScoped<IContentBriefService, ContentBriefService>();
        services.AddScoped<IAIWritingService, AIWritingService>();
        services.AddScoped<IKeywordResearchService, KeywordResearchService>();
        services.AddScoped<IWordPressPublishService, WordPressPublishService>();
        services.AddScoped<IBrandVoiceService, BrandVoiceService>();
        services.AddScoped<ISerpAnalysisService, SerpAnalysisService>();
        services.AddScoped<IInternalLinkService, InternalLinkService>();
        services.AddScoped<ISiteAuditService, SiteAuditService>();
        services.AddScoped<IPlagiarismService, PlagiarismService>();
        services.AddMemoryCache();
        services.AddHttpClient("GoogleOAuth");
        services.AddHttpClient("GoogleApis");
        services.AddSingleton<IGoogleOAuthStateStore, InMemoryGoogleOAuthStateStore>();
        services.AddSingleton(_ => new GoogleOAuthOptions
        {
            ClientId = ReadEnv("GOOGLE_CLIENT_ID"),
            ClientSecret = ReadEnv("GOOGLE_CLIENT_SECRET"),
            RedirectUri = ReadEnv("GOOGLE_REDIRECT_URI"),
        });
        services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
        services.AddScoped<IGoogleDataService, GoogleDataService>();
        services.AddScoped<CannibalizationService>();
        services.AddScoped<DashboardOverviewService>();
        services.AddScoped<ITopicalHierarchyBuilder, TopicalHierarchyBuilder>();
        services.AddScoped<TopicalMapService>();
        services.AddScoped<PublishedContentAuditService>();
        services.AddScoped<GeoVisibilityService>();
        services.AddScoped<ContentGuardService>();
        services.AddScoped<RankTrackingService>();

        // Niche Analyzer
        services.AddScoped<INicheProfileRepository, HttpNicheProfileRepository>();
        services.AddScoped<INicheAnalyticsDapperRepository, HttpNicheAnalyticsDapperRepository>();
        services.AddScoped<SchemaOrgExtractor>();
        services.AddScoped<SitemapExtractor>();
        services.AddScoped<NavMenuExtractor>();
        services.AddScoped<HomepageHeadingsExtractor>();
        services.AddScoped<PageContentExtractor>();
        services.AddScoped<SitePageCrawler>();
        services.AddScoped<InternalLinkExtractor>();
        services.AddScoped<UrlPatternExtractor>();
        services.AddScoped<PillarValidator>();
        services.AddScoped<PillarMerger>();
        services.AddScoped<TopicFusionEngine>();
        services.AddScoped<PillarDemandEnricher>();
        services.AddScoped<GscQueryExtractor>();
        services.AddScoped<NicheAuthorityScorer>();
        services.AddScoped<NicheRootEntityBuilder>();
        services.AddScoped<NicheAnalyzerService>();
        services.AddScoped<NicheAnalysisBackgroundJob>();

        services.AddHttpClient("PayPal");
        services.AddSingleton(_ => new PayPalOptions
        {
            ClientId = ReadEnv("PAYPAL_CLIENT_ID"),
            ClientSecret = ReadEnv("PAYPAL_CLIENT_SECRET"),
            WebhookId = ReadEnv("PAYPAL_WEBHOOK_ID"),
            UseSandbox = !string.Equals(ReadEnv("PAYPAL_ENVIRONMENT"), "live", StringComparison.OrdinalIgnoreCase),
        });
        services.AddScoped<IPayPalBillingService, PayPalBillingService>();

        services.AddHttpClient("Copyscape", client => client.Timeout = TimeSpan.FromSeconds(90));
        services.AddSingleton(_ => new CopyscapeOptions
        {
            Username = ReadEnv("COPYSCAPE_USERNAME"),
            ApiKey = ReadEnv("COPYSCAPE_API_KEY"),
            SpendLimitUsd = decimal.TryParse(ReadEnv("COPYSCAPE_SPEND_LIMIT_USD"), out var limit) ? limit : 0.50m,
        });
        services.AddScoped<IPlagiarismProvider, CopyscapePlagiarismProvider>();

        services.AddHttpClient("PublicScanPage", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GeekSEO-Scan/1.0 (https://seo.geekatyourspot.com)");
        });
        services.AddHttpClient("PublicScanPsi", client => client.Timeout = TimeSpan.FromSeconds(45));
        services.AddScoped<IPublicSiteScanService, PublicSiteScanService>();

        services.AddSignalR();
        services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, Hubs.SubUserIdProvider>();
        AddAuthentication(services, configuration);

        return services;
    }

    private static string ReadEnv(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var authServerUrl = Environment.GetEnvironmentVariable("GEEK_OAUTH_AUTHORITY")
            ?? Environment.GetEnvironmentVariable("AUTH_SERVER_URL");
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");

        if (!string.IsNullOrWhiteSpace(authServerUrl))
        {
            var authority = authServerUrl.TrimEnd('/');
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.RequireHttpsMetadata = !authority.Contains("localhost", StringComparison.OrdinalIgnoreCase);
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        NameClaimType = "sub",
                        ClockSkew = TimeSpan.FromMinutes(1),
                    };
                    options.Events = JwtBearerEvents();
                });
            services.AddAuthorization();
            return;
        }

        if (string.IsNullOrWhiteSpace(jwtKey))
            return;

        var keyBytes = Convert.FromBase64String(jwtKey);
        var issuer = configuration["Jwt:Authority"]
            ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
            ?? "https://api.geekatyourspot.com";
        var audience = configuration["Jwt:Audience"]
            ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            ?? "geekseo";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
                options.Events = JwtBearerEvents();
            });
        services.AddAuthorization();
    }

    private static JwtBearerEvents JwtBearerEvents() => new()
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/seo-scoring"))
                context.Token = accessToken;
            return Task.CompletedTask;
        },
    };
}

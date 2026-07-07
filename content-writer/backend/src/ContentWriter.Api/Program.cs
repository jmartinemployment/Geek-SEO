using System.Text.Json.Serialization;
using ContentWriter.Application.Providers;
using ContentWriter.Application.Services;
using ContentWriter.Application.Services.JsonLd;
using ContentWriter.Application.Services.PromptBuilders;
using ContentWriter.Application.Services.SchemaBuilders;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Data;
using ContentWriter.Infrastructure.Repositories;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

Env.TraversePath().Load();

var startupLogger = LoggerFactory.Create(logging => logging.AddSimpleConsole())
    .CreateLogger("ContentWriter.Startup");

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "ContentWriterFrontend";

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = DatabaseConnectionResolver.Resolve(builder.Configuration);
var databaseProvider = DatabaseConnectionResolver.DetectProvider(connectionString);

startupLogger.LogInformation("Content Writer database provider: {Provider}", databaseProvider);
if (databaseProvider == ContentWriterDatabaseProvider.PostgreSql)
{
    startupLogger.LogInformation(
        "PostgreSQL schema: {Schema}",
        ContentWriterDbContextOptionsExtensions.SchemaName);
}

builder.Services.AddDbContext<ContentWriterDbContext>(options =>
{
    switch (databaseProvider)
    {
        case ContentWriterDatabaseProvider.Sqlite:
            options.UseSqlite(connectionString);
            break;
        case ContentWriterDatabaseProvider.PostgreSql:
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable(
                    ContentWriterDbContextOptionsExtensions.MigrationsHistoryTableName,
                    ContentWriterDbContextOptionsExtensions.SchemaName));
            break;
        default:
            options.UseSqlServer(connectionString);
            break;
    }
});

builder.Services.AddScoped<IProjectRepository, ProjectRepository>();

builder.Services.Configure<LlmProvidersOptions>(builder.Configuration.GetSection(LlmProvidersOptions.SectionName));
builder.Services.Configure<CompanyProfileOptions>(builder.Configuration.GetSection(CompanyProfileOptions.SectionName));

builder.Services.AddHttpClient<LmStudioProvider>();
builder.Services.AddHttpClient<OpenAiProvider>();
builder.Services.AddHttpClient<AnthropicProvider>();

builder.Services.AddKeyedTransient<IContentGenerationProvider>(LlmProviderType.LmStudio,
    (sp, _) => sp.GetRequiredService<LmStudioProvider>());
builder.Services.AddKeyedTransient<IContentGenerationProvider>(LlmProviderType.OpenAi,
    (sp, _) => sp.GetRequiredService<OpenAiProvider>());
builder.Services.AddKeyedTransient<IContentGenerationProvider>(LlmProviderType.Anthropic,
    (sp, _) => sp.GetRequiredService<AnthropicProvider>());

builder.Services.AddScoped<IContentProviderFactory, ContentProviderFactory>();

builder.Services.AddHttpClient<ISiteCrawlerService, SiteCrawlerService>();
builder.Services.AddScoped<IKeywordHtmlParserService, KeywordHtmlParserService>();
builder.Services.AddScoped<IContentPromptBuilder, ContentPromptBuilder>();
builder.Services.AddScoped<ISoftwareApplicationSchemaBuilder, SoftwareApplicationSchemaBuilder>();
builder.Services.AddScoped<ITechnicalArticleSchemaBuilder, TechnicalArticleSchemaBuilder>();
builder.Services.AddScoped<IBlogPostingSchemaBuilder, BlogPostingSchemaBuilder>();
builder.Services.AddScoped<IContentGenerationOrchestrator, ContentGenerationOrchestrator>();
builder.Services.AddSingleton<IJsonLdParserService, JsonLdParserService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "https://seo.geekatyourspot.com", "http://localhost:3000" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ContentWriterDbContext>();
    try
    {
        if (databaseProvider == ContentWriterDatabaseProvider.Sqlite)
            db.Database.EnsureCreated();
        else
            db.Database.Migrate();

        startupLogger.LogInformation("Database ready.");
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Database initialization failed.");
        if (!app.Environment.IsDevelopment())
            throw;
    }
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5199";
app.Run($"http://0.0.0.0:{port}");

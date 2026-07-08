using ContentImageSpike.Abstractions;
using ContentImageSpike.Application;
using ContentImageSpike.Domain;
using ContentImageSpike.Infrastructure;
using ContentWriter.Infrastructure.Data;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

Env.TraversePath().Load();

var parsed = CliArgs.Parse(args);
if (parsed.ShowHelp)
{
    CliArgs.PrintHelp();
    return 0;
}

if (parsed.ProjectId is null)
{
    Console.Error.WriteLine("Missing required --project-id <guid>");
    CliArgs.PrintHelp();
    return 1;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var options = new ImageSpikeOptions
{
    OutputDirectory = parsed.OutputDirectory
        ?? configuration[$"{ImageSpikeOptions.SectionName}:OutputDirectory"]
        ?? "output/image-spike",
    OpenAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? configuration[$"{ImageSpikeOptions.SectionName}:OpenAiApiKey"]
        ?? configuration["LlmProviders:OpenAi:ApiKey"]
        ?? string.Empty,
    OpenAiModel = configuration[$"{ImageSpikeOptions.SectionName}:OpenAiModel"] ?? "dall-e-3",
    LeonardoApiKey = Environment.GetEnvironmentVariable("LEONARDO_API_KEY")
        ?? configuration[$"{ImageSpikeOptions.SectionName}:LeonardoApiKey"]
        ?? string.Empty,
    LeonardoModelId = configuration[$"{ImageSpikeOptions.SectionName}:LeonardoModelId"]
        ?? "de7d3faf-762f-48e0-b3b7-9d0ac3a3fcf3",
};

var connectionString = ResolveConnectionString(configuration);
var dbOptions = BuildDbContextOptions(connectionString);

var services = new ServiceCollection();
services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Information));
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton(Options.Create(options));
services.AddSingleton(dbOptions);
services.AddScoped<ContentWriterDbContext>(_ => new ContentWriterDbContext(dbOptions));

services.AddHttpClient<OpenAiImageProvider>();
services.AddHttpClient<LeonardoImageProvider>();
services.AddSingleton<IContentImageSourceReader, ContentWriterImageSourceReader>();
services.AddSingleton<IImagePromptBuilder, PillarFigurePromptBuilder>();
services.AddSingleton<IImagePromptBuilder>(new SocialEyeCandyPromptBuilder(ImageUseCase.SocialFacebook, "Facebook"));
services.AddSingleton<IImagePromptBuilder>(new SocialEyeCandyPromptBuilder(ImageUseCase.SocialLinkedIn, "LinkedIn"));
services.AddSingleton<IImageGenerationProvider, OpenAiImageProvider>(sp => sp.GetRequiredService<OpenAiImageProvider>());
services.AddSingleton<IImageGenerationProvider, LeonardoImageProvider>(sp => sp.GetRequiredService<LeonardoImageProvider>());
services.AddSingleton<IImageArtifactWriter, LocalImageArtifactWriter>();
services.AddSingleton<ImageSpikeService>();

await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var spike = scope.ServiceProvider.GetRequiredService<ImageSpikeService>();

try
{
    var paths = await spike.RunAsync(parsed.ProjectId.Value, parsed.Providers);
    if (paths.Count == 0)
    {
        Console.Error.WriteLine("No images were generated (check logs for provider errors).");
        return 2;
    }

    Console.WriteLine($"Generated {paths.Count} image(s):");
    foreach (var path in paths)
        Console.WriteLine($"  {path}");

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static string ResolveConnectionString(IConfiguration configuration)
{
    var fromEnv = Environment.GetEnvironmentVariable("CONTENT_WRITER_DATABASE_URL")
        ?? Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrWhiteSpace(fromEnv))
        return DatabaseConnectionResolver.NormalizePostgresUrl(fromEnv);

    return configuration.GetConnectionString("ContentWriterDb")
        ?? $"Data Source={Path.GetFullPath(Path.Combine("src", "ContentWriter.Api", "contentwriter.db"))}";
}

static DbContextOptions<ContentWriterDbContext> BuildDbContextOptions(string connectionString)
{
    var provider = DatabaseConnectionResolver.DetectProvider(connectionString);
    var builder = new DbContextOptionsBuilder<ContentWriterDbContext>();

    switch (provider)
    {
        case ContentWriterDatabaseProvider.Sqlite:
            builder.UseSqlite(connectionString);
            break;
        case ContentWriterDatabaseProvider.PostgreSql:
            builder.UseContentWriterPostgres(connectionString);
            break;
        default:
            builder.UseSqlServer(connectionString);
            break;
    }

    return builder.Options;
}

internal static class CliArgs
{
    public static Parsed Parse(string[] args)
    {
        var result = new Parsed();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "-h":
                case "--help":
                    result.ShowHelp = true;
                    break;
                case "--project-id":
                    if (i + 1 < args.Length && Guid.TryParse(args[++i], out var id))
                        result.ProjectId = id;
                    break;
                case "--provider":
                    if (i + 1 < args.Length)
                        result.Providers = ParseProviders(args[++i]);
                    break;
                case "--output-dir":
                    if (i + 1 < args.Length)
                        result.OutputDirectory = args[++i];
                    break;
            }
        }

        return result;
    }

    private static HashSet<string> ParseProviders(string value) => value.ToLowerInvariant() switch
    {
        "both" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "openai", "leonardo" },
        "openai" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "openai" },
        "leonardo" => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "leonardo" },
        _ => new HashSet<string>(
            value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase),
    };

    public static void PrintHelp()
    {
        Console.WriteLine("""
            ContentImageSpike — compare OpenAI vs Leonardo images from Content Writer DB.

            Usage:
              dotnet run --project tools/ContentImageSpike -- --project-id <guid> [options]

            Options:
              --project-id <guid>   Required. Content Writer project to read.
              --provider both       openai | leonardo | both (default: both)
              --output-dir <path>   Default: output/image-spike
              -h, --help

            Environment:
              CONTENT_WRITER_DATABASE_URL / DATABASE_URL  Postgres (production)
              ConnectionStrings:ContentWriterDb             SQLite path (local appsettings)
              OPENAI_API_KEY
              LEONARDO_API_KEY
            """);
    }

    public sealed class Parsed
    {
        public bool ShowHelp { get; set; }
        public Guid? ProjectId { get; set; }
        public string? OutputDirectory { get; set; }
        public HashSet<string> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "openai",
            "leonardo",
        };
    }
}

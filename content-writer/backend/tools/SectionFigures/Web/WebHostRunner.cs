using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using SectionFigures.Models;

namespace SectionFigures.Web;

public static class WebHostRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task RunAsync(int port, bool openBrowser)
    {
        var outputRoot = EnvironmentConfig.RequireOutputRoot();
        var session = new FigureSession();
        var runner = new GenerateRunner();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        var app = builder.Build();

        var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (Directory.Exists(wwwroot))
        {
            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(wwwroot),
            });
        }

        app.MapGet("/api/status", () =>
        {
            var apiUrl = Environment.GetEnvironmentVariable("CONTENT_WRITER_API_URL");
            return Results.Json(new
            {
                apiUrl,
                outputDir = outputRoot,
                contentWriterApiConfigured = !string.IsNullOrWhiteSpace(apiUrl),
                contentWriterKeyConfigured = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CONTENT_WRITER_API_KEY")),
                openAiConfigured = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
                model = ImageGenerationDefaults.OpenAiModel,
                size = ImageGenerationDefaults.OpenAiSize,
                costPerImageUsd = ImageGenerationDefaults.EstimatedCostPerImageUsd,
                costConfirmThreshold = ImageGenerationDefaults.CostConfirmThreshold,
            }, JsonOptions);
        });

        app.MapPost("/api/export", async (ExportRequest request) =>
        {
            if (request.ProjectId == Guid.Empty)
            {
                return Results.BadRequest(new { error = "Project ID is required." });
            }

            try
            {
                var client = ContentWriterFiguresClient.FromEnvironment();
                var manifest = await client.ExportManifestAsync(request.ProjectId);
                var jobFile = FigureJobBuilder.BuildJobs(manifest);
                session.SetJobs(jobFile);
                return Results.Json(BuildPlanResponse(jobFile, outputRoot), JsonOptions);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/api/plan", () =>
        {
            var jobs = session.Jobs;
            if (jobs is null)
            {
                return Results.BadRequest(new { error = "Export a project first." });
            }

            return Results.Json(BuildPlanResponse(jobs, outputRoot), JsonOptions);
        });

        app.MapPost("/api/generate", (GenerateRequest request) =>
        {
            var jobs = session.Jobs;
            if (jobs is null)
            {
                return Results.BadRequest(new { error = "Export a project first." });
            }

            var summary = JobPlanner.Summarize(jobs.Jobs, outputRoot);
            var toRun = request.Force ? summary.TotalJobs : summary.ToGenerate;

            if (toRun == 0)
            {
                return Results.BadRequest(new { error = "Nothing to generate — all AVIFs already exist on disk." });
            }

            if (toRun > ImageGenerationDefaults.CostConfirmThreshold && !request.Confirmed)
            {
                return Results.BadRequest(new
                {
                    error = $"Confirm spend: {toRun} image(s) ≈ ${toRun * ImageGenerationDefaults.EstimatedCostPerImageUsd:F2} USD.",
                    requiresConfirmation = true,
                    toGenerate = toRun,
                    estimatedCostUsd = toRun * ImageGenerationDefaults.EstimatedCostPerImageUsd,
                });
            }

            if (!runner.TryStart(jobs.Jobs, outputRoot, request.Force, request.Concurrency, request.FailFast))
            {
                return Results.Conflict(new { error = "A batch is already running." });
            }

            return Results.Accepted();
        });

        app.MapGet("/api/generate/status", () => Results.Json(runner.Snapshot(), JsonOptions));

        var url = $"http://127.0.0.1:{port}";
        Console.WriteLine($"SectionFigures UI: {url}");
        Console.WriteLine($"Output: {outputRoot}");

        if (openBrowser)
        {
            TryOpenBrowser(url);
        }

        await app.RunAsync();
    }

    private static object BuildPlanResponse(FigureJobFile jobFile, string outputRoot)
    {
        var summary = JobPlanner.Summarize(jobFile.Jobs, outputRoot);
        var rows = jobFile.Jobs.Select(j => new
        {
            j.RelativePath,
            j.SourceType,
            j.Heading,
            j.HeadingSlug,
            existsOnDisk = File.Exists(JobPlanner.AbsolutePath(outputRoot, j.RelativePath)),
        }).ToList();

        return new
        {
            projectId = jobFile.ProjectId,
            exportedAtUtc = jobFile.ExportedAtUtc,
            summary = new
            {
                summary.TotalJobs,
                summary.AlreadyOnDisk,
                summary.ToGenerate,
                summary.Model,
                summary.Size,
                summary.EstimatedCostUsd,
            },
            rows,
        };
    }

    private static void TryOpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            Console.WriteLine("Could not open browser automatically — paste the URL above.");
        }
    }

    private sealed record ExportRequest(Guid ProjectId);

    private sealed record GenerateRequest(bool Confirmed, bool Force, int Concurrency, bool FailFast);
}

public static class EnvironmentConfig
{
    public static string RequireOutputRoot()
    {
        var root = Environment.GetEnvironmentVariable("CONTENT_IMAGE_OUTPUT_DIR");
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException(
                "CONTENT_IMAGE_OUTPUT_DIR is required (path to geekatyourspot/public).");
        }

        return Path.GetFullPath(root);
    }
}

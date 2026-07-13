using System.CommandLine;
using System.Text.Json;
using DotNetEnv;
using SectionFigures.Models;

namespace SectionFigures;

public static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task<int> Main(string[] args)
    {
        Env.TraversePath().Load();

        var projectIdOption = new Option<Guid>("--project-id")
        {
            Description = "Content Writer project GUID",
        };

        var outOption = new Option<FileInfo?>("--out", "Write jobs JSON to this file");

        var jobsOption = new Option<FileInfo?>("--jobs", "Path to jobs.json from export-jobs");

        var yesOption = new Option<bool>("--yes", "Confirm OpenAI spend without interactive prompt");

        var forceOption = new Option<bool>("--force", "Regenerate even when AVIF already exists on disk");

        var concurrencyOption = new Option<int>("--concurrency", () => 4, "Parallel OpenAI workers");

        var failFastOption = new Option<bool>("--fail-fast", "Stop batch on first OpenAI failure");

        var root = new RootCommand("SectionFigures — external section art pipeline (HTTP read, disk write, no database)");

        var exportCmd = new Command("export-jobs", "Fetch figure briefs from Content Writer HTTP API and write jobs.json");
        exportCmd.AddOption(projectIdOption);
        projectIdOption.IsRequired = true;
        exportCmd.AddOption(outOption);
        exportCmd.SetHandler(async (projectId, outFile) =>
        {
            var client = ContentWriterFiguresClient.FromEnvironment();
            var manifest = await client.ExportManifestAsync(projectId);
            var jobs = FigureJobBuilder.BuildJobs(manifest);
            var json = JsonSerializer.Serialize(jobs, JsonOptions);
            if (outFile is not null)
            {
                await File.WriteAllTextAsync(outFile.FullName, json);
                Console.WriteLine($"Wrote {outFile.FullName} ({jobs.Jobs.Count} jobs)");
            }
            else
            {
                Console.WriteLine(json);
            }
        }, projectIdOption, outOption);

        var planCmd = new Command("plan", "Dry-run: paths, disk status, cost estimate");
        planCmd.AddOption(jobsOption);
        planCmd.AddOption(projectIdOption);
        planCmd.SetHandler(async (jobsFile, projectId) =>
        {
            var outputRoot = RequireOutputRoot();
            var jobFile = await LoadJobsAsync(jobsFile, projectId);
            var summary = JobPlanner.Summarize(jobFile.Jobs, outputRoot);
            JobPlanner.PrintPlan(summary, jobFile.Jobs, outputRoot);
        }, jobsOption, projectIdOption);

        var generateCmd = new Command("generate", "Generate AVIF files from jobs.json (OpenAI → disk only, no DB write-back)");
        generateCmd.AddOption(jobsOption);
        generateCmd.AddOption(projectIdOption);
        generateCmd.AddOption(yesOption);
        generateCmd.AddOption(forceOption);
        generateCmd.AddOption(concurrencyOption);
        generateCmd.AddOption(failFastOption);
        generateCmd.SetHandler(async (jobsFile, projectId, yes, force, concurrency, failFast) =>
        {
            var outputRoot = RequireOutputRoot();
            var jobFile = await LoadJobsAsync(jobsFile, projectId);
            var summary = JobPlanner.Summarize(jobFile.Jobs, outputRoot);
            var toRun = force ? summary.TotalJobs : summary.ToGenerate;

            if (toRun > ImageGenerationDefaults.CostConfirmThreshold && !yes)
            {
                Console.Error.WriteLine(
                    $"Refusing to generate {toRun} image(s) without --yes (estimated ${summary.EstimatedCostUsd:F2} USD).");
                Environment.ExitCode = 2;
                return;
            }

            var openAi = OpenAiImageClient.FromEnvironment();
            var result = await JobGenerator.RunAsync(
                jobFile.Jobs,
                outputRoot,
                openAi,
                FigureAvifEncoder.Default,
                force,
                concurrency,
                failFast);

            Console.WriteLine(
                $"Done: succeeded={result.Succeeded} skippedExists={result.SkippedExists} failed={result.Failed}");
            if (result.Failed > 0)
            {
                Environment.ExitCode = 1;
            }
        }, jobsOption, projectIdOption, yesOption, forceOption, concurrencyOption, failFastOption);

        root.AddCommand(exportCmd);
        root.AddCommand(planCmd);
        root.AddCommand(generateCmd);

        try
        {
            return await root.InvokeAsync(args);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ex.Message.Contains("missing GeekApiSlug", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
        }
    }

    private static string RequireOutputRoot()
    {
        var root = Environment.GetEnvironmentVariable("CONTENT_IMAGE_OUTPUT_DIR");
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException(
                "CONTENT_IMAGE_OUTPUT_DIR is required (path to geekatyourspot/public).");
        }

        return Path.GetFullPath(root);
    }

    private static async Task<FigureJobFile> LoadJobsAsync(FileInfo? jobsFile, Guid projectId)
    {
        if (jobsFile is not null)
        {
            var json = await File.ReadAllTextAsync(jobsFile.FullName);
            return JsonSerializer.Deserialize<FigureJobFile>(json)
                ?? throw new InvalidOperationException($"Could not parse {jobsFile.FullName}");
        }

        if (projectId == Guid.Empty)
        {
            throw new InvalidOperationException("Provide --jobs or --project-id.");
        }

        var client = ContentWriterFiguresClient.FromEnvironment();
        var manifest = await client.ExportManifestAsync(projectId);
        return FigureJobBuilder.BuildJobs(manifest);
    }
}

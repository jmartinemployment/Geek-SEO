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

        var headingSlugOption = new Option<string?>("--heading-slug", "Heading slug from jobs.json");

        var forceOption = new Option<bool>("--force", "Regenerate even when AVIF already exists on disk");

        var root = new RootCommand("SectionFigures — one section at a time (HTTP read, disk write, agent-driven)");

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

        var planCmd = new Command("plan", "Checklist: paths and what is already on disk");
        planCmd.AddOption(jobsOption);
        planCmd.AddOption(projectIdOption);
        planCmd.SetHandler(async (jobsFile, projectId) =>
        {
            var outputRoot = EnvironmentConfig.RequireOutputRoot();
            var jobFile = await LoadJobsAsync(jobsFile, projectId);
            var summary = JobPlanner.Summarize(jobFile.Jobs, outputRoot);
            JobPlanner.PrintPlan(summary, jobFile.Jobs, outputRoot);
        }, jobsOption, projectIdOption);

        var generateOneCmd = new Command("generate-one", "OpenAI → AVIF for one section");
        generateOneCmd.AddOption(jobsOption);
        generateOneCmd.AddOption(projectIdOption);
        generateOneCmd.AddOption(headingSlugOption);
        generateOneCmd.AddOption(forceOption);
        headingSlugOption.IsRequired = true;
        generateOneCmd.SetHandler(async (jobsFile, projectId, headingSlug, force) =>
        {
            var outputRoot = EnvironmentConfig.RequireOutputRoot();
            var jobFile = await LoadJobsAsync(jobsFile, projectId);
            var slug = headingSlug!.Trim();
            var job = jobFile.Jobs.FirstOrDefault(j =>
                string.Equals(j.HeadingSlug, slug, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"No job with heading slug \"{slug}\".");

            var openAi = OpenAiImageClient.FromEnvironment();
            var result = await JobGenerator.RunAsync(
                [job],
                outputRoot,
                openAi,
                FigureAvifEncoder.Default,
                force,
                concurrency: 1,
                failFast: true);

            Console.WriteLine(
                $"Done: succeeded={result.Succeeded} skippedExists={result.SkippedExists} failed={result.Failed}");
            Console.WriteLine(JobPlanner.AbsolutePath(outputRoot, job.RelativePath));
            if (result.Failed > 0)
            {
                Environment.ExitCode = 1;
            }
        }, jobsOption, projectIdOption, headingSlugOption, forceOption);

        root.AddCommand(exportCmd);
        root.AddCommand(planCmd);
        root.AddCommand(generateOneCmd);

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

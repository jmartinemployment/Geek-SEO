using SectionFigures.Models;

namespace SectionFigures;

public sealed record PlanSummary(
    int TotalJobs,
    int AlreadyOnDisk,
    int ToGenerate,
    string Model,
    string Size,
    decimal EstimatedCostUsd);

public static class JobPlanner
{
    public static PlanSummary Summarize(IReadOnlyList<FigureJob> jobs, string outputRoot)
    {
        var already = jobs.Count(j => File.Exists(AbsolutePath(outputRoot, j.RelativePath)));
        var toGenerate = jobs.Count - already;
        return new PlanSummary(
            jobs.Count,
            already,
            toGenerate,
            ImageGenerationDefaults.OpenAiModel,
            ImageGenerationDefaults.OpenAiSize,
            toGenerate * ImageGenerationDefaults.EstimatedCostPerImageUsd);
    }

    public static void PrintPlan(PlanSummary summary, IReadOnlyList<FigureJob> jobs, string outputRoot)
    {
        Console.WriteLine($"Jobs: {summary.TotalJobs}  Skipped (already on disk): {summary.AlreadyOnDisk}  To generate: {summary.ToGenerate}");
        Console.WriteLine($"Model: {summary.Model} @ {summary.Size}");
        Console.WriteLine(
            $"Estimated cost: {summary.ToGenerate} × ${ImageGenerationDefaults.EstimatedCostPerImageUsd:F3} ≈ ${summary.EstimatedCostUsd:F2} USD (verify current OpenAI pricing)");

        foreach (var job in jobs)
        {
            var exists = File.Exists(AbsolutePath(outputRoot, job.RelativePath));
            var flag = exists ? "[exists]" : "[generate]";
            Console.WriteLine($"  {flag} {job.RelativePath}  ({job.SourceType} — {job.Heading})");
        }
    }

    public static string AbsolutePath(string outputRoot, string relativePath) =>
        Path.Combine(outputRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
}

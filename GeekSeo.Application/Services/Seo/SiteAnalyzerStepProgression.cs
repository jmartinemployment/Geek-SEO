using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

/// <summary>Step ordering rules for the Site Analyzer wizard.</summary>
public static class SiteAnalyzerStepProgression
{
    /// <summary>Keyword pack steps that may write pack artifacts — only after build validation passes.</summary>
    public static readonly IReadOnlySet<int> PackPersistSteps = new HashSet<int> { 5, 7, 8, 9 };

    public static bool IsPackPersistStep(int step) => PackPersistSteps.Contains(step);

    /// <summary>Step 10 finalizes handoff; it does not run artifact gates.</summary>
    public static bool IsHandoffStep(int step) => step == 10;

    public static bool PriorStepsGreen(int step, IReadOnlyList<SiteAnalyzerStepRunRow> runs, int minStep = 1)
    {
        for (var s = minStep; s < step; s++)
        {
            var row = runs.FirstOrDefault(r => r.StepNumber == s);
            if (row is null || !string.Equals(row.Status, "green", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}

using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public sealed record MethodologySectionPlan(
    MethodologyPhaseDefinition Phase,
    string SuggestedH2,
    IReadOnlyList<string> SubtopicsFromSerp);

/// <summary>
/// Maps SERP subtopics onto the four-phase methodology spine — competitor titles are never used as H2s.
/// </summary>
public static class MethodologySectionHintBuilder
{
    public static IReadOnlyList<MethodologySectionPlan> BuildPlans(
        string keyword,
        IEnumerable<string> serpSubtopicCandidates)
    {
        var pool = serpSubtopicCandidates
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var phases = WritingMethodologySpec.FourPhase.PhaseDefinitions;
        var plans = new List<MethodologySectionPlan>(phases.Count);
        for (var i = 0; i < phases.Count; i++)
        {
            var phase = phases[i];
            plans.Add(new MethodologySectionPlan(
                phase,
                ArticleMethodologyScaffold.SuggestTopicHeading(keyword, phase),
                pool.Skip(i * 3).Take(4).ToList()));
        }

        return plans;
    }
}

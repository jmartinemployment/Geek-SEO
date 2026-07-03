using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public static class DraftPlagiarismDraftEnricher
{
    public static async Task<string> EnsureOriginalDraftAsync(
        string html,
        WritingResearchContext research,
        IAIProvider ai,
        CancellationToken ct = default)
    {
        var report = DraftPlagiarismRules.Evaluate(html, research);
        if (report.Passed)
            return html;

        var failures = report.Failures;
        if (failures.Count == 0)
            return html;

        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = BuildRepairSystemPrompt(),
            UserPrompt = BuildRepairUserPrompt(html, failures),
            MaxTokens = 8192,
            Temperature = 0.35,
        }, ct);

        if (!response.IsSuccess || response.Value is null)
            return html;

        var repaired = AiHtmlSanitizer.ToHtmlFragment(response.Value.Content).Trim();
        return DraftPlagiarismRules.PassesAllRules(repaired, research) ? repaired : html;
    }

    private static string BuildRepairSystemPrompt() =>
        "You revise SEO article HTML to fix originality rule violations. " +
        "Rewrite only offending headings or sentences — preserve structure, h1, four methodology h2 sections, and closing FAQ. " +
        "Use original wording; do not copy SERP titles or competitor phrasing. Output HTML only.";

    private static string BuildRepairUserPrompt(string html, IReadOnlyList<PlagiarismRuleResult> failures)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Failed originality rules:");
        foreach (var failure in failures)
            builder.AppendLine($"- {failure.RuleId}: {failure.Detail}");
        builder.AppendLine();
        builder.AppendLine("Article to revise:");
        builder.AppendLine(html.Trim());
        return builder.ToString().TrimEnd();
    }
}

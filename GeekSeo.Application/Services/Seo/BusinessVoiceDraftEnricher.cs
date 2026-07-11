using System.Text.RegularExpressions;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public static partial class BusinessVoiceDraftEnricher
{
    public static async Task<string> EnsureBusinessVoiceDraftAsync(
        string html,
        WritingResearchContext research,
        IAIProvider ai,
        CancellationToken ct = default)
    {
        var pack = BusinessVoicePackBuilder.Build(research);
        if (!pack.Enabled)
            return html;

        html = EnsureCtaBeforeFaq(html, pack);

        if (BusinessVoiceValidator.PassesAllGates(html, pack))
            return html;

        var failed = BusinessVoiceValidator.FailedGates(html, pack)
            .Where(g => g.GateId != "content_cta")
            .ToList();

        if (failed.Count == 0)
            return html;

        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = BusinessVoicePrompt.BuildRepairSystemPrompt(),
            UserPrompt = BusinessVoicePrompt.BuildRepairUserPrompt(html, pack, failed),
            MaxTokens = 8192,
            Temperature = 0.45,
        }, ct);

        if (!response.IsSuccess || response.Value is null)
            return EnsureCtaBeforeFaq(html, pack);

        var repaired = AiHtmlSanitizer.ToHtmlFragment(response.Value.Content).Trim();
        repaired = EnsureCtaBeforeFaq(repaired, pack);

        if (!BusinessVoiceValidator.PassesAllGates(repaired, pack)
            && BusinessVoiceValidator.FailedGates(repaired, pack).Any(g => g.GateId != "content_cta"))
        {
            return EnsureCtaBeforeFaq(html, pack);
        }

        return repaired;
    }

    public static string EnsureCtaBeforeFaq(string html, BusinessVoicePack pack)
    {
        if (!pack.Enabled)
            return html;

        if (html.Contains("free strategy call", StringComparison.OrdinalIgnoreCase))
            return html;

        var faqStart = FindFaqSectionStart(html);
        if (faqStart < 0)
            return html.TrimEnd() + "\n" + pack.CtaParagraphHtml;

        return html[..faqStart].TrimEnd() + "\n" + pack.CtaParagraphHtml + "\n" + html[faqStart..].TrimStart();
    }

    private static int FindFaqSectionStart(string html)
    {
        var match = FaqHeadingRegex().Match(html);
        if (match.Success)
            return match.Index;

        var headingIndex = html.IndexOf(ContentWritingRules.ClosingFaqHeading, StringComparison.OrdinalIgnoreCase);
        if (headingIndex < 0)
            return -1;

        var h2Start = html.LastIndexOf("<h2", headingIndex, StringComparison.OrdinalIgnoreCase);
        return h2Start >= 0 ? h2Start : headingIndex;
    }

    [GeneratedRegex("<h2[^>]*>\\s*[^<]*faq[^<]*</h2>", RegexOptions.IgnoreCase)]
    private static partial Regex FaqHeadingRegex();
}

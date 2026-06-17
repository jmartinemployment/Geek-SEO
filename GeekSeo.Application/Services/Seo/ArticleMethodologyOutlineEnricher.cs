using System.Text.RegularExpressions;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public static partial class ArticleMethodologyOutlineEnricher
{
    public static int CountBodyH2Sections(string html)
    {
        var body = ExtractBodyBeforeFaq(html);
        return H2Regex().Matches(body).Count;
    }

    public static bool HasRequiredBodySections(string html, WritingMethodologySpec methodology) =>
        methodology.PhaseDefinitions.Count == 0
        || CountBodyH2Sections(html) >= methodology.PhaseDefinitions.Count;

    public static async Task<string> EnsureMethodologyOutlineAsync(
        string outline,
        ContentBrief brief,
        IAIProvider ai,
        CancellationToken ct = default)
    {
        var methodology = brief.Methodology;
        if (HasRequiredBodySections(outline, methodology))
            return outline;

        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ArticleMethodologyPrompt.BuildOutlineRepairSystemPrompt(),
            UserPrompt = ArticleMethodologyPrompt.BuildOutlineRepairUserPrompt(
                brief.Keyword,
                methodology,
                outline,
                brief.CompetitorHeadingHighlights),
            MaxTokens = 2048,
            Temperature = 0.5,
        }, ct);

        if (!response.IsSuccess || response.Value is null)
            return outline;

        var bodySections = AiHtmlSanitizer.ToHtmlFragment(response.Value.Content).Trim();
        if (CountBodyH2Sections(bodySections) < methodology.PhaseDefinitions.Count)
            return outline;

        var (_, faqTail) = SplitAtFaq(outline);
        if (!string.IsNullOrWhiteSpace(faqTail))
            return bodySections + "\n" + faqTail.Trim();

        return bodySections;
    }

    private static string ExtractBodyBeforeFaq(string html)
    {
        var faqStart = FindFaqSectionStart(html);
        return faqStart < 0 ? html : html[..faqStart];
    }

    private static (string Body, string? FaqTail) SplitAtFaq(string html)
    {
        var faqStart = FindFaqSectionStart(html);
        if (faqStart < 0)
            return (html.Trim(), null);

        return (html[..faqStart].TrimEnd(), html[faqStart..].TrimStart());
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

    [GeneratedRegex("<h2\\b", RegexOptions.IgnoreCase)]
    private static partial Regex H2Regex();

    [GeneratedRegex("<h2[^>]*>\\s*[^<]*faq[^<]*</h2>", RegexOptions.IgnoreCase)]
    private static partial Regex FaqHeadingRegex();
}

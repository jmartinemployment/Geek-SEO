using System.Text.RegularExpressions;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public static partial class ArticleMethodologyDraftEnricher
{
    public static string EnsureMethodologyDraft(string draft, ContentBrief brief) =>
        ArticleMethodologyScaffold.EnsureBodySections(
            draft,
            brief.Keyword,
            brief.Methodology);

    public static async Task<string> EnsureResearchMethodologyDraftAsync(
        string draft,
        ResearchDraftRequest request,
        IAIProvider ai,
        CancellationToken ct = default)
    {
        var methodology = WritingMethodologySpec.FourPhase;
        var keyword = request.Research.DerivedKeyword;
        draft = ArticleMethodologyScaffold.StripMovementLabels(draft);
        if (ArticleMethodologyScaffold.HasRequiredBodyStructure(draft, methodology))
            return draft;

        var faqStart = FindFaqSectionStart(draft);
        var bodyWithMaybeH1 = faqStart >= 0 ? draft[..faqStart] : draft;
        var faqTail = faqStart >= 0 ? draft[faqStart..].TrimStart() : string.Empty;

        var competitorHeadings = request.Research.Competitors
            .SelectMany(c => c.Headings.Where(h => h.Level <= 3).Select(h => h.Text))
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(15)
            .ToList();

        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ArticleMethodologyPrompt.BuildDraftRepairSystemPrompt(methodology),
            UserPrompt = ArticleMethodologyPrompt.BuildDraftRepairUserPrompt(
                keyword,
                methodology,
                bodyWithMaybeH1,
                request.Research.SectionHints,
                competitorHeadings),
            MaxTokens = 8192,
            Temperature = 0.4,
        }, ct);

        if (!response.IsSuccess || response.Value is null)
            return ArticleMethodologyScaffold.SanitizeDraft(draft, keyword, methodology);

        var repaired = AiHtmlSanitizer.ToHtmlFragment(response.Value.Content).Trim();
        repaired = ArticleMethodologyScaffold.StripMovementLabels(repaired);
        if (!string.IsNullOrWhiteSpace(faqTail) && FindFaqSectionStart(repaired) < 0)
            repaired = repaired.TrimEnd() + "\n" + faqTail;

        if (!ArticleMethodologyScaffold.HasRequiredBodyStructure(repaired, methodology))
            repaired = ArticleMethodologyScaffold.SanitizeDraft(repaired, keyword, methodology);

        return repaired;
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

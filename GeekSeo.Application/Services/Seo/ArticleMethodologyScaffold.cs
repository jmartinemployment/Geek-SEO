using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static partial class ArticleMethodologyScaffold
{
    public static string StripMovementLabels(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html;

        var result = html;
        result = MovementLabelParagraphRegex().Replace(result, string.Empty);
        result = MovementLabelHeadingRegex().Replace(result, string.Empty);
        result = MovementLabelPlainParagraphRegex().Replace(result, string.Empty);
        result = MovementLabelStrongRegex().Replace(result, string.Empty);
        result = EmptyParagraphRegex().Replace(result, string.Empty);
        return result.Trim();
    }

    public static string SanitizeDraft(string html, string keyword, WritingMethodologySpec methodology)
    {
        html = StripMovementLabels(html);
        return EnsureBodySections(html, keyword, methodology);
    }

    public static bool HasRequiredBodyStructure(string html, WritingMethodologySpec methodology)
    {
        if (methodology.PhaseDefinitions.Count == 0)
            return true;

        return CountBodyH2Sections(html) == methodology.PhaseDefinitions.Count;
    }

    public static string BuildDeterministicBodySections(string keyword, WritingMethodologySpec methodology)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < methodology.PhaseDefinitions.Count; i++)
        {
            var phase = methodology.PhaseDefinitions[i];
            builder.AppendLine($"<h2>{WebUtility.HtmlEncode(SuggestTopicHeading(keyword, phase))}</h2>");
            builder.AppendLine("<h3>Key points to cover</h3>");
            builder.AppendLine("<h3>Decisions for this phase</h3>");
        }

        return builder.ToString().TrimEnd();
    }

    public static string EnsureBodySections(
        string html,
        string keyword,
        WritingMethodologySpec methodology)
    {
        html = StripMovementLabels(html);
        if (methodology.PhaseDefinitions.Count == 0 || HasRequiredBodyStructure(html, methodology))
            return html;

        return EnsureBodySectionHeadings(html, keyword, methodology);
    }

    private static string EnsureBodySectionHeadings(string html, string keyword, WritingMethodologySpec methodology)
    {
        var phases = methodology.PhaseDefinitions;
        if (phases.Count == 0)
            return html;

        var faqStart = FindFaqSectionStart(html);
        var body = faqStart >= 0 ? html[..faqStart] : html;
        var tail = faqStart >= 0 ? html[faqStart..] : string.Empty;

        if (CountBodyH2Sections(body) != phases.Count)
            body = BuildDeterministicBodySections(keyword, methodology);

        return body.TrimEnd() + (string.IsNullOrWhiteSpace(tail) ? string.Empty : "\n" + tail.Trim());
    }

    public static int CountBodyH2Sections(string html) =>
        H2Regex().Matches(ExtractBodyBeforeFaq(html)).Count;

    public static string SuggestTopicHeading(string keyword, MethodologyPhaseDefinition phase)
    {
        var topic = string.IsNullOrWhiteSpace(keyword) ? "this topic" : keyword.Trim();
        var family = phase.HeadingFamilies.FirstOrDefault() ?? phase.Label;

        return phase.Id switch
        {
            "business-objectives" => $"Why {topic} matters now",
            "data-quality-assessment" => $"{TitleCase(family)} before you implement {topic}",
            "ai-tech-selection" => $"Choosing the right AI technologies for {topic}",
            "implementation-strategy" => $"Implementation strategy and rollout for {topic}",
            "tech-selection" => $"Choosing the right AI technologies for {topic}",
            "pilot-implementation" => $"Implementation strategy and rollout for {topic}",
            "scaling-safety" => $"Scaling {topic} safely across the business",
            _ => $"{TitleCase(family)} for {topic}",
        };
    }

    private static string TitleCase(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? value
            : char.ToUpperInvariant(value[0]) + value[1..];

    private static string ExtractBodyBeforeFaq(string html)
    {
        var faqStart = FindFaqSectionStart(html);
        return faqStart < 0 ? html : html[..faqStart];
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

    [GeneratedRegex(@"<p>\s*<strong>\s*Movement\s+\d+\s*[—–-][^<]*</strong>\s*</p>\s*", RegexOptions.IgnoreCase)]
    private static partial Regex MovementLabelParagraphRegex();

    [GeneratedRegex(@"<h([1-6])\b[^>]*>\s*Movement\s+\d+\s*[—–-][^<]*</h\1>\s*", RegexOptions.IgnoreCase)]
    private static partial Regex MovementLabelHeadingRegex();

    [GeneratedRegex(@"<p\b[^>]*>\s*Movement\s+\d+\s*[—–-][^<]*</p>\s*", RegexOptions.IgnoreCase)]
    private static partial Regex MovementLabelPlainParagraphRegex();

    [GeneratedRegex(@"<strong>\s*Movement\s+\d+\s*[—–-][^<]*</strong>\s*", RegexOptions.IgnoreCase)]
    private static partial Regex MovementLabelStrongRegex();

    [GeneratedRegex(@"<p\b[^>]*>\s*</p>\s*", RegexOptions.IgnoreCase)]
    private static partial Regex EmptyParagraphRegex();
}

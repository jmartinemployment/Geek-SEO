using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static partial class ArticleMethodologyScaffold
{
    public static string BuildMovementLabelHtml(int movementNumber, string phaseLabel) =>
        $"<p><strong>Movement {movementNumber} — {WebUtility.HtmlEncode(phaseLabel)}</strong></p>";

    public static bool HasVisibleMethodologyMovements(string html, WritingMethodologySpec methodology)
    {
        if (methodology.PhaseDefinitions.Count == 0)
            return true;

        var body = ExtractBodyBeforeFaq(html);
        return methodology.PhaseDefinitions.All(phase =>
            body.Contains(phase.Label, StringComparison.OrdinalIgnoreCase)
            && MovementLabelRegex().IsMatch(body)
            && body.Contains($"Movement ", StringComparison.Ordinal));
    }

    public static string BuildDeterministicBodySections(string keyword, WritingMethodologySpec methodology)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < methodology.PhaseDefinitions.Count; i++)
        {
            var phase = methodology.PhaseDefinitions[i];
            builder.AppendLine(BuildMovementLabelHtml(i + 1, phase.Label));
            builder.AppendLine($"<h2>{WebUtility.HtmlEncode(SuggestTopicHeading(keyword, phase))}</h2>");
            builder.AppendLine("<h3>Key points to cover</h3>");
            builder.AppendLine("<h3>Decisions for this phase</h3>");
        }

        return builder.ToString().TrimEnd();
    }

    public static string EnsureVisibleMovements(
        string html,
        string keyword,
        WritingMethodologySpec methodology)
    {
        if (methodology.PhaseDefinitions.Count == 0 || HasVisibleMethodologyMovements(html, methodology))
            return html;

        return InjectMovementLabels(html, keyword, methodology);
    }

    public static string InjectMovementLabels(string html, string keyword, WritingMethodologySpec methodology)
    {
        var phases = methodology.PhaseDefinitions;
        if (phases.Count == 0)
            return html;

        var faqStart = FindFaqSectionStart(html);
        var body = faqStart >= 0 ? html[..faqStart] : html;
        var tail = faqStart >= 0 ? html[faqStart..] : string.Empty;

        if (CountBodyH2Sections(body) < phases.Count)
            body = BuildDeterministicBodySections(keyword, methodology);
        else
        {
            var matches = H2Regex().Matches(body).Cast<Match>().Take(phases.Count).ToList();
            for (var i = matches.Count - 1; i >= 0; i--)
            {
                var phase = phases[i];
                var prefix = body[..matches[i].Index];
                if (prefix.Contains(phase.Label, StringComparison.OrdinalIgnoreCase)
                    && MovementLabelRegex().IsMatch(prefix))
                {
                    continue;
                }

                body = body.Insert(matches[i].Index, BuildMovementLabelHtml(i + 1, phase.Label) + "\n");
            }
        }

        return body.TrimEnd() + (string.IsNullOrWhiteSpace(tail) ? string.Empty : "\n" + tail.Trim());
    }

    public static int CountBodyH2Sections(string html) =>
        H2Regex().Matches(ExtractBodyBeforeFaq(html)).Count;

    private static string SuggestTopicHeading(string keyword, MethodologyPhaseDefinition phase)
    {
        var topic = string.IsNullOrWhiteSpace(keyword) ? "this topic" : keyword.Trim();
        var family = phase.HeadingFamilies.FirstOrDefault() ?? phase.Label;

        return phase.Id switch
        {
            "business-objectives" => $"Why {topic} matters now",
            "data-quality-assessment" => $"{TitleCase(family)} before you implement {topic}",
            "tech-selection" => $"Choosing the right stack for {topic}",
            "pilot-implementation" => $"Pilot plan and rollout for {topic}",
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

    [GeneratedRegex(@"Movement\s+\d+\s*—", RegexOptions.IgnoreCase)]
    private static partial Regex MovementLabelRegex();
}

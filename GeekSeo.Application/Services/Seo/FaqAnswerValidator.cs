using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static partial class FaqAnswerValidator
{
    public sealed record ValidationOutcome(
        IReadOnlyDictionary<string, string> AnswersById,
        IReadOnlyList<string> Skipped);

    public static ValidationOutcome ValidateAll(
        IReadOnlyList<LinkedFaqAssignment> assignments,
        IReadOnlyList<LinkedFaqResult> results)
    {
        var registry = assignments.ToDictionary(a => a.Id, StringComparer.Ordinal);
        var answers = new Dictionary<string, string>(StringComparer.Ordinal);
        var skipped = new List<string>();

        foreach (var assignment in assignments)
        {
            var result = results.FirstOrDefault(r => string.Equals(r.Id, assignment.Id, StringComparison.Ordinal));
            if (result is null || string.IsNullOrWhiteSpace(result.AnswerHtml))
            {
                skipped.Add(assignment.Id);
                continue;
            }

            var sanitized = AiHtmlSanitizer.ToHtmlFragment(result.AnswerHtml).Trim();
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                skipped.Add(assignment.Id);
                continue;
            }

            if (!registry.ContainsKey(result.Id))
            {
                skipped.Add(assignment.Id);
                continue;
            }

            var validated = ValidateSingle(assignment, sanitized);
            if (string.IsNullOrWhiteSpace(validated))
            {
                skipped.Add(assignment.Id);
                continue;
            }

            answers[assignment.Id] = validated;
        }

        return new ValidationOutcome(answers, skipped);
    }

    public static string ValidateSingle(LinkedFaqAssignment assignment, string answerHtml)
    {
        if (ContainsUnsafeScheme(answerHtml))
            return string.Empty;

        if (!assignment.IsTargetActive)
            return StripAllAnchors(answerHtml).Trim();

        var anchors = ExtractAnchors(answerHtml);
        if (anchors.Count == 0)
        {
            return InjectLink(
                StripAllAnchors(answerHtml).Trim(),
                assignment.TargetPath,
                assignment.AnchorText);
        }

        var matching = anchors
            .Where(a => string.Equals(a.Href, assignment.TargetPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matching.Count == 0)
        {
            var plain = StripAllAnchors(answerHtml).Trim();
            return InjectLink(plain, assignment.TargetPath, assignment.AnchorText);
        }

        var withoutOthers = StripAllAnchors(answerHtml).Trim();
        if (withoutOthers.Contains(assignment.AnchorText, StringComparison.Ordinal))
        {
            return ReplaceFirstOccurrence(
                withoutOthers,
                assignment.AnchorText,
                BuildAnchor(assignment.TargetPath, assignment.AnchorText));
        }

        return InjectLink(withoutOthers, assignment.TargetPath, assignment.AnchorText);
    }

    private static string InjectLink(string plain, string targetPath, string anchorText)
    {
        if (string.IsNullOrWhiteSpace(plain))
            return BuildAnchor(targetPath, anchorText);

        return $"{plain.TrimEnd()} {BuildAnchor(targetPath, anchorText)}";
    }

    private static string BuildAnchor(string targetPath, string anchorText) =>
        $"<a href=\"{WebUtility.HtmlEncode(targetPath)}\">{WebUtility.HtmlEncode(anchorText)}</a>";

    private static string ReplaceFirstOccurrence(string text, string search, string replacement)
    {
        var index = text.IndexOf(search, StringComparison.Ordinal);
        return index < 0 ? text : string.Concat(text.AsSpan(0, index), replacement, text.AsSpan(index + search.Length));
    }

    private static bool ContainsUnsafeScheme(string html) =>
        html.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
        html.Contains("data:", StringComparison.OrdinalIgnoreCase);

    private static string StripAllAnchors(string html) =>
        AnchorRegex().Replace(html, m => WebUtility.HtmlDecode(m.Groups[2].Value));

    private static List<(string Href, string Text)> ExtractAnchors(string html)
    {
        var list = new List<(string Href, string Text)>();
        foreach (Match match in AnchorRegex().Matches(html))
        {
            list.Add((match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim()));
        }

        return list;
    }

    [GeneratedRegex(@"<a\s+[^>]*href\s*=\s*[""']([^""']+)[""'][^>]*>(.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex AnchorRegex();
}

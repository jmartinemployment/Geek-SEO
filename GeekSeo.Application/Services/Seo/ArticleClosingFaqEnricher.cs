using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public static partial class ArticleClosingFaqEnricher
{
    public static int CountClosingFaqs(string html)
    {
        var section = ExtractFaqSectionBody(html);
        return section is null ? 0 : FaqH3Regex().Matches(section).Count;
    }

    public static async Task<string> AppendAdditionalClosingFaqsAsync(
        string html,
        string keyword,
        int additionalCount,
        IAIProvider ai,
        CancellationToken ct = default)
    {
        if (additionalCount <= 0)
            return html;

        if (!HasClosingFaqSection(html))
            return await EnsureClosingFaqDraftAsync(html, keyword, [], ai, ct);

        var existing = ExtractExistingFaqQuestions(html);
        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt =
                $"Write ONLY {additionalCount} new FAQ entries in HTML as <h3>question</h3> followed by a concise <p> answer (2-4 sentences). " +
                "Do not repeat existing questions. No <h2>, no markdown fences.",
            UserPrompt =
                $"Blog spoke keyword: {keyword}\n" +
                "Existing FAQ questions (must not duplicate):\n" +
                (existing.Count > 0 ? string.Join('\n', existing.Select((q, i) => $"{i + 1}. {q}")) : "(none)") +
                $"\n\nWrite exactly {additionalCount} new distinct questions with answers.",
            MaxTokens = 1536,
            Temperature = 0.5,
        }, ct);

        if (!response.IsSuccess || response.Value is null)
            return html;

        var fragment = AiHtmlSanitizer.ToHtmlFragment(response.Value.Content).Trim();
        if (string.IsNullOrWhiteSpace(fragment))
            return html;

        return AppendToFaqSection(html, fragment);
    }

    public static bool HasClosingFaqSection(string html)
    {
        var section = ExtractFaqSectionBody(html);
        if (section is null)
            return false;

        return FaqH3Regex().Matches(section).Count >= ContentWritingRules.ClosingFaqCount;
    }

    public static bool HasCompleteClosingFaqSection(string html)
    {
        var section = ExtractFaqSectionBody(html);
        if (section is null)
            return false;

        if (FaqH3Regex().Matches(section).Count < ContentWritingRules.ClosingFaqCount)
            return false;

        const string placeholder = "Expand this answer in the editor.";
        var substantiveAnswers = 0;
        foreach (Match match in FaqAnswerParagraphRegex().Matches(section))
        {
            var text = StripTags(match.Groups[1].Value);
            if (string.IsNullOrWhiteSpace(text))
                continue;
            if (text.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
                continue;
            if (CountWords(text) < 12)
                continue;

            substantiveAnswers++;
        }

        return substantiveAnswers >= ContentWritingRules.ClosingFaqCount;
    }

    public static async Task<string> EnsureClosingFaqDraftAsync(
        string html,
        string keyword,
        IEnumerable<string> serpPaaQuestions,
        IAIProvider ai,
        CancellationToken ct = default)
    {
        if (HasCompleteClosingFaqSection(html))
            return html;

        html = RemoveIncompleteClosingFaqSection(html);
        var questions = ContentWritingRules.BuildClosingFaqQuestions(keyword, serpPaaQuestions, null);
        return await EnsureClosingFaqDraftCoreAsync(html, keyword, questions, ai, ct);
    }

    public static string EnsureClosingFaqOutline(string outline, ContentBrief brief)
    {
        if (HasClosingFaqSection(outline))
            return outline;

        var questions = ResolveQuestions(brief);
        var builder = new System.Text.StringBuilder(outline.TrimEnd());
        builder.Append('\n');
        builder.Append("<h2>").Append(ContentWritingRules.ClosingFaqHeading).Append("</h2>\n");
        foreach (var question in questions)
            builder.Append("<h3>").Append(WebUtility.HtmlEncode(question)).Append("</h3>\n");

        return builder.ToString().TrimEnd();
    }

    public static async Task<string> EnsureClosingFaqDraftAsync(
        string html,
        ContentBrief brief,
        IAIProvider ai,
        CancellationToken ct = default)
    {
        if (HasCompleteClosingFaqSection(html))
            return html;

        html = RemoveIncompleteClosingFaqSection(html);
        var questions = ResolveQuestions(brief);
        return await EnsureClosingFaqDraftCoreAsync(html, brief.Keyword, questions, ai, ct);
    }

    public static async Task<string> EnsureClosingFaqDraftAsync(
        string html,
        WritingResearchContext research,
        IAIProvider ai,
        CancellationToken ct = default)
    {
        if (HasCompleteClosingFaqSection(html))
            return html;

        html = RemoveIncompleteClosingFaqSection(html);

        var questions = ResolveQuestions(research);
        return await EnsureClosingFaqDraftCoreAsync(html, research.DerivedKeyword, questions, ai, ct);
    }

    private static async Task<string> EnsureClosingFaqDraftCoreAsync(
        string html,
        string keyword,
        IReadOnlyList<string> questions,
        IAIProvider ai,
        CancellationToken ct)
    {
        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt =
                $"Write ONLY the closing FAQ section in HTML. Start with <h2>{ContentWritingRules.ClosingFaqHeading}</h2>. " +
                $"Use exactly {ContentWritingRules.ClosingFaqCount} questions as <h3> elements with a concise <p> answer (2-4 sentences) under each. No markdown fences.",
            UserPrompt =
                $"Keyword: {keyword}\n" +
                "Answer these questions in order:\n" +
                string.Join('\n', questions.Select((question, index) => $"{index + 1}. {question}")),
            MaxTokens = 2048,
            Temperature = 0.5,
        }, ct);

        if (!response.IsSuccess || response.Value is null)
            return AppendPlaceholderFaq(html, questions);

        var faqHtml = AiHtmlSanitizer.ToHtmlFragment(response.Value.Content);
        if (!HasClosingFaqSection(faqHtml))
            faqHtml = AppendPlaceholderFaq(string.Empty, questions).Trim();

        return html.TrimEnd() + "\n" + faqHtml.Trim();
    }

    private static IReadOnlyList<string> ResolveQuestions(WritingResearchContext research) =>
        research.ClosingFaqs.Count > 0
            ? research.ClosingFaqs.OrderBy(f => f.DisplayOrder).Select(f => f.Question).ToList()
            : ContentWritingRules.BuildClosingFaqQuestions(
                research.DerivedKeyword,
                research.PeopleAlsoAsk.Select(p => p.Question).ToList(),
                []);

    private static IReadOnlyList<string> ResolveQuestions(ContentBrief brief) =>
        brief.ClosingFaqQuestions.Count > 0
            ? brief.ClosingFaqQuestions
            : ContentWritingRules.BuildClosingFaqQuestions(
                brief.Keyword,
                brief.PeopleAlsoAsk,
                brief.NicheContext.GapTopics);

    private static string AppendPlaceholderFaq(string html, IReadOnlyList<string> questions)
    {
        var builder = new System.Text.StringBuilder(html.TrimEnd());
        if (builder.Length > 0)
            builder.Append('\n');
        builder.Append("<h2>").Append(ContentWritingRules.ClosingFaqHeading).Append("</h2>\n");
        foreach (var question in questions.Take(ContentWritingRules.ClosingFaqCount))
        {
            builder.Append("<h3>").Append(WebUtility.HtmlEncode(question)).Append("</h3>\n");
            builder.Append("<p>Expand this answer in the editor.</p>\n");
        }

        return builder.ToString().TrimEnd();
    }

    private static string RemoveIncompleteClosingFaqSection(string html)
    {
        if (HasCompleteClosingFaqSection(html))
            return html;

        var faqStart = FindFaqSectionStart(html);
        if (faqStart < 0)
            return html;

        var tail = html[faqStart..];
        var nextH2 = NextH2Regex().Match(tail);
        var removeLength = nextH2.Success && nextH2.Index > 0 ? nextH2.Index : tail.Length;
        return html[..faqStart].TrimEnd();
    }

    private static string? ExtractFaqSectionBody(string html)
    {
        var faqStart = FindFaqSectionStart(html);
        if (faqStart < 0)
            return null;

        var tail = html[faqStart..];
        var nextH2 = NextH2Regex().Match(tail);
        if (nextH2.Success && nextH2.Index > 0)
            tail = tail[..nextH2.Index];

        return tail;
    }

    private static int FindFaqSectionStart(string html)
    {
        foreach (Match match in H2InnerRegex().Matches(html))
        {
            if (!IsFaqHeading(StripTags(match.Groups[1].Value)))
                continue;

            return match.Index;
        }

        return -1;
    }

    private static bool IsFaqHeading(string text)
    {
        var normalized = Regex.Replace(text.Trim(), @"\s+", " ");
        return normalized.Equals(ContentWritingRules.ClosingFaqHeading, StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("FAQ", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("FAQs", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripTags(string html) =>
        Regex.Replace(html, "<[^>]+>", string.Empty).Trim();

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static List<string> ExtractExistingFaqQuestions(string html)
    {
        var section = ExtractFaqSectionBody(html);
        if (section is null)
            return [];

        var questions = new List<string>();
        foreach (Match match in FaqH3InnerRegex().Matches(section))
        {
            var text = StripTags(match.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(text))
                questions.Add(text);
        }

        return questions;
    }

    private static string AppendToFaqSection(string html, string fragment)
    {
        var faqStart = FindFaqSectionStart(html);
        if (faqStart < 0)
            return html.TrimEnd() + "\n" + fragment;

        var tail = html[faqStart..];
        var nextH2 = NextH2Regex().Match(tail);
        var insertAt = nextH2.Success && nextH2.Index > 0
            ? faqStart + nextH2.Index
            : html.Length;

        return html[..insertAt].TrimEnd() + "\n" + fragment.Trim() + html[insertAt..];
    }

    [GeneratedRegex("<h3\\b[^>]*>(.*?)</h3>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex FaqH3InnerRegex();

    [GeneratedRegex("<h3\\b", RegexOptions.IgnoreCase)]
    private static partial Regex FaqH3Regex();

    [GeneratedRegex("<h2\\b[^>]*>(.*?)</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex H2InnerRegex();

    [GeneratedRegex("<h2\\b", RegexOptions.IgnoreCase)]
    private static partial Regex NextH2Regex();

    [GeneratedRegex("<p\\b[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex FaqAnswerParagraphRegex();
}

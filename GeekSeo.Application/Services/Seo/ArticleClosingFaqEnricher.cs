using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public static partial class ArticleClosingFaqEnricher
{
    public static bool HasClosingFaqSection(string html)
    {
        var faqStart = FindFaqSectionStart(html);
        if (faqStart < 0)
            return false;

        var tail = html[faqStart..];
        return FaqH3Regex().Matches(tail).Count >= ContentWritingRules.ClosingFaqCount;
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
        if (HasClosingFaqSection(html))
            return html;

        var questions = ResolveQuestions(brief);
        return await EnsureClosingFaqDraftCoreAsync(html, brief.Keyword, questions, ai, ct);
    }

    public static async Task<string> EnsureClosingFaqDraftAsync(
        string html,
        WritingResearchContext research,
        IAIProvider ai,
        CancellationToken ct = default)
    {
        if (HasClosingFaqSection(html))
            return html;

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

    [GeneratedRegex("<h3\\b", RegexOptions.IgnoreCase)]
    private static partial Regex FaqH3Regex();

    [GeneratedRegex("<h2[^>]*>\\s*[^<]*faq[^<]*</h2>", RegexOptions.IgnoreCase)]
    private static partial Regex FaqHeadingRegex();
}

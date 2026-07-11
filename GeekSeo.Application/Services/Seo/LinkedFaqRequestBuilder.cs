using System.Text.Json;
using System.Text.RegularExpressions;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static partial class LinkedFaqRequestBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static LinkedFaqEnrichmentRequest Build(
        string pillarKeyword,
        string contentHtml,
        SiteWritingFocus? siteFocus,
        IReadOnlyList<LinkedFaqAssignment> assignments) =>
        new(
            BusinessContext: siteFocus is null
                ? string.Empty
                : SiteWritingFocusSerializer.ToBusinessContext(siteFocus),
            PillarKeyword: pillarKeyword,
            CurrentHtmlExcerpt: TruncatePlain(contentHtml, 2000),
            FaqAssignments: assignments);

    public static string SerializeRequest(LinkedFaqEnrichmentRequest request) =>
        JsonSerializer.Serialize(new
        {
            businessContext = request.BusinessContext,
            pillarKeyword = request.PillarKeyword,
            currentHtmlExcerpt = request.CurrentHtmlExcerpt,
            faqAssignments = request.FaqAssignments.Select(a => new
            {
                id = a.Id,
                question = a.Question,
                targetPath = a.TargetPath,
                anchorText = a.AnchorText,
                isTargetActive = a.IsTargetActive,
            }),
        }, JsonOptions);

    public static string BuildSystemPrompt() =>
        """
        You are a deterministic HTML content enricher. Generate short, conversion-focused FAQ answers and embed a single exact anchor link per item based ONLY on the Link Target Registry in faqAssignments.

        You may ONLY use href values explicitly provided when isTargetActive is true.
        If targetPath is empty or isTargetActive is false, do NOT output an <a> tag.
        Do NOT invent paths, guess slugs, or add markdown links.

        Length and style: 2-4 sentences; direct answer; qualify buyers using businessContext; match tone of currentHtmlExcerpt.
        If isTargetActive is true, embed exactly one link: <a href="{targetPath}">{anchorText}</a> with anchorText verbatim.
        If isTargetActive is false, answer completely with no links; anchorText may appear as plain text.

        Return ONLY valid JSON matching: {"faqResults":[{"id":"faq-01","question":"...","answerHtml":"..."}]}
        answerHtml is inner paragraph content only (no h2/h3/p wrapper). No markdown fences.
        """;

    private static string TruncatePlain(string html, int maxChars)
    {
        var plain = StripHtml(html);
        return plain.Length <= maxChars ? plain : plain[..maxChars];
    }

    private static string StripHtml(string html) =>
        string.IsNullOrWhiteSpace(html)
            ? string.Empty
            : HtmlTagRegex().Replace(html, " ").Replace("&nbsp;", " ", StringComparison.Ordinal).Trim();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}

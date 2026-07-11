using System.Text.Json;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ArticleSchemaBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string AppendSchemaScripts(string articleHtml, ContentBrief brief, string title)
    {
        var scripts = BuildScripts(brief, title, articleHtml);
        if (scripts.Count == 0)
            return articleHtml;

        return $"{articleHtml}\n{string.Join("\n", scripts)}";
    }

    public static IReadOnlyList<string> BuildScripts(ContentBrief brief, string title, string articleHtml)
    {
        var scripts = new List<string>
        {
            BuildScript(new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = brief.SchemaBlueprint.PrimaryType,
                ["headline"] = title,
                ["keywords"] = brief.Keyword,
                ["about"] = brief.SchemaBlueprint.AboutEntities
                    .Select(name => new Dictionary<string, object?>
                    {
                        ["@type"] = "Thing",
                        ["name"] = name,
                    })
                    .ToList(),
                ["author"] = string.IsNullOrWhiteSpace(brief.AuthorOrganizationName)
                    ? null
                    : new Dictionary<string, object?>
                    {
                        ["@type"] = "Organization",
                        ["name"] = brief.AuthorOrganizationName,
                        ["url"] = brief.AuthorOrganizationUrl,
                    },
            }),
        };

        foreach (var software in brief.SchemaBlueprint.SoftwareEntities)
        {
            scripts.Add(BuildScript(new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "SoftwareApplication",
                ["name"] = software,
                ["applicationCategory"] = "BusinessApplication",
            }));
        }

        var faqQuestions = ResolveFaqQuestions(brief);
        if (brief.SchemaBlueprint.AdditionalTypes.Contains("FAQPage", StringComparer.OrdinalIgnoreCase)
            && faqQuestions.Count > 0)
        {
            var answers = ExtractFaqAnswers(articleHtml, faqQuestions);
            scripts.Add(BuildScript(new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "FAQPage",
                ["mainEntity"] = faqQuestions.Select(question => new Dictionary<string, object?>
                {
                    ["@type"] = "Question",
                    ["name"] = question,
                    ["acceptedAnswer"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Answer",
                        ["text"] = answers.TryGetValue(question, out var answer) ? answer : string.Empty,
                    },
                }).ToList(),
            }));
        }

        return scripts;
    }

    public static IReadOnlyList<string> BuildScripts(WritingResearchContext research, string title, string articleHtml)
    {
        var brief = new ContentBrief
        {
            Keyword = research.DerivedKeyword,
            Location = research.SearchLocation,
            ClosingFaqQuestions = research.ClosingFaqs
                .OrderBy(f => f.DisplayOrder)
                .Select(f => f.Question)
                .Take(ContentWritingRules.ClosingFaqCount)
                .ToList(),
            PeopleAlsoAsk = research.PeopleAlsoAsk.Select(p => p.Question).ToList(),
            SchemaBlueprint = new SchemaBlueprint
            {
                PrimaryType = "TechArticle",
                AdditionalTypes = ["FAQPage"],
            },
        };

        return BuildScripts(brief, title, articleHtml);
    }

    private static IReadOnlyList<string> ResolveFaqQuestions(ContentBrief brief)
    {
        if (brief.ClosingFaqQuestions.Count > 0)
            return brief.ClosingFaqQuestions.Take(ContentWritingRules.ClosingFaqCount).ToList();

        return ContentWritingRules
            .BuildClosingFaqQuestions(brief.Keyword, brief.PeopleAlsoAsk, brief.NicheContext.GapTopics)
            .Take(ContentWritingRules.ClosingFaqCount)
            .ToList();
    }

    private static string BuildScript(object payload) =>
        $"<script type=\"application/ld+json\">{JsonSerializer.Serialize(payload, JsonOptions)}</script>";

    private static Dictionary<string, string> ExtractFaqAnswers(string articleHtml, IReadOnlyList<string> questions)
    {
        var answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var question in questions)
        {
            var index = articleHtml.IndexOf(question, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                continue;

            var answerStart = articleHtml.IndexOf("<p>", index, StringComparison.OrdinalIgnoreCase);
            var answerEnd = answerStart >= 0
                ? articleHtml.IndexOf("</p>", answerStart, StringComparison.OrdinalIgnoreCase)
                : -1;
            if (answerStart < 0 || answerEnd <= answerStart)
                continue;

            var paragraphHtml = articleHtml[(answerStart + 3)..answerEnd];
            answers[question] = HtmlTextUtility.StripHtml(paragraphHtml);
        }

        return answers;
    }
}

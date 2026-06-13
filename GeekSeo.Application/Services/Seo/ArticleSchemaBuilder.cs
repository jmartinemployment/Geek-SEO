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
            BuildScript(new
            {
                @context = "https://schema.org",
                @type = brief.SchemaBlueprint.PrimaryType,
                headline = title,
                keywords = brief.Keyword,
                about = brief.SchemaBlueprint.AboutEntities.Select(name => new { @type = "Thing", name }),
                author = string.IsNullOrWhiteSpace(brief.AuthorOrganizationName)
                    ? null
                    : new
                    {
                        @type = "Organization",
                        name = brief.AuthorOrganizationName,
                        url = brief.AuthorOrganizationUrl,
                    },
            }),
        };

        foreach (var software in brief.SchemaBlueprint.SoftwareEntities)
        {
            scripts.Add(BuildScript(new
            {
                @context = "https://schema.org",
                @type = "SoftwareApplication",
                name = software,
                applicationCategory = "BusinessApplication",
            }));
        }

        if (brief.SchemaBlueprint.AdditionalTypes.Contains("FAQPage", StringComparer.OrdinalIgnoreCase)
            && brief.PeopleAlsoAsk.Count > 0)
        {
            var answers = ExtractFaqAnswers(articleHtml, brief.PeopleAlsoAsk);
            scripts.Add(BuildScript(new
            {
                @context = "https://schema.org",
                @type = "FAQPage",
                mainEntity = brief.PeopleAlsoAsk.Select(question => new
                {
                    @type = "Question",
                    name = question,
                    acceptedAnswer = new
                    {
                        @type = "Answer",
                        text = answers.TryGetValue(question, out var answer) ? answer : string.Empty,
                    },
                }),
            }));
        }

        return scripts;
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

using System.Net;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class FaqHtmlAssembler
{
    public static string Build(
        IReadOnlyList<LinkedFaqAssignment> assignments,
        IReadOnlyDictionary<string, string> answersById)
    {
        if (assignments.Count == 0)
            return string.Empty;

        var builder = new System.Text.StringBuilder();
        builder.Append("<h2>").Append(ContentWritingRules.ClosingFaqHeading).Append("</h2>\n");

        foreach (var assignment in assignments)
        {
            if (!answersById.TryGetValue(assignment.Id, out var answer) || string.IsNullOrWhiteSpace(answer))
                continue;

            builder.Append("<h3>").Append(WebUtility.HtmlEncode(assignment.Question)).Append("</h3>\n");
            builder.Append("<p>").Append(answer.Trim()).Append("</p>\n");
        }

        return builder.ToString().TrimEnd();
    }
}

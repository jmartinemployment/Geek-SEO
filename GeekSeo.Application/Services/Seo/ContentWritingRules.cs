namespace GeekSeo.Application.Services.Seo;

public static class ContentWritingRules
{
    public const int ClosingFaqCount = 5;
    public const string ClosingFaqHeading = "Frequently Asked Questions";

    public static IReadOnlyList<string> BuildClosingFaqQuestions(
        string keyword,
        IEnumerable<string> serpPaaQuestions,
        IEnumerable<string>? gapTopics = null)
    {
        var questions = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return;

            var trimmed = question.Trim();
            if (!seen.Add(trimmed))
                return;

            questions.Add(trimmed);
        }

        foreach (var question in SerpQuestionFilter.FilterForKeyword(keyword, serpPaaQuestions))
        {
            Add(question);
            if (questions.Count >= ClosingFaqCount)
                return questions.Take(ClosingFaqCount).ToList();
        }

        if (gapTopics is not null)
        {
            foreach (var gap in gapTopics)
            {
                if (string.IsNullOrWhiteSpace(gap))
                    continue;

                var gapQuestion = gap.Contains('?', StringComparison.Ordinal)
                    ? gap.Trim()
                    : $"How does {gap.Trim()} relate to {keyword}?";
                Add(gapQuestion);
                if (questions.Count >= ClosingFaqCount)
                    return questions.Take(ClosingFaqCount).ToList();
            }
        }

        foreach (var template in BuildFallbackQuestions(keyword))
        {
            Add(template);
            if (questions.Count >= ClosingFaqCount)
                break;
        }

        return questions.Take(ClosingFaqCount).ToList();
    }

    private static IEnumerable<string> BuildFallbackQuestions(string keyword)
    {
        yield return $"What is {keyword}?";
        yield return $"How much does {keyword} cost?";
        yield return $"How long does {keyword} take to implement?";
        yield return $"What are the main benefits of {keyword}?";
        yield return $"Who should consider {keyword}?";
        yield return $"What mistakes should businesses avoid with {keyword}?";
    }
}

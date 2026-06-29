namespace SiteAnalyzer2.Serp.Models;

public record SerpQuery(string Keyword, string? Location = null, int NumResults = 20);

public record SerpOrganicResult(int Position, string Url, string Title, string Snippet, string Domain);

public record SerpPaaQuestion(int Sequence, string QuestionText);

public record SerpResultSet(
    IReadOnlyList<SerpOrganicResult> Results,
    IReadOnlyList<SerpPaaQuestion> PaaQuestions,
    string? PacingWarning = null,
    int SerpPageNumber = 1,
    SerpParsedFeatures? Features = null);

namespace GeekSeo.Application.Services.Seo;

public static class OperatorResearchQueryPack
{
    public const string JunkExclusionSuffix =
        "-template -pdf -generator -reddit -quora -course -syllabus";

    public static IReadOnlyList<OperatorResearchQueryTemplate> Build(OperatorResearchQueryOptions options)
    {
        var keyword = options.Keyword.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
            return [];

        var phrase = QuotePhrase(keyword);
        var junk = JunkExclusionSuffix;

        var queries = new List<OperatorResearchQueryTemplate>
        {
            new("citations_wikipedia", "Wikipedia", $"{phrase} site:en.wikipedia.org {junk}"),
            new(
                "citations_government",
                "Government / standards",
                $"{phrase} (site:nist.gov OR site:ftc.gov OR site:usa.gov OR site:cdc.gov OR site:nih.gov) {junk}"),
            new("citations_research", "Research (.edu)", $"{phrase} site:edu {junk}"),
            new("citations_pdf", "Research PDF", $"{phrase} filetype:pdf site:edu {junk}"),
            new("paa_supplement", "Question variants", $"{phrase} (how OR why OR what OR when OR cost OR vs) {junk}"),
        };

        return queries;
    }

    public static string QuotePhrase(string keyword) =>
        $"\"{keyword.Replace("\"", string.Empty, StringComparison.Ordinal).Trim()}\"";
}

public sealed record OperatorResearchQueryOptions
{
    public required string Keyword { get; init; }
    public string TargetSiteUrl { get; init; } = string.Empty;
    public string LocalCity { get; init; } = string.Empty;
    public DateOnly NewsAfterDate { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-18));
}

public sealed record OperatorResearchQueryTemplate(
    string Bucket,
    string Label,
    string Query,
    string SearchEngine = "google");

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
        var afterDate = options.NewsAfterDate.ToString("yyyy-MM-dd");
        var local = options.LocalCity.Trim();
        var domain = NormalizeDomain(options.TargetSiteUrl);

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
            new("featured_snippet", "Featured snippet", $"what is {phrase}"),
            new("featured_snippet_alt", "Featured snippet (alt)", $"{phrase} definition"),
            new("news", "Recent news", $"{phrase} after:{afterDate} -template -pdf -generator -reddit -quora"),
            new(
                "contrast_traditional",
                "Traditional vs AI",
                $"{phrase} spreadsheet OR workshop OR whiteboard -AI -template -pdf -generator"),
        };

        if (!string.IsNullOrWhiteSpace(local))
        {
            queries.Add(new(
                "local_angle",
                "Local SMB angle",
                $"{phrase} \"small business\" \"{local}\" -template -pdf -generator -reddit -quora -course"));
        }

        if (!string.IsNullOrWhiteSpace(domain))
            queries.Add(new("own_site", "Your site pages", $"site:{domain} {phrase}"));

        queries.Add(new("scholar", "Google Scholar", phrase, SearchEngine: "google_scholar"));

        return queries;
    }

    public static string QuotePhrase(string keyword) =>
        $"\"{keyword.Replace("\"", string.Empty, StringComparison.Ordinal).Trim()}\"";

    private static string NormalizeDomain(string? siteUrl)
    {
        if (string.IsNullOrWhiteSpace(siteUrl))
            return string.Empty;

        if (!Uri.TryCreate(siteUrl.Trim(), UriKind.Absolute, out var uri))
            return siteUrl.Trim().TrimStart('/');

        var host = uri.Host;
        return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
    }
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

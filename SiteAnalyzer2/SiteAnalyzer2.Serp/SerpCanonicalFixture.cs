namespace SiteAnalyzer2.Serp;

/// <summary>
/// Canonical saved Google SERP HTML used for parser tests, integration tests, and inline dev import.
/// File: <c>tests/fixtures/serp/ai-market-intelligence-analytics.html</c>
/// </summary>
public static class SerpCanonicalFixture
{
    public const string HtmlFileName = "ai-market-intelligence-analytics.html";

    /// <summary>Keyword inferred from the saved page title / search box.</summary>
    public const string Keyword = "ai Market Intelligence & Analytics";

    /// <summary>Expected parse counts from the canonical HTML (C# parser). Update when the HTML file changes.</summary>
    public static class Expected
    {
        public const int Organic = 8;
        public const int Paid = 5;
        public const int AiOverview = 1;
        public const int RelatedSearchesBlocks = 1;
        public const int RelatedQueries = 8;
        public const int TotalItems = 15;
        public const int Page = 1;
        public const long SeResultsCount = 389_000_000;
        public const string FirstOrganicDomain = "alpha-sense.com";
        public const string SecondOrganicDomain = "improvado.io";
        public const string ThirdOrganicDomain = "quantilope.com";
        public const string ThirdOrganicDescriptionFragment = "top AI tools for market research";
        public const string SecondOrganicWebsiteName = "Improvado";
        public const string SecondOrganicPreSnippet = "Jun 2, 2026";
    }
}

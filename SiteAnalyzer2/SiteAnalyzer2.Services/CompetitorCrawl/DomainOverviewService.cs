using SiteAnalyzer2.Services.Parsing;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.CompetitorCrawl;

/// <summary>
/// Domain Overview — organic positions from owned SERP imports plus optional page snapshot.
/// </summary>
public sealed class DomainOverviewService(
    IHttpClientFactory httpClientFactory,
    PageExtractionService extractionService,
    OwnedDomainIndexService ownedIndex)
{
    public async Task<DomainOverviewDto?> GetAsync(string domainOrUrl, CancellationToken ct = default)
    {
        var input = DomainOverviewInput.Parse(domainOrUrl);
        if (input is null)
            return null;

        var index = await ownedIndex.LoadAsync(input.Domain, ct);
        return BuildDto(input, fetched: null, index, []);
    }

    public async Task<DomainOverviewDto?> AnalyzeAsync(string domainOrUrl, CancellationToken ct = default)
    {
        var input = DomainOverviewInput.Parse(domainOrUrl);
        if (input is null)
            return null;

        var warnings = new List<string>();
        var index = await ownedIndex.LoadAsync(input.Domain, ct);
        var client = httpClientFactory.CreateClient(nameof(DomainOverviewService));
        client.Timeout = TimeSpan.FromSeconds(45);

        try
        {
            using var response = await client.GetAsync(input.AnalyzedUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            var html = response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync(ct)
                : null;

            if (string.IsNullOrWhiteSpace(html))
            {
                warnings.Add($"Fetch returned no HTML (HTTP {(int)response.StatusCode}).");
                return BuildDto(
                    input,
                    new FetchedPage((int)response.StatusCode, null, null, null, [], false),
                    index,
                    warnings);
            }

            if (LooksLikeNonHtmlShell(html))
            {
                warnings.Add(
                    "Site returned non-HTML content to bots. Try a specific URL path (e.g. /expense-management).");
                var markdown = ExtractMarkdownStructure(html);
                if (markdown is { } md && (md.Title is not null || md.H2Count > 0))
                {
                    return BuildDto(
                        input,
                        new FetchedPage(
                            (int)response.StatusCode,
                            md.Title,
                            null,
                            md.H2Count,
                            [],
                            true),
                        index,
                        warnings);
                }
            }

            var uri = response.RequestMessage?.RequestUri ?? new Uri(input.AnalyzedUrl);
            var extracted = extractionService.Extract(html, uri, input.Domain);

            var title = ResolveTitle(extracted);
            var description = ResolveMeta(extracted, "description", "og:description");
            var h2Count = extracted.Headings.Count(h => h.Level == 2);
            var schemaTypes = extracted.JsonLdBlocks
                .Select(b => b.ParsedType)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (string.IsNullOrWhiteSpace(title) && h2Count == 0 && schemaTypes.Count == 0)
                warnings.Add("No title, headings, or schema were extracted.");

            var fetched = new FetchedPage(
                (int)response.StatusCode,
                title,
                description,
                h2Count,
                schemaTypes,
                true);

            return BuildDto(input, fetched, index, warnings);
        }
        catch (Exception ex)
        {
            warnings.Add(ex.Message);
            return BuildDto(input, null, index, warnings);
        }
    }

    private static DomainOverviewDto BuildDto(
        DomainOverviewInput input,
        FetchedPage? fetched,
        OwnedDomainIndexSnapshot index,
        IReadOnlyList<string> warnings)
    {
        var positions = EnrichPositions(input, fetched, index.Positions);

        return new DomainOverviewDto
        {
            Domain = input.Domain,
            SiteRootUrl = input.SiteRootUrl,
            AnalyzedUrl = input.AnalyzedUrl,
            RequestedInput = input.RequestedInput,
            Scope = input.Scope == DomainOverviewScope.Url ? "url" : "domain",
            PageFetched = fetched?.Fetched == true,
            HttpStatus = fetched?.HttpStatus,
            PageTitle = fetched?.Title,
            MetaDescription = fetched?.MetaDescription,
            H2Count = fetched?.H2Count,
            SchemaTypes = fetched?.SchemaTypes ?? [],
            OrganicKeywordsCount = index.DistinctKeywordCount,
            TotalPositionsCount = positions.Count,
            ResearchImportCount = index.ContributingImportCount,
            Positions = positions,
            Message = BuildMessage(index, fetched, positions, input),
            Warnings = warnings,
        };
    }

    private static List<DomainOrganicPositionRow> EnrichPositions(
        DomainOverviewInput input,
        FetchedPage? fetched,
        IReadOnlyList<DomainOrganicPositionRow> positions)
    {
        var list = positions.ToList();
        if (fetched?.Fetched != true || list.Count == 0)
            return list;

        var keyword = list[0].Keyword;
        var pathMatch = KeywordPathMatcher.Score(keyword, input.AnalyzedUrl);
        if (pathMatch is not ("exact" or "strong"))
            return list;

        if (list.Any(p => string.Equals(p.Url, input.AnalyzedUrl, StringComparison.OrdinalIgnoreCase)))
            return list;

        list.Insert(0, new DomainOrganicPositionRow
        {
            Keyword = keyword,
            Position = 0,
            Intent = list[0].Intent,
            Url = input.AnalyzedUrl,
            SerpFeatures = "on-page match",
            PathMatch = pathMatch,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        return list;
    }

    private static string BuildMessage(
        OwnedDomainIndexSnapshot index,
        FetchedPage? fetched,
        IReadOnlyList<DomainOrganicPositionRow> positions,
        DomainOverviewInput input)
    {
        var pathMatch = positions.FirstOrDefault(p =>
            string.Equals(p.Url, input.AnalyzedUrl, StringComparison.OrdinalIgnoreCase))?.PathMatch;

        if (index.DistinctKeywordCount > 0)
        {
            var imports = index.ContributingImportCount == 1 ? "import" : "imports";
            var matchNote = pathMatch is "exact" or "strong"
                ? " This URL is a strong keyword match — SERP showed a different ranking page."
                : "";
            return
                $"{index.DistinctKeywordCount} keyword(s) from {index.ContributingImportCount} research {imports}.{matchNote}";
        }

        if (fetched?.Fetched == true)
            return "No positions in sa2 yet. Page snapshot fetched — import SERP HTML for keywords where this domain ranks.";

        return "No positions in sa2 yet. Import SERP HTML for keywords where this domain ranks.";
    }

    private static (string? Title, int H2Count)? ExtractMarkdownStructure(string text)
    {
        string? title = null;
        var h2 = 0;
        foreach (var line in text.Split('\n'))
        {
            var t = line.Trim();
            if (title is null && t.StartsWith("# ", StringComparison.Ordinal) && !t.StartsWith("## ", StringComparison.Ordinal))
                title = t[2..].Trim();
            if (t.StartsWith("## ", StringComparison.Ordinal) && !t.StartsWith("### ", StringComparison.Ordinal))
                h2++;
        }

        return title is null && h2 == 0 ? null : (title, h2);
    }

    private static string? ResolveTitle(PageExtractionResult extracted)
    {
        var title = extracted.MetaTags
            .FirstOrDefault(m => string.Equals(m.NameOrProperty, "title", StringComparison.OrdinalIgnoreCase))
            ?.Content;
        if (!string.IsNullOrWhiteSpace(title))
            return title;

        return ResolveMeta(extracted, "og:title", "twitter:title");
    }

    private static string? ResolveMeta(PageExtractionResult extracted, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = extracted.MetaTags
                .FirstOrDefault(m => string.Equals(m.NameOrProperty, key, StringComparison.OrdinalIgnoreCase))
                ?.Content;
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private static bool LooksLikeNonHtmlShell(string html)
    {
        var trimmed = html.TrimStart();
        if (trimmed.StartsWith("#", StringComparison.Ordinal) &&
            !trimmed.Contains("<html", StringComparison.OrdinalIgnoreCase))
            return true;

        return !trimmed.Contains("<html", StringComparison.OrdinalIgnoreCase)
            && !trimmed.Contains("<head", StringComparison.OrdinalIgnoreCase)
            && !trimmed.Contains("<body", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record FetchedPage(
        int HttpStatus,
        string? Title,
        string? MetaDescription,
        int? H2Count,
        IReadOnlyList<string> SchemaTypes,
        bool Fetched);
}

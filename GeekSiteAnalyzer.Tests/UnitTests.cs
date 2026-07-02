using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Infrastructure.Seeding;
using SiteAnalyzer2.Repositories;
using SiteAnalyzer2.Services.Filtering;
using SiteAnalyzer2.Services.Integrations;
using SiteAnalyzer2.Services.Parsing;
using SiteAnalyzer2.Services.Pipeline;
using SiteAnalyzer2.Services.Rankings;
using SiteAnalyzer2.Services.SiteAudit;
using SiteAnalyzer2.Services.Utilities;
using SiteAnalyzer2.Serp;
using SiteAnalyzer2.Serp.Models;
using System.Text.Json;

namespace SiteAnalyzer2.Tests;

internal static class TestFixtures
{
    public static string Path(params string[] parts)
        => System.IO.Path.Combine(AppContext.BaseDirectory, "fixtures", System.IO.Path.Combine(parts));

    public static string CanonicalSerpHtmlPath()
        => ResolveRepoPath("serp", SerpCanonicalFixture.HtmlFileName);

    public static string ReadCanonicalSerpHtml()
        => File.ReadAllText(CanonicalSerpHtmlPath());

    public static string ResolveRepoPath(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "tests", "fixtures", System.IO.Path.Combine(parts));
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return Path(parts);
    }
}

public class RelevanceFilterServiceTests
{
    private static AnalysisRun CreateRun() => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        Keyword = "best crm software",
        TargetSiteUrl = "https://example.com",
        IncludeReferenceDomains = false
    };

    [Fact]
    public async Task FilterFixtureMatrix_CoversAllBuckets()
    {
        var run = CreateRun();
        var filter = new RelevanceFilterService(null!);
        var serpItems = LoadFixtureSerpItems(run);

        var owned = new List<ProjectOwnedDomain>
        {
            new() { ProjectId = run.ProjectId, Domain = "client-secondary.example.com" }
        };

        await filter.ApplyFilterAsync(
            run,
            serpItems,
            SeedData.ReferenceExcludeDomains.Select(d => new ReferenceExcludeDomain { Domain = d }).ToList(),
            SeedData.KnownPlatformDomains.Select(d => new KnownPlatformDomain { Domain = d }).ToList(),
            owned,
            []);

        Assert.Contains(serpItems, c => (c.Url ?? "").Contains("wikipedia") && c.FilterStatus == FilterStatus.Excluded);
        Assert.Contains(serpItems, c => (c.Domain ?? "").Contains("reddit") && c.IncludeReason == IncludeReason.KnownPlatform);
        Assert.Contains(serpItems, c => (c.Domain ?? "").Contains("quora") && c.IncludeReason == IncludeReason.KnownPlatform);
        Assert.Contains(serpItems, c => (c.Url ?? "").Contains("pipedrive.com/features") && c.IncludeReason == IncludeReason.MultiPropertyCascade);
        Assert.Contains(serpItems, c => c.Domain == "support.competitor.com" && c.FilterStatus == FilterStatus.Rejected);
        Assert.Contains(serpItems, c => c.Domain == "client-secondary.example.com" && c.FilterStatus == FilterStatus.Excluded);
        Assert.True(serpItems.Count(c => c.FilterStatus == FilterStatus.Included) >= 3);
    }

    private static List<SerpItem> LoadFixtureSerpItems(AnalysisRun run)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestFixtures.Path("serp", "default-serp.json")));
        return doc.RootElement.GetProperty("results").EnumerateArray().Select(item => new SerpItem
        {
            Id = Guid.NewGuid(),
            ProjectId = run.ProjectId,
            RunId = run.Id,
            Type = SerpItemTypes.Organic,
            RankGroup = item.GetProperty("position").GetInt32(),
            Url = item.GetProperty("url").GetString()!,
            Title = item.GetProperty("title").GetString()!,
            Description = item.GetProperty("snippet").GetString()!,
            Domain = item.GetProperty("domain").GetString()!
        }).ToList();
    }
}

public class PageExtractionServiceTests
{
    [Fact]
    public void Extract_CapturesH1ThroughH6AndJsonLd()
    {
        var html = File.ReadAllText(TestFixtures.Path("html", "sample-page.html"));
        var service = new PageExtractionService(null!);
        var result = service.Extract(html, new Uri("https://example.com/crm"), "example.com");

        Assert.NotEmpty(result.Headings.Where(h => h.Level == 1));
        Assert.True(result.Headings.Count(h => h.Level is >= 2 and <= 6) >= 5);
        Assert.NotEmpty(result.JsonLdBlocks);
        Assert.Equal("https://example.com/crm", result.CanonicalUrl);
        Assert.NotEmpty(result.InternalLinks);
    }

    [Fact]
    public void Extract_DoesNotThrowOnArrayRootJsonLd()
    {
        var html = """
            <html><head>
            <script type="application/ld+json">[{"@type":"Organization","name":"Geek"}]</script>
            </head><body><a href="/about">About</a></body></html>
            """;
        var service = new PageExtractionService(null!);
        var result = service.Extract(html, new Uri("https://www.geekatyourspot.com/"), "geekatyourspot.com");
        Assert.Single(result.JsonLdBlocks);
        Assert.Equal("Organization", result.JsonLdBlocks[0].ParsedType);
        Assert.Single(result.InternalLinks);
    }

    [Fact]
    public void Extract_PreservesSpacesAcrossBrInHeadings()
    {
        var html = """
            <html><body>
            <h2>Clone Yourself<br /><span>Work 24/7</span></h2>
            <h2><a href="/use-cases">Artificial<br />Intelligence <br />Use<br /><span>Cases</span></a></h2>
            <h2>The<br /><span>Methodology</span></h2>
            </body></html>
            """;
        var service = new PageExtractionService(null!);
        var result = service.Extract(html, new Uri("https://www.geekatyourspot.com/"), "geekatyourspot.com");

        Assert.Contains(result.Headings, h => h.Text == "Clone Yourself Work 24/7");
        Assert.Contains(result.Headings, h => h.Text == "Artificial Intelligence Use Cases");
        Assert.Contains(result.Headings, h => h.Text == "The Methodology");
    }
}

public class SerpLiveAdvancedSerializerTests
{
    [Fact]
    public void Build_IncludesOrganicItems()
    {
        var run = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            Keyword = "weather forecast",
            SerpLocationCode = 2840,
            SerpLanguageCode = "en",
            SerpDevice = "desktop",
            SerpOs = "windows",
            SerpDepth = 1,
            SerpSeDomain = "google.com",
            SerpCheckUrl = "https://www.google.com/search?q=weather+forecast",
            SerpCapturedAt = DateTime.UtcNow,
            SerpPagesCount = 1,
            SerpItemsCount = 1,
            SerpItemTypesJson = "[\"organic\"]"
        };

        var items = new List<SerpItem>
        {
            new()
            {
                Type = SerpItemTypes.Organic,
                RankGroup = 1,
                RankAbsolute = 1,
                Page = 1,
                Domain = "weather.com",
                Title = "Weather",
                Url = "https://weather.com/",
                Description = "Forecast text",
                Breadcrumb = "https://weather.com",
                WebsiteName = "The Weather Channel"
            }
        };

        var json = JsonSerializer.Serialize(SerpLiveAdvancedSerializer.Build(run, items));
        Assert.Contains("weather forecast", json, StringComparison.Ordinal);
        Assert.Contains("https://weather.com/", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"organic\"", json, StringComparison.Ordinal);
        Assert.Contains("The Weather Channel", json, StringComparison.Ordinal);
    }
}

public class SerpRunReportSerializerTests
{
    [Fact]
    public void Build_IncludesFilteredOutOrganicAndFeatureRows()
    {
        var run = new AnalysisRun
        {
            Keyword = "crm software",
            SerpPagesCount = 1,
            SerpItemsCount = 3,
            SerpItemTypesJson = "[\"ai_overview\",\"paid\",\"organic\"]"
        };

        var items = new List<SerpItem>
        {
            new()
            {
                Type = SerpItemTypes.AiOverview,
                RankAbsolute = 1,
                RankGroup = 1,
                Page = 1,
                AiOverviewAvailable = false,
                AiOverviewStatusMessage = "An AI Overview is not available for this search"
            },
            new()
            {
                Type = SerpItemTypes.Paid,
                RankAbsolute = 2,
                RankGroup = 1,
                Page = 1,
                Ads = true,
                Url = "https://ads.example.com",
                Title = "Ad",
                Domain = "ads.example.com"
            },
            new()
            {
                Type = SerpItemTypes.Organic,
                RankAbsolute = 3,
                RankGroup = 1,
                Page = 1,
                Title = "Wikipedia",
                Description = "Overview",
                Url = "https://en.wikipedia.org/wiki/CRM",
                Domain = "wikipedia.org",
                FilterStatus = FilterStatus.Excluded,
                Filtered = true,
                ExcludeReason = "Reference domain excluded from crawl."
            }
        };

        var json = JsonSerializer.Serialize(SerpLiveAdvancedSerializer.Build(run, items));

        Assert.Contains("\"filtered\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"ai_overview\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"paid\"", json, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"organic\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_RelatedSearchesUsesUnifiedQueryList()
    {
        var run = new AnalysisRun { Keyword = "ai analytics", SerpPagesCount = 1, SerpItemsCount = 1 };
        var items = new List<SerpItem>
        {
            new()
            {
                Type = SerpItemTypes.RelatedSearches,
                RankAbsolute = 5,
                RankGroup = 1,
                Page = 1,
                RelatedQueries =
                [
                    new SerpRelatedQuery { Sequence = 1, QueryText = "ai market research tool", QueryType = SerpRelatedQueryType.RelatedSearch },
                    new SerpRelatedQuery { Sequence = 2, QueryText = "best ai for market analysis", QueryType = SerpRelatedQueryType.RelatedSearch }
                ]
            }
        };

        var json = JsonSerializer.Serialize(SerpLiveAdvancedSerializer.Build(run, items));
        Assert.Contains("\"type\":\"related_searches\"", json, StringComparison.Ordinal);
        Assert.Contains("ai market research tool", json, StringComparison.Ordinal);
    }
}

public class FilterInspectionViewTests
{
    [Fact]
    public void Build_OrdersBySerpPositionAndSummarizes()
    {
        var serp = new List<SerpItem>
        {
            new()
            {
                RankGroup = 2,
                Title = "Example",
                Description = "Snippet text",
                Url = "https://example.com",
                Domain = "example.com",
                FilterStatus = FilterStatus.PendingReview
            }
        };

        var json = JsonSerializer.Serialize(FilterInspectionView.Build(serp));
        Assert.Contains("\"filtered_out\"", json, StringComparison.Ordinal);
        Assert.Contains("For crawl", json, StringComparison.Ordinal);
        Assert.Contains("PendingReview", json, StringComparison.Ordinal);
        Assert.Contains("Ambiguous relevance", json, StringComparison.Ordinal);
        Assert.Contains("\"rejected\"", json, StringComparison.Ordinal);
    }
}

public class SerpFilterCountsTests
{
    [Fact]
    public void FromRunItems_CountsRejectedAndCrawlSeeds()
    {
        var runId = Guid.NewGuid();
        var items = new List<SerpItem>
        {
            new()
            {
                RunId = runId,
                Type = SerpItemTypes.Organic,
                Url = "https://ramp.com/blog/what-is-gaap",
                Title = "What Is GAAP?",
                Description = "Accounting",
                FilterStatus = FilterStatus.Rejected
            },
            new()
            {
                RunId = runId,
                Type = SerpItemTypes.Organic,
                Url = "https://ramp.com/expense-management",
                Title = "Expense management",
                Description = "Software",
                FilterStatus = FilterStatus.Included
            },
            new()
            {
                RunId = runId,
                Type = SerpItemTypes.Organic,
                Url = "https://en.wikipedia.org/wiki/Expense",
                FilterStatus = FilterStatus.Excluded
            }
        };

        var summary = SerpFilterCounts.FromRunItems(items, "https://www.example.com/", "AI Expense Management");

        Assert.True(summary.FilterApplied);
        Assert.Equal(1, summary.Included);
        Assert.Equal(1, summary.Rejected);
        Assert.Equal(1, summary.Excluded);
        Assert.Equal(1, summary.CrawlEligible);
        Assert.Equal(1, summary.CompetitorCrawlSeedCount);
    }

    [Fact]
    public void SelectEligible_IncludesPendingReview_AfterFilter()
    {
        var items = new List<SerpItem>
        {
            new()
            {
                Type = SerpItemTypes.Organic,
                Url = "https://vendor.com/expense-management",
                FilterStatus = FilterStatus.PendingReview
            },
            new()
            {
                Type = SerpItemTypes.Organic,
                Url = "https://vendor.com/blog/gaap",
                FilterStatus = FilterStatus.Rejected
            }
        };

        var eligible = SerpCrawlEligibility.SelectEligible(items, "AI Expense Management", filterApplied: true);

        Assert.Single(eligible);
        Assert.Contains("expense-management", eligible[0].Url, StringComparison.OrdinalIgnoreCase);
    }
}

public class KeywordPathMatcherTests
{
    [Fact]
    public void ContainsAnyKeywordToken_MatchesTitleSnippetOrUrl()
    {
        Assert.False(KeywordPathMatcher.ContainsAnyKeywordToken(
            "AI Expense Management",
            "https://ramp.com/blog/what-is-gaap",
            "What Is GAAP?",
            "Accounting principles"));

        Assert.True(KeywordPathMatcher.ContainsAnyKeywordToken(
            "AI Expense Management",
            "https://ramp.com/expense-management",
            "Expense software",
            ""));

        Assert.False(KeywordPathMatcher.ContainsAnyKeywordToken(
            "AI Expense Management",
            "https://ramp.com/blog/what-is-gaap",
            "What Is GAAP?",
            "Accounting principles and compliance"));
    }

    [Fact]
    public async Task ApplyFilter_RejectsCompetitorWithoutKeywordOverlap()
    {
        var run = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Keyword = "AI Expense Management Invoicing",
            TargetSiteUrl = "https://example.com",
            IncludeReferenceDomains = false
        };

        var serpItems = new List<SerpItem>
        {
            new()
            {
                RunId = run.Id,
                Type = SerpItemTypes.Organic,
                RankAbsolute = 10,
                RankGroup = 10,
                Url = "https://ramp.com/blog/what-is-gaap",
                Title = "What Is GAAP? Accounting Principles",
                Description = "Learn accounting standards.",
                Domain = "ramp.com"
            },
            new()
            {
                RunId = run.Id,
                Type = SerpItemTypes.Organic,
                RankAbsolute = 8,
                RankGroup = 8,
                Url = "https://ramp.com/expense-management",
                Title = "Expense management software",
                Description = "Automate expense reporting.",
                Domain = "ramp.com"
            }
        };

        var filter = new RelevanceFilterService(null!);
        await filter.ApplyFilterAsync(run, serpItems, [], [], [], []);

        var gaap = serpItems.Single(i => i.Url!.Contains("gaap"));
        var expense = serpItems.Single(i => i.Url!.Contains("expense-management"));

        Assert.Equal(FilterStatus.Rejected, gaap.FilterStatus);
        Assert.NotEqual(FilterStatus.Rejected, expense.FilterStatus);
        Assert.True(KeywordPathMatcher.ContainsAnyKeywordToken(
            run.Keyword, expense.Url, expense.Title, expense.Description));
    }
}

public class AiMarketExampleSnapshotTests
{
    [Fact]
    public void AiMarketExample_SerpAndFilterSnapshot()
    {
        var html = TestFixtures.ReadCanonicalSerpHtml();
        var parsed = GoogleSerpHtmlParser.ParseLivePage(html);

        var run = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Keyword = parsed.Keyword,
            TargetSiteUrl = "https://example.com",
            IncludeReferenceDomains = false,
            SerpMaxPage = parsed.PagesCount,
            SerpPagesCount = parsed.PagesCount
        };

        var serpItems = parsed.Items
            .Where(i => i.Type == SerpItemTypes.Organic)
            .Select(i => new SerpItem
            {
                Id = Guid.NewGuid(),
                ProjectId = run.ProjectId,
                RunId = run.Id,
                Type = i.Type,
                RankGroup = i.RankGroup,
                RankAbsolute = i.RankAbsolute,
                Page = i.Page,
                Url = i.Url,
                Title = i.Title,
                Description = i.Description,
                Domain = i.Domain,
                Breadcrumb = i.Breadcrumb,
                WebsiteName = i.WebsiteName
            })
            .ToList();

        var filter = new RelevanceFilterService(null!);
        filter.ApplyFilterAsync(
            run,
            serpItems,
            SeedData.ReferenceExcludeDomains.Select(d => new ReferenceExcludeDomain { Domain = d }).ToList(),
            SeedData.KnownPlatformDomains.Select(d => new KnownPlatformDomain { Domain = d }).ToList(),
            [],
            []).GetAwaiter().GetResult();

        var organicCount = serpItems.Count;
        var relatedCount = parsed.Items
            .SelectMany(i => i.RelatedQueries ?? [])
            .Count();
        var serpGatePass = parsed.Items.Count >= SerpGateConfiguration.ResolveMinItems();

        var referenceDomains = SeedData.ReferenceExcludeDomains;
        var hasReferenceInSerp = serpItems.Any(r =>
            referenceDomains.Any(d => (r.Domain ?? "").Contains(d, StringComparison.OrdinalIgnoreCase)));
        var referenceExcluded = serpItems.Count(c =>
            c.FilterStatus == FilterStatus.Excluded
            && c.ExcludeReason != null
            && c.ExcludeReason.Contains("reference", StringComparison.OrdinalIgnoreCase));
        var filterGatePass = !hasReferenceInSerp || referenceExcluded >= 1;

        var included = serpItems.Count(c => c.FilterStatus == FilterStatus.Included);
        var excluded = serpItems.Count(c => c.FilterStatus == FilterStatus.Excluded);
        var pending = serpItems.Count(c => c.FilterStatus == FilterStatus.PendingReview);
        var rejected = serpItems.Count(c => c.FilterStatus == FilterStatus.Rejected);

        var filterView = FilterInspectionView.Build(serpItems);
        run.SerpItemsCount = parsed.Items.Count;
        run.SerpItemTypesJson = JsonSerializer.Serialize(parsed.ItemTypes);
        var reportJson = JsonSerializer.Serialize(SerpLiveAdvancedSerializer.Build(run, parsed.Items.Select(ToEntity).ToList()));

        var snapshot = new
        {
            keyword = run.Keyword,
            serpPage = parsed.PagesCount,
            serpGate = new { passed = serpGatePass, organicCount, relatedCount },
            filterGate = new { passed = filterGatePass, included, excluded, rejected, pending },
            serpReport = reportJson,
            filter = filterView
        };

        _ = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        Assert.True(organicCount >= 1);
        Assert.True(serpGatePass);
        Assert.Equal(organicCount, included + excluded + pending + rejected);
        Assert.Contains("\"type\":\"organic\"", reportJson, StringComparison.Ordinal);
    }

    private static SerpItem ToEntity(SerpParsedItem item) =>
        new()
        {
            Type = item.Type,
            RankGroup = item.RankGroup,
            RankAbsolute = item.RankAbsolute,
            Page = item.Page,
            Domain = item.Domain,
            Title = item.Title,
            Url = item.Url,
            Breadcrumb = item.Breadcrumb,
            WebsiteName = item.WebsiteName,
            Description = item.Description,
            Ads = item.Ads,
            AiOverviewAvailable = item.AiOverviewAvailable,
            AiOverviewMarkdown = item.AiOverviewMarkdown,
            AiOverviewStatusMessage = item.AiOverviewStatusMessage,
            RelatedQueries = item.RelatedQueries?.Select(q => new SerpRelatedQuery
            {
                Sequence = q.Sequence,
                QueryText = q.QueryText,
                QueryType = q.QueryType
            }).ToList() ?? []
        };
}

public class SerpCanonicalFixtureTests
{
    [Fact]
    public void PersistedView_MirrorsCanonicalFixtureCounts()
    {
        var parsed = GoogleSerpHtmlParser.ParseLivePage(TestFixtures.ReadCanonicalSerpHtml());
        var run = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            Keyword = parsed.Keyword,
            SerpItemsCount = parsed.Items.Count,
            SerpSeResultsCount = parsed.SeResultsCount,
            SerpPagesCount = parsed.PagesCount
        };
        var entities = parsed.Items.Select(i => new SerpItem
        {
            Id = Guid.NewGuid(),
            RunId = run.Id,
            Type = i.Type,
            RankGroup = i.RankGroup,
            RankAbsolute = i.RankAbsolute,
            Domain = i.Domain,
            Title = i.Title,
            Url = i.Url,
            Ads = i.Ads,
            RelatedQueries = i.RelatedQueries?.Select(q => new SerpRelatedQuery
            {
                Id = Guid.NewGuid(),
                Sequence = q.Sequence,
                QueryText = q.QueryText,
                QueryType = q.QueryType
            }).ToList() ?? []
        }).ToList();

        var json = JsonSerializer.Serialize(SerpPersistedView.Build(run, entities));
        using var doc = JsonDocument.Parse(json);
        var counts = doc.RootElement.GetProperty("table_counts");
        Assert.Equal(SerpCanonicalFixture.Expected.TotalItems, counts.GetProperty("serp_items").GetInt32());
        Assert.Equal(SerpCanonicalFixture.Expected.RelatedQueries, counts.GetProperty("serp_related_queries").GetInt32());
    }

    [Fact]
    public void CanonicalHtml_ParsesExpectedItemCounts()
    {
        var parsed = GoogleSerpHtmlParser.ParseLivePage(TestFixtures.ReadCanonicalSerpHtml());

        Assert.Equal(SerpCanonicalFixture.Expected.Page, parsed.PagesCount);
        Assert.Equal(SerpCanonicalFixture.Expected.Organic,
            parsed.Items.Count(i => i.Type == SerpItemTypes.Organic));
        Assert.Equal(SerpCanonicalFixture.Expected.Paid,
            parsed.Items.Count(i => i.Type == SerpItemTypes.Paid));
        Assert.Equal(SerpCanonicalFixture.Expected.AiOverview,
            parsed.Items.Count(i => i.Type == SerpItemTypes.AiOverview));
        Assert.Equal(SerpCanonicalFixture.Expected.RelatedSearchesBlocks,
            parsed.Items.Count(i => i.Type == SerpItemTypes.RelatedSearches));
        Assert.Equal(SerpCanonicalFixture.Expected.TotalItems, parsed.Items.Count);

        var firstOrganic = parsed.Items.First(i => i.Type == SerpItemTypes.Organic);
        Assert.Equal(SerpCanonicalFixture.Expected.FirstOrganicDomain, firstOrganic.Domain);
        Assert.Contains(SerpCanonicalFixture.Keyword, parsed.Keyword, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(SerpCanonicalFixture.Expected.SeResultsCount, parsed.SeResultsCount);

        var related = parsed.Items.FirstOrDefault(i => i.Type == SerpItemTypes.RelatedSearches);
        Assert.NotNull(related);
        Assert.Equal(SerpCanonicalFixture.Expected.RelatedQueries, related!.RelatedQueries?.Count ?? 0);
        Assert.All(related.RelatedQueries!, q =>
            Assert.Equal(SerpRelatedQueryType.PeopleAlsoSearchFor, q.QueryType));

        var improvado = parsed.Items.First(i =>
            i.Type == SerpItemTypes.Organic
            && string.Equals(i.Domain, SerpCanonicalFixture.Expected.SecondOrganicDomain, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(SerpCanonicalFixture.Expected.SecondOrganicPreSnippet, improvado.PreSnippet);
        Assert.Equal(SerpCanonicalFixture.Expected.SecondOrganicWebsiteName, improvado.WebsiteName);

        var quantilope = parsed.Items.First(i =>
            i.Type == SerpItemTypes.Organic
            && string.Equals(i.Domain, SerpCanonicalFixture.Expected.ThirdOrganicDomain, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            SerpCanonicalFixture.Expected.ThirdOrganicDescriptionFragment,
            quantilope.Description ?? "",
            StringComparison.OrdinalIgnoreCase);
    }
}

public class GoogleSerpHtmlParserTests
{
    [Fact]
    public void ParseLivePage_ExtractsDataForSeoShapedOrganicFields()
    {
        var parsed = GoogleSerpHtmlParser.ParseLivePage(TestFixtures.ReadCanonicalSerpHtml());

        var organic = parsed.Items.Where(i => i.Type == SerpItemTypes.Organic).ToList();
        Assert.NotEmpty(organic);

        var first = organic[0];
        Assert.False(string.IsNullOrWhiteSpace(first.Title));
        Assert.False(string.IsNullOrWhiteSpace(first.Url));
        Assert.False(string.IsNullOrWhiteSpace(first.Domain));
        Assert.False(string.IsNullOrWhiteSpace(first.Description));
        Assert.Contains(parsed.ItemTypes, t => t == SerpItemTypes.Organic);
        Assert.True(parsed.Items.Any(i => i.Type == SerpItemTypes.RelatedSearches || i.RelatedQueries?.Count > 0));
    }

    [Fact]
    public void ParseLivePage_ModernPasfMarkup_ExtractsRelatedSuggestions()
    {
        var html = File.ReadAllText(FindFixturePath("how you implement AI Content Marketing - Google Search.html"));
        var parsed = GoogleSerpHtmlParser.ParseLivePage(html);

        var related = parsed.Items.FirstOrDefault(i => i.Type == SerpItemTypes.RelatedSearches);
        Assert.NotNull(related);
        Assert.True(related!.RelatedQueries!.Count > 0, "Expected PASF suggestions from saved SERP HTML.");
        Assert.Contains(
            related.RelatedQueries!,
            q => q.QueryText.Contains("content creation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseLivePage_AiOverview_ExtractsFrameworkDespiteUnavailableSnippet()
    {
        var html = File.ReadAllText(FindFixturePath("how you implement AI Content Marketing - Google Search.html"));
        var parsed = GoogleSerpHtmlParser.ParseLivePage(html);

        var ai = parsed.Items.FirstOrDefault(i => i.Type == SerpItemTypes.AiOverview);
        Assert.NotNull(ai);
        Assert.True(ai!.AiOverviewAvailable);
        Assert.Contains("Step-by-Step", ai.AiOverviewMarkdown ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Implementation Framework", ai.AiOverviewMarkdown ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseLivePage_ModernPasfMarkup_CountsResultTypes()
    {
        var html = File.ReadAllText(FindFixturePath("how you implement AI Content Marketing - Google Search.html"));
        var parsed = GoogleSerpHtmlParser.ParseLivePage(html);

        Assert.Equal(9, parsed.Items.Count(i => i.Type == SerpItemTypes.Organic));
        Assert.Equal(5, parsed.Items.Count(i => i.Type == SerpItemTypes.Paid));
        Assert.Single(parsed.Items, i => i.Type == SerpItemTypes.AiOverview);
        Assert.Single(parsed.Items, i => i.Type == SerpItemTypes.RelatedSearches);
    }

    [Fact]
    public void ParseLivePage_GoogleSample_ExtractsOrganic()
    {
        var html = File.ReadAllText(FindFixturePath("google-sample.html"));
        var parsed = GoogleSerpHtmlParser.ParseLivePage(html);
        var organic = parsed.Items.Where(i => i.Type == SerpItemTypes.Organic).ToList();
        Assert.Equal(2, organic.Count);
        Assert.Contains(organic, r => (r.Url ?? "").Contains("hubspot.com", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DetectPageNumber_PageOneSavedHtml()
    {
        Assert.Equal(1, GoogleSerpHtmlParser.DetectPageNumber(TestFixtures.ReadCanonicalSerpHtml()));
    }

    [Fact]
    public void LooksLikeJavaScriptRequired_DetectsEnableJsShell()
    {
        const string html = "<meta content=\"0;url=/httpservice/retry/enablejs?sei=abc\" http-equiv=\"refresh\">";
        Assert.True(GoogleSerpHtmlParser.LooksLikeJavaScriptRequired(html));
    }

    private static string ResolveFixtureHtml(string fileName) => FindFixturePath(fileName);

    private static string FindFixturePath(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "fixtures", "serp", fileName);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        return TestFixtures.Path("serp", fileName);
    }
}

public class PaaTextImportParserTests
{
  [Fact]
  public void LooksLikePaaTextList_true_for_question_lines()
  {
    const string text = """
        What is an AI customer journey?
        How do SMBs map journeys?
        """;

    Assert.True(PaaTextImportParser.LooksLikePaaTextList(text));
  }

  [Fact]
  public void LooksLikePaaTextList_false_for_serp_html()
  {
    Assert.False(PaaTextImportParser.LooksLikePaaTextList(TestFixtures.ReadCanonicalSerpHtml()));
  }

  [Fact]
  public void Parse_extracts_questions_and_sets_type()
  {
    const string text = """
        # curated PAA
        What is an AI customer journey?
        How do SMBs map journeys?
        short
        """;

    var parsed = PaaTextImportParser.Parse(text, "ai customer journey");

    Assert.Equal("ai customer journey", parsed.Keyword);
    Assert.Single(parsed.Items);
    Assert.Equal(SerpItemTypes.PeopleAlsoAsk, parsed.Items[0].Type);
    Assert.Equal(2, parsed.Items[0].RelatedQueries!.Count);
    Assert.Contains(
        parsed.Items[0].RelatedQueries!,
        q => q.QueryText.Contains("AI customer journey", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void Parse_throws_when_no_questions()
  {
    Assert.Throws<InvalidOperationException>(() => PaaTextImportParser.Parse("# only comments\nshort", "kw"));
  }
}

public class PaaLaneImportComposerTests
{
  [Fact]
  public void MergeContents_dedupes_questions_across_txt_files()
  {
    var merged = PaaLaneImportComposer.MergeContents(
      [
        new PaaLaneImportFile("a.txt", "What is an AI customer journey?\n"),
        new PaaLaneImportFile("b.txt", "How do SMBs map journeys?\nWhat is an AI customer journey?"),
      ],
      "ai customer journey");

    Assert.Equal(2, PaaLaneImportComposer.ExtractQuestions(merged).Count);
  }

  [Fact]
  public void MergeContents_throws_when_all_files_empty()
  {
    Assert.Throws<InvalidOperationException>(() =>
      PaaLaneImportComposer.MergeContents(
        [new PaaLaneImportFile("a.txt", "   \n# comments only")],
        "kw"));
  }

  [Fact]
  public void MergeContents_filters_off_topic_questions()
  {
    var merged = PaaLaneImportComposer.MergeContents(
      [
        new PaaLaneImportFile("a.txt", "What is an AI customer journey?\n"),
        new PaaLaneImportFile("b.txt", "What is GAAP accounting?\nHow do SMBs map journeys?"),
      ],
      "ai customer journey");

    var questions = PaaLaneImportComposer.ExtractQuestions(merged);
    Assert.Equal(2, questions.Count);
    Assert.Contains(questions, q => q.Contains("customer journey", StringComparison.OrdinalIgnoreCase));
    Assert.DoesNotContain(questions, q => q.Contains("GAAP", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void MergeContents_throws_when_all_questions_off_topic()
  {
    Assert.Throws<InvalidOperationException>(() =>
      PaaLaneImportComposer.MergeContents(
        [new PaaLaneImportFile("a.txt", "What is GAAP?\nHow do taxes work?")],
        "ai customer journey"));
  }
}

public class PaaQuestionRelevanceFilterTests
{
  [Theory]
  [InlineData("ai customer journey", "What is an AI customer journey map?", true)]
  [InlineData("ai customer journey", "How do SMBs map customer journeys?", true)]
  [InlineData("ai customer journey", "What is GAAP accounting?", false)]
  [InlineData("ai customer journey", "Where can I find an AI customer journey PDF?", false)]
  public void IsRelevantToKeyword_matches_topic_and_blocks_off_intent(string keyword, string question, bool expected)
  {
    Assert.Equal(expected, PaaQuestionRelevanceFilter.IsRelevantToKeyword(keyword, question));
  }
}

public class SerpGateConfigurationTests
{
    [Fact]
    public void ResolveMinItems_DefaultsToOne()
    {
        Environment.SetEnvironmentVariable("SERP_GATE_MIN_ITEMS", null);
        Environment.SetEnvironmentVariable("SERP_GATE_MIN_ORGANIC", null);
        Assert.Equal(1, SerpGateConfiguration.ResolveMinItems());
    }

    [Fact]
    public void ResolveMinItems_ReadsEnv()
    {
        Environment.SetEnvironmentVariable("SERP_GATE_MIN_ITEMS", "3");
        try
        {
            Assert.Equal(3, SerpGateConfiguration.ResolveMinItems());
        }
        finally
        {
            Environment.SetEnvironmentVariable("SERP_GATE_MIN_ITEMS", null);
        }
    }

    [Fact]
    public void ResolveMinItems_FallsBackToLegacyOrganicEnv()
    {
        Environment.SetEnvironmentVariable("SERP_GATE_MIN_ITEMS", null);
        Environment.SetEnvironmentVariable("SERP_GATE_MIN_ORGANIC", "2");
        try
        {
            Assert.Equal(2, SerpGateConfiguration.ResolveMinItems());
        }
        finally
        {
            Environment.SetEnvironmentVariable("SERP_GATE_MIN_ORGANIC", null);
        }
    }
}

public class SerpFixtureFileCleanupTests
{
    [Fact]
    public void CleanUnderContentRoot_RemovesHtmlAndFilesFolders()
    {
        var root = Path.Combine(Path.GetTempPath(), "sa-cleanup-" + Guid.NewGuid());
        var serp = Path.Combine(root, "fixtures", "serp");
        Directory.CreateDirectory(serp);
        var filesDir = Path.Combine(serp, "keyword - Google Search_files");
        Directory.CreateDirectory(filesDir);
        File.WriteAllText(Path.Combine(filesDir, "junk"), "x");
        File.WriteAllText(Path.Combine(serp, "save.html"), "<html></html>");
        File.WriteAllText(Path.Combine(serp, "default-serp.json"), "{}");

        var removed = SiteAnalyzer2.Services.Pipeline.SerpFixtureFileCleanup.CleanUnderContentRoot(root);

        Assert.Equal(2, removed);
        Assert.False(Directory.Exists(filesDir));
        Assert.False(File.Exists(Path.Combine(serp, "save.html")));
        Assert.True(File.Exists(Path.Combine(serp, "default-serp.json")));

        Directory.Delete(root, recursive: true);
    }
}

public class FixtureSerpProviderTests
{
    [Fact]
    public async Task FetchOrganicResultsAsync_IsDisabled()
    {
        var provider = new SiteAnalyzer2.Serp.Providers.FixtureSerpProvider();
        await Assert.ThrowsAsync<NotSupportedException>(
            () => provider.FetchOrganicResultsAsync(new SiteAnalyzer2.Serp.Models.SerpQuery("best crm software")));
    }
}

public class SerpProviderResolverTests
{
    private static readonly Microsoft.Extensions.Logging.Abstractions.NullLogger<SiteAnalyzer2.Serp.SerpProviderResolver> ResolverLogger =
        Microsoft.Extensions.Logging.Abstractions.NullLogger<SiteAnalyzer2.Serp.SerpProviderResolver>.Instance;

    private static SiteAnalyzer2.Serp.Providers.GoogleScraperProvider CreateGoogleProvider() =>
        new(new HttpClient());

    [Fact]
    public void Resolve_UnknownKey_Throws()
    {
        var resolver = new SiteAnalyzer2.Serp.SerpProviderResolver(
            CreateGoogleProvider(),
            ResolverLogger,
            new SiteAnalyzer2.Serp.Providers.FixtureSerpProvider());

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve("serpapi"));
    }

    [Fact]
    public void Resolve_GoogleScraper_ReturnsProvider()
    {
        var google = CreateGoogleProvider();
        var resolver = new SiteAnalyzer2.Serp.SerpProviderResolver(
            google,
            ResolverLogger,
            new SiteAnalyzer2.Serp.Providers.FixtureSerpProvider());

        Assert.Same(google, resolver.Resolve("google-scraper"));
    }

    [Fact]
    public void Resolve_Fixture_RejectedWhenExternalExecution()
    {
        Environment.SetEnvironmentVariable("SERP_EXECUTION", "external");
        try
        {
            var resolver = new SiteAnalyzer2.Serp.SerpProviderResolver(
                CreateGoogleProvider(),
                ResolverLogger,
                new SiteAnalyzer2.Serp.Providers.FixtureSerpProvider());

            var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("fixture"));
            Assert.Contains("not allowed", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SERP_EXECUTION", null);
        }
    }

    [Fact]
    public void Resolve_Fixture_RejectedWhenFixtureNotRegistered()
    {
        var resolver = new SiteAnalyzer2.Serp.SerpProviderResolver(CreateGoogleProvider(), ResolverLogger, fixture: null);

        var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("fixture"));
        Assert.Contains("not allowed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}

public class CrawlPriorityMatcherTests
{
    [Fact]
    public void IsPriorityUrl_MatchesConfiguredPattern()
    {
        var url = new Uri("https://example.com/about-us/team");
        Assert.True(SiteAnalyzer2.Services.Crawling.CrawlPriorityMatcher.IsPriorityUrl(
            url,
            ["/about-us"],
            new HashSet<string>()));
    }

    [Fact]
    public void IsPriorityUrl_MatchesNavLinkSet()
    {
        var url = new Uri("https://example.com/custom-page");
        var navLinks = new HashSet<string> { "https://example.com/custom-page" };
        Assert.True(SiteAnalyzer2.Services.Crawling.CrawlPriorityMatcher.IsPriorityUrl(
            url,
            [],
            navLinks));
    }
}

public class BusinessFocusClassificationServiceTests
{
    [Fact]
    public void NormalizeTargetUrl_UsesAuthorityOnly()
    {
        var normalized = SiteAnalyzer2.Services.BusinessFocus.BusinessFocusClassificationService
            .NormalizeTargetUrl("https://Example.com/path/page");
        Assert.Equal("https://www.example.com/", normalized);
    }
}

public class BusinessFocusProviderConfigurationTests
{
    [Fact]
    public void ResolveEffectiveProvider_RequiresExplicitSettingWhenNoKeys()
    {
        Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", null);
        Environment.SetEnvironmentVariable("BUSINESS_FOCUS_AI_PROVIDER", null);
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SiteAnalyzer2.Services.BusinessFocus.BusinessFocusProviderConfiguration.ResolveEffectiveProvider());

        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveEffectiveProvider_InfersOpenAiWhenKeyConfigured()
    {
        Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", null);
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-openai");

        try
        {
            Assert.Equal(
                SiteAnalyzer2.Services.BusinessFocus.BusinessFocusProvider.OpenAi,
                SiteAnalyzer2.Services.BusinessFocus.BusinessFocusProviderConfiguration.ResolveEffectiveProvider());
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        }
    }

    [Fact]
    public void ResolveEffectiveProvider_RejectsAuto()
    {
        Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", "auto");

        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                SiteAnalyzer2.Services.BusinessFocus.BusinessFocusProviderConfiguration.ResolveEffectiveProvider());

            Assert.Contains("auto", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", null);
        }
    }

    [Fact]
    public void ResolveEffectiveProvider_ExplicitHuman()
    {
        Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", "human");

        try
        {
            Assert.Equal(
                SiteAnalyzer2.Services.BusinessFocus.BusinessFocusProvider.Human,
                SiteAnalyzer2.Services.BusinessFocus.BusinessFocusProviderConfiguration.ResolveEffectiveProvider());
        }
        finally
        {
            Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", null);
        }
    }

    [Fact]
    public void ResolveEffectiveProvider_ExplicitOpenAiRequiresOpenAiKey()
    {
        Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", "openai");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "test-anthropic");

        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                SiteAnalyzer2.Services.BusinessFocus.BusinessFocusProviderConfiguration.ResolveEffectiveProvider());
        }
        finally
        {
            Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", null);
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
        }
    }

    [Fact]
    public void ResolveEffectiveProvider_ExplicitAnthropicRequiresAnthropicKey()
    {
        Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", "anthropic");
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-openai");

        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                SiteAnalyzer2.Services.BusinessFocus.BusinessFocusProviderConfiguration.ResolveEffectiveProvider());
        }
        finally
        {
            Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", null);
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        }
    }
}

public class CompetitorCrawlServiceTests
{
    [Fact]
    public void SelectSeedsPerDomain_PicksLowestRankAbsolute_AndExcludesTarget()
    {
        var runId = Guid.NewGuid();
        var items = new List<SerpItem>
        {
            new()
            {
                RunId = runId,
                Type = SerpItemTypes.Organic,
                RankAbsolute = 3,
                Url = "https://www.alpha.com/page-b",
                Ads = false
            },
            new()
            {
                RunId = runId,
                Type = SerpItemTypes.Organic,
                RankAbsolute = 1,
                Url = "https://alpha.com/page-a",
                Ads = false
            },
            new()
            {
                RunId = runId,
                Type = SerpItemTypes.Organic,
                RankAbsolute = 2,
                Url = "https://beta.com/home",
                Ads = false
            },
            new()
            {
                RunId = runId,
                Type = SerpItemTypes.Organic,
                RankAbsolute = 4,
                Url = "https://www.example.com/about",
                Ads = false
            }
        };

        var seeds = SiteAnalyzer2.Services.CompetitorCrawl.CompetitorCrawlService
            .SelectSeedsPerDomain(items, "example.com");

        Assert.Equal(2, seeds.Count);
        Assert.Contains(seeds, s => s.Domain == "alpha.com" && s.RankAbsolute == 1 && s.Url.Contains("page-a"));
        Assert.Contains(seeds, s => s.Domain == "beta.com" && s.RankAbsolute == 2);
        Assert.DoesNotContain(seeds, s => s.Domain == "example.com");
    }

    [Fact]
    public void SelectSeedsPerDomain_PrefersPathRelevantUrl_OverHigherRank()
    {
        var runId = Guid.NewGuid();
        var keyword = "AI Expense Management Invoicing";
        var items = new List<SerpItem>
        {
            new()
            {
                RunId = runId,
                Type = SerpItemTypes.Organic,
                RankAbsolute = 10,
                Url = "https://ramp.com/blog/what-is-gaap",
                Ads = false
            },
            new()
            {
                RunId = runId,
                Type = SerpItemTypes.Organic,
                RankAbsolute = 15,
                Url = "https://ramp.com/expense-management",
                Ads = false
            }
        };

        var seeds = SiteAnalyzer2.Services.CompetitorCrawl.CompetitorCrawlService
            .SelectSeedsPerDomain(items, "example.com", keyword);

        var ramp = Assert.Single(seeds);
        Assert.Equal("ramp.com", ramp.Domain);
        Assert.Contains("expense-management", ramp.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(15, ramp.RankAbsolute);
    }
}

public class SiteProfileServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static void AddLinkedProfile(AppDbContext db, Guid projectId, string siteUrl)
    {
        db.SiteProfiles.Add(new SiteProfile
        {
            Id = Guid.NewGuid(),
            SiteUrl = siteUrl,
            GeekSeoProjectId = projectId,
        });
        db.Projects.Add(new Project { Id = projectId, Name = "Geek-SEO" });
    }

    [Fact]
    public async Task ResolveProjectId_NormalizesInputBeforeMatch()
    {
        await using var db = CreateDb();
        var projectId = Guid.NewGuid();
        AddLinkedProfile(db, projectId, "https://www.geekatyourspot.com/");
        await db.SaveChangesAsync();

        var service = new SiteAnalyzer2.Services.Integrations.SiteProfileService(db);
        var resolved = await service.ResolveProjectIdForImportAsync("HTTP://GEEKATYOURSPOT.com/");

        Assert.Equal(projectId, resolved);
    }

    [Fact]
    public async Task ResolveProjectId_StoredWithWww_InputWithoutWww()
    {
        await using var db = CreateDb();
        var projectId = Guid.NewGuid();
        AddLinkedProfile(db, projectId, "https://www.geekatyourspot.com/");
        await db.SaveChangesAsync();

        var service = new SiteAnalyzer2.Services.Integrations.SiteProfileService(db);
        var resolved = await service.ResolveProjectIdForImportAsync("geekatyourspot.com");

        Assert.Equal(projectId, resolved);
    }

    [Fact]
    public async Task ResolveProjectId_FromSiteProfile()
    {
        await using var db = CreateDb();
        var projectId = Guid.NewGuid();
        AddLinkedProfile(db, projectId, "https://www.geekatyourspot.com/");
        await db.SaveChangesAsync();

        var service = new SiteAnalyzer2.Services.Integrations.SiteProfileService(db);
        var resolved = await service.ResolveProjectIdForImportAsync("https://www.geekatyourspot.com");

        Assert.Equal(projectId, resolved);
    }

    [Fact]
    public async Task ResolveProjectId_ProfileWithoutGeekSeoLink_Throws()
    {
        await using var db = CreateDb();
        db.SiteProfiles.Add(new SiteProfile
        {
            Id = Guid.NewGuid(),
            SiteUrl = "https://www.geekatyourspot.com/",
        });
        await db.SaveChangesAsync();

        var service = new SiteAnalyzer2.Services.Integrations.SiteProfileService(db);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResolveProjectIdForImportAsync("geekatyourspot.com"));

        Assert.Contains("Geek-SEO project is not linked", ex.Message);
    }

    [Fact]
    public async Task ResolveProjectId_UnknownUrl_Throws()
    {
        await using var db = CreateDb();
        var service = new SiteAnalyzer2.Services.Integrations.SiteProfileService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResolveProjectIdForImportAsync("https://new-site.example"));

        Assert.Contains("Geek-SEO project is not linked", ex.Message);
    }

    [Fact]
    public async Task LinkGeekSeoProject_CreatesProfileAndProjectRow()
    {
        await using var db = CreateDb();
        var projectId = Guid.NewGuid();
        var service = new SiteAnalyzer2.Services.Integrations.SiteProfileService(db);

        await service.LinkGeekSeoProjectAsync(projectId, "geekatyourspot.com");

        var profile = await db.SiteProfiles.SingleAsync();
        Assert.Equal("https://www.geekatyourspot.com/", profile.SiteUrl);
        Assert.Equal(projectId, profile.GeekSeoProjectId);
        Assert.True(await db.Projects.AnyAsync(p => p.Id == projectId));
    }

    [Fact]
    public async Task CreateOrGetAsync_CreatesProfileWithHostnameDisplayName()
    {
        await using var db = CreateDb();
        var service = new SiteAnalyzer2.Services.Integrations.SiteProfileService(db);

        var result = await service.CreateOrGetAsync("geekatyourspot.com");

        Assert.True(result.Created);
        Assert.Equal("https://www.geekatyourspot.com/", result.SiteUrl);
        Assert.Equal("geekatyourspot.com", result.DisplayName);
        var profile = await db.SiteProfiles.SingleAsync();
        Assert.Equal(result.Id, profile.Id);
        Assert.Equal("geekatyourspot.com", profile.DisplayName);
    }

    [Fact]
    public async Task CreateOrGetAsync_ReturnsExistingProfileWithoutDuplicate()
    {
        await using var db = CreateDb();
        var existingId = Guid.NewGuid();
        db.SiteProfiles.Add(new SiteProfile
        {
            Id = existingId,
            SiteUrl = "https://www.geekatyourspot.com/",
            DisplayName = "Geek At Your Spot",
        });
        await db.SaveChangesAsync();

        var service = new SiteAnalyzer2.Services.Integrations.SiteProfileService(db);
        var result = await service.CreateOrGetAsync("https://www.geekatyourspot.com/");

        Assert.False(result.Created);
        Assert.Equal(existingId, result.Id);
        Assert.Equal("Geek At Your Spot", result.DisplayName);
        Assert.Single(db.SiteProfiles);
    }

    [Fact]
    public async Task GetDetailByUrlAsync_ReturnsMappedProfile()
    {
        await using var db = CreateDb();
        var profileId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-2);
        db.SiteProfiles.Add(new SiteProfile
        {
            Id = profileId,
            SiteUrl = "https://www.geekatyourspot.com/",
            DisplayName = "geekatyourspot.com",
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            BusinessType = "LocalBusiness",
            PrimaryNiche = "IT services",
            NicheTags = ["managed it", "cybersecurity"],
            CompetitorDomains = ["competitor.com"],
        });
        await db.SaveChangesAsync();

        var service = new SiteAnalyzer2.Services.Integrations.SiteProfileService(db);
        var detail = await service.GetDetailByUrlAsync("geekatyourspot.com");

        Assert.NotNull(detail);
        Assert.Equal(profileId, detail.Id);
        Assert.Equal("https://www.geekatyourspot.com/", detail.SiteUrl);
        Assert.Equal("LocalBusiness", detail.BusinessType);
        Assert.Equal(["managed it", "cybersecurity"], detail.NicheTags);
        Assert.Equal(["competitor.com"], detail.CompetitorDomains);
    }

    [Fact]
    public async Task CreateOrGetAsync_InvalidUrl_Throws()
    {
        await using var db = CreateDb();
        var service = new SiteAnalyzer2.Services.Integrations.SiteProfileService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrGetAsync("not a url"));
    }

    [Fact]
    public async Task ResolveProjectId_LinksBootstrapProject_WhenApiBootstrapConfigured()
    {
        var previous = Environment.GetEnvironmentVariable(
            SiteAnalyzer2.Services.Integrations.OperatorBootstrapConfiguration.EnvVarName);
        var projectId = Guid.NewGuid();
        Environment.SetEnvironmentVariable(
            SiteAnalyzer2.Services.Integrations.OperatorBootstrapConfiguration.EnvVarName,
            projectId.ToString());

        try
        {
            await using var db = CreateDb();
            db.SiteProfiles.Add(new SiteProfile
            {
                Id = Guid.NewGuid(),
                SiteUrl = "https://www.geekatyourspot.com/",
            });
            await db.SaveChangesAsync();

            var service = new SiteAnalyzer2.Services.Integrations.SiteProfileService(db);

            var resolved = await service.ResolveProjectIdForImportAsync("https://www.geekatyourspot.com/");

            Assert.Equal(projectId, resolved);
            var profile = await db.SiteProfiles.SingleAsync();
            Assert.Equal("https://www.geekatyourspot.com/", profile.SiteUrl);
            Assert.Equal(projectId, profile.GeekSeoProjectId);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                SiteAnalyzer2.Services.Integrations.OperatorBootstrapConfiguration.EnvVarName,
                previous);
        }
    }
}

public class SiteProfileAssemblerHelpersTests
{
    [Fact]
    public void BuildSiteProfileFromHomepage_UsesJsonLdAndHeadings()
    {
        var homepage = new TargetPageSnapshot
        {
            Page = new Page { Url = "https://www.geekatyourspot.com/" },
            Headings =
            [
                new PageHeading { Level = 1, Text = "Managed IT Services", Sequence = 0 },
                new PageHeading { Level = 3, Text = "Marketing", Sequence = 1 },
            ],
            MetaTags =
            [
                new PageMetaTag { NameOrProperty = "title", Content = "AI Consulting for Small Business" },
            ],
            JsonLdBlocks =
            [
                new PageJsonLd
                {
                    ParsedType = "LocalBusiness",
                    RawJson = """
                        {
                          "@type": ["LocalBusiness","ProfessionalService"],
                          "name": "Geek at Your Spot",
                          "description": "AI consulting firm helping small businesses implement AI and automation.",
                          "areaServed": [
                            {"@type":"County","name":"Broward County","containedInPlace":"Florida"}
                          ],
                          "address": {"addressLocality":"Delray Beach","addressRegion":"FL"}
                        }
                        """,
                },
            ],
        };

        var write = SiteAnalyzer2.Services.ProfileAssembly.SiteProfileAssemblerHelpers.BuildSiteProfileFromHomepage(
            homepage,
            "geekatyourspot.com");

        Assert.Contains("Local business", write.BusinessType!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Geek at Your Spot", write.PrimaryNiche!);
        Assert.Contains("AI consulting firm", write.BusinessSummary!);
        Assert.Contains("Broward County", write.ServiceAreaDescription!);
        Assert.Contains("Marketing", write.NicheTags);
        Assert.NotEmpty(write.WritingRecommendations);
        Assert.Contains(write.WritingRecommendations, r => r.Contains("Site profile panel", StringComparison.OrdinalIgnoreCase));
        SiteAnalyzer2.Services.ProfileAssembly.SiteProfileAssemblerHelpers.ValidateHomepageOutput(write);
    }

    [Fact]
    public void BuildHomepageWritingRecommendations_FlagsUseCasePagesForRepositioning()
    {
        var homepage = new TargetPageSnapshot
        {
            Page = new Page { Url = "https://www.geekatyourspot.com/" },
            InternalLinks =
            [
                new TargetPageInternalLink
                {
                    AbsoluteUrl = "https://www.geekatyourspot.com/use-cases/marketing/content-marketing",
                    AnchorText = "AI Content Marketing",
                },
                new TargetPageInternalLink
                {
                    AbsoluteUrl = "https://www.geekatyourspot.com/use-cases/marketing/customer-journeys",
                    AnchorText = "Customer Journeys",
                },
            ],
        };

        var recommendations = SiteAnalyzer2.Services.ProfileAssembly.SiteProfileAssemblerHelpers
            .BuildHomepageWritingRecommendations(
                homepage,
                "Local business · Professional Service",
                "Broward County (Florida), Palm Beach County (Florida)",
                ["Delray Beach", "FL"],
                ["ArtificialIntelligence UseCases"],
                "Technology consultancy helping small businesses implement AI.");

        Assert.Contains(recommendations, r => r.Contains("rewrite in place", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(recommendations, r => r.Contains("AI Content Marketing", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(recommendations, r => r.Contains("TechArticle", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FindMatchedPillarTopic_MatchesKeywordToHeading()
    {
        var topic = SiteAnalyzer2.Services.ProfileAssembly.SiteProfileAssemblerHelpers.FindMatchedPillarTopic(
            "ai content marketing",
            ["Content Marketing Strategy", "About Us"]);

        Assert.Equal("Content Marketing Strategy", topic);
    }

    [Fact]
    public void BuildGapTopicsFromResearch_IncludesKeywordHeadingsAndFindings()
    {
        var findings = new List<Finding>
        {
            new()
            {
                FindingType = FindingType.ContentBlockGap,
                PayloadJson = """{"missingBlockTypes":["faq"]}""",
            },
        };

        var gaps = SiteAnalyzer2.Services.ProfileAssembly.SiteProfileAssemblerHelpers.BuildGapTopicsFromResearch(
            findings,
            [],
            "ai bookkeeping",
            ["Our Services", "Pricing"],
            ["Implementation Guide", "FAQ Section"]);

        Assert.Contains("ai bookkeeping", gaps);
        Assert.Contains("Add faq content block", gaps);
        Assert.Contains("Implementation Guide", gaps);
        Assert.Contains("Our Services", gaps);
    }

    [Fact]
    public void BuildNicheTagsFromHomepage_DoesNotInjectRunKeywords()
    {
        var tags = SiteAnalyzer2.Services.ProfileAssembly.SiteProfileAssemblerHelpers.BuildNicheTagsFromHomepage(
            ["Managed IT"],
            [],
            "Local business",
            "Example Co");

        Assert.DoesNotContain(tags, t => t.Equals("best crm software", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Managed IT", tags);
    }

    [Fact]
    public void BuildGapTopics_PrefersFindingPayload()
    {
        var findings = new List<Finding>
        {
            new()
            {
                Id = Guid.NewGuid(),
                RunId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                FindingType = FindingType.ContentBlockGap,
                PayloadJson = """{"missingBlockTypes":["faq"]}""",
            },
        };

        var gaps = SiteAnalyzer2.Services.ProfileAssembly.SiteProfileAssemblerHelpers.BuildGapTopics(
            findings,
            [],
            "ai content marketing");

        Assert.Contains("Add faq content block", gaps);
    }

    [Fact]
    public void BuildWritingInstructions_IncludesContentQualityBar()
    {
        var siteWrite = new SiteProfileAssemblyWrite
        {
            BusinessSummary = "AI consulting for small businesses.",
        };
        var runWrite = new RunWritingFocusWrite
        {
            MatchedPillarTopic = "ai bookkeeping",
            GapTopics = ["ai bookkeeping", "integrations"],
        };

        var instructions = SiteAnalyzer2.Services.ProfileAssembly.SiteProfileAssemblerHelpers.BuildWritingInstructions(
            siteWrite,
            runWrite,
            "ai bookkeeping");

        Assert.Contains(
            SiteAnalyzer2.Services.ProfileAssembly.SiteProfileAssemblerHelpers.ContentQualityBarInstruction,
            instructions);
        Assert.Contains("integrations", instructions, StringComparison.Ordinal);
    }

    [Fact]
    public void HomepageJsonLdRecommendationBuilder_ProducesBusinessAndWebsiteScriptBlocks()
    {
        var profile = new SiteProfile
        {
            SiteUrl = "https://www.geekatyourspot.com/",
            DisplayName = "geekatyourspot.com",
            BusinessProfileAt = DateTime.UtcNow,
            BusinessType = "Local business · Professional service",
            BusinessSummary = "AI consulting firm helping small businesses implement AI and automation.",
            PrimaryNiche = "Geek at Your Spot",
            ServiceAreaDescription = "Broward County (Florida)",
            GeoAnchorNodes = ["Broward County", "Delray Beach"],
            NicheTags = ["Managed IT Services", "Marketing"],
        };

        var snippets = SiteAnalyzer2.Services.ProfileAssembly.HomepageJsonLdRecommendationBuilder.Build(profile);

        Assert.Equal(2, snippets.Count);
        Assert.Equal("business-entity", snippets[0].Id);
        Assert.Equal("website", snippets[1].Id);
        Assert.Contains("LocalBusiness", snippets[0].ScriptTag, StringComparison.Ordinal);
        Assert.Contains("#business", snippets[0].ScriptTag, StringComparison.Ordinal);
        Assert.Contains("Geek at Your Spot", snippets[0].ScriptTag, StringComparison.Ordinal);
        Assert.Contains("knowsAbout", snippets[0].ScriptTag, StringComparison.Ordinal);
        Assert.Contains("WebSite", snippets[1].ScriptTag, StringComparison.Ordinal);
        Assert.Contains("\"publisher\"", snippets[1].ScriptTag, StringComparison.Ordinal);
        Assert.StartsWith("<script type=\"application/ld+json\">", snippets[0].ScriptTag, StringComparison.Ordinal);
        Assert.StartsWith("<script type=\"application/ld+json\">", snippets[1].ScriptTag, StringComparison.Ordinal);
    }

    [Fact]
    public void HomepageJsonLdRecommendationBuilder_ReturnsEmptyWhenProfileNotAssembled()
    {
        var profile = new SiteProfile
        {
            SiteUrl = "https://www.example.com/",
            DisplayName = "example.com",
        };

        var snippets = SiteAnalyzer2.Services.ProfileAssembly.HomepageJsonLdRecommendationBuilder.Build(profile);

        Assert.Empty(snippets);
    }
}

public class ContentWriterSiteBundleBuilderTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 6, 24, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Build_IncludesFullSiteProfileFieldsAndBundleMetadata()
    {
        var profileId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var profile = new SiteProfile
        {
            Id = profileId,
            GeekSeoProjectId = projectId,
            SiteUrl = "https://www.geekatyourspot.com/",
            DisplayName = "Geek at Your Spot",
            CreatedAt = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 6, 24, 9, 0, 0, DateTimeKind.Utc),
            BusinessProfileAt = new DateTime(2026, 6, 24, 9, 0, 0, DateTimeKind.Utc),
            LastRunAt = new DateTime(2026, 6, 24, 10, 0, 0, DateTimeKind.Utc),
            BusinessType = "ProfessionalService",
            BusinessDescription = "AI implementation consultancy",
            BusinessSummary = "Helps SMBs adopt AI.",
            GeneratedSchemaJson = "{\"@context\":\"https://schema.org\",\"@type\":\"ProfessionalService\"}",
            PrimaryNiche = "AI consulting",
            NicheDescription = "Local AI implementation",
            NicheTags = ["AI", "automation"],
            GeoAnchorNodes = ["Delray Beach, FL"],
            ServiceAreaDescription = "South Florida",
            CompetitorDomains = ["competitor.com"],
            AuthorityPageUrls = ["https://www.geekatyourspot.com/use-cases/"],
            WritingRecommendations = ["Use implementation-first framing."],
        };

        var bundle = ContentWriterSiteBundleBuilder.Build(profile, CapturedAt);

        Assert.Equal(ContentWriterSiteBundleDto.CurrentBundleVersion, bundle.BundleVersion);
        Assert.Equal(CapturedAt, bundle.CapturedAt);
        Assert.Equal(profileId, bundle.SiteProfileId);
        Assert.Equal(projectId, bundle.GeekSeoProjectId);
        Assert.Equal("https://www.geekatyourspot.com/", bundle.SiteUrl);
        Assert.Contains("ProfessionalService", bundle.GeneratedSchemaJson ?? "");
        Assert.Equal("AI consulting", bundle.PrimaryNiche);
        Assert.Equal(["AI", "automation"], bundle.NicheTags);
        Assert.Equal(["Delray Beach, FL"], bundle.GeoAnchorNodes);
        Assert.NotEmpty(bundle.RecommendedHomepageJsonLd);
    }

    [Fact]
    public async Task GetByProfileId_ReturnsNullWhenMissing()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var service = new ContentWriterSiteBundleService(db);
        var bundle = await service.GetByProfileIdAsync(Guid.NewGuid());

        Assert.Null(bundle);
    }

    [Fact]
    public async Task GetByProfileId_LoadsBundleFromDatabase()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var profileId = Guid.NewGuid();
        db.SiteProfiles.Add(new SiteProfile
        {
            Id = profileId,
            SiteUrl = "https://www.example.com/",
            DisplayName = "Example",
            PrimaryNiche = "Widgets",
        });
        await db.SaveChangesAsync();

        var service = new ContentWriterSiteBundleService(db);
        var bundle = await service.GetByProfileIdAsync(profileId);

        Assert.NotNull(bundle);
        Assert.Equal(profileId, bundle!.SiteProfileId);
        Assert.Equal("Widgets", bundle.PrimaryNiche);
    }
}

public class ContentWriterKeywordBundleBuilderTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 6, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Build_IncludesRunPillarGapsAndBundleMetadata()
    {
        var runId = Guid.NewGuid();
        var run = new AnalysisRun
        {
            Id = runId,
            ProjectId = Guid.NewGuid(),
            Keyword = "ai content marketing",
            TargetSiteUrl = "https://www.geekatyourspot.com/",
            Status = RunStatus.Running,
            SerpSeResultsCount = 1_200_000,
            SerpCapturedAt = new DateTime(2026, 6, 24, 10, 0, 0, DateTimeKind.Utc),
            MatchedPillarTopic = "AI Content Marketing",
            MatchedPillarIntent = "informational",
            MatchedPillarAngle = "implementation framework",
            GapTopics = ["content operations", "customer journeys"],
            WritingInstructions = "Target keyword: ai content marketing.",
            CompetitorCrawlStatus = "complete",
            CompetitorCrawlFinishedAt = new DateTime(2026, 6, 24, 11, 0, 0, DateTimeKind.Utc),
        };

        var export = ContentWriterKeywordBundleBuilder.Build(run, [], [], [], CapturedAt);

        Assert.Equal(ContentWriterSerpExportDto.CurrentBundleVersion, export.BundleVersion);
        Assert.Equal(CapturedAt, export.CapturedAt);
        Assert.Equal("AI Content Marketing", export.MatchedPillarTopic);
        Assert.Equal("informational", export.MatchedPillarIntent);
        Assert.Equal("implementation framework", export.MatchedPillarAngle);
        Assert.Equal(["content operations", "customer journeys"], export.GapTopics);
        Assert.Contains(export.WritingRecommendations, r => r.Contains("AI Content Marketing", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("complete", export.CompetitorCrawlStatus);
    }

    [Fact]
    public void Build_ExportsCompetitorSeedHeadingsSchemaAndBenchmarks()
    {
        var run = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Keyword = "test",
            TargetSiteUrl = "https://www.example.com/",
        };

        var competitorPageId = Guid.NewGuid();
        var competitorPages = new List<CompetitorPage>
        {
            new()
            {
                Id = competitorPageId,
                RunId = run.Id,
                Domain = "hubspot.com",
                Url = "https://blog.hubspot.com/marketing/ai-content",
                SeedRankAbsolute = 1,
                DepthFromSeed = 0,
                Headings =
                [
                    new CompetitorPageHeading { Level = 1, Text = "AI Content Marketing", Sequence = 1 },
                    new CompetitorPageHeading { Level = 2, Text = "Planning", Sequence = 2 },
                    new CompetitorPageHeading { Level = 2, Text = "Execution", Sequence = 3 },
                ],
                JsonLdBlocks =
                [
                    new CompetitorPageJsonLd { ParsedType = "Article" },
                    new CompetitorPageJsonLd { ParsedType = "FAQPage" },
                ],
            },
            new()
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                Domain = "hubspot.com",
                Url = "https://blog.hubspot.com/marketing/ai-content/deep",
                SeedRankAbsolute = 1,
                DepthFromSeed = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                Domain = "semrush.com",
                Url = "https://www.semrush.com/blog/ai-marketing",
                SeedRankAbsolute = 2,
                DepthFromSeed = 0,
                Headings =
                [
                    new CompetitorPageHeading { Level = 2, Text = "Overview", Sequence = 1 },
                ],
            },
        };

        var export = ContentWriterKeywordBundleBuilder.Build(run, [], competitorPages, [], CapturedAt);

        Assert.Equal(2, export.Competitors.Count);
        var hubspot = export.Competitors[0];
        Assert.Equal("hubspot.com", hubspot.Domain);
        Assert.Equal(2, hubspot.PagesCrawledOnDomain);
        Assert.Equal(2, hubspot.Headings.Count(h => h.Level == 2));
        Assert.Contains("Article", hubspot.SchemaTypes);
        Assert.Contains("FAQPage", hubspot.SchemaTypes);
        Assert.True(hubspot.HasFaqSchema);
        Assert.True(hubspot.WordCountEstimate > 0);
        Assert.Equal(2, export.Benchmarks.MedianH2CountTop5);
        Assert.Equal(2, export.Benchmarks.CompetitorDomainCount);
        Assert.Equal(3, export.Benchmarks.CompetitorPageCount);
    }

    [Fact]
    public void Build_ExportsPasfAndPaaAsSeparateSerpItems()
    {
        var run = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Keyword = "widget repair",
            TargetSiteUrl = "https://www.example.com/",
        };

        var pasfItemId = Guid.NewGuid();
        var paaItemId = Guid.NewGuid();
        var serpItems = new List<SerpItem>
        {
            new()
            {
                Id = pasfItemId,
                RunId = run.Id,
                Type = SerpItemTypes.RelatedSearches,
                RankAbsolute = 1,
                RelatedQueries =
                [
                    new SerpRelatedQuery
                    {
                        Sequence = 1,
                        QueryText = "widget repair cost",
                        QueryType = SerpRelatedQueryType.PeopleAlsoSearchFor,
                    },
                ],
            },
            new()
            {
                Id = paaItemId,
                RunId = run.Id,
                Type = SerpItemTypes.PeopleAlsoAsk,
                RankAbsolute = 2,
                RelatedQueries =
                [
                    new SerpRelatedQuery
                    {
                        Sequence = 1,
                        QueryText = "What is widget repair?",
                        QueryType = SerpRelatedQueryType.PeopleAlsoAsk,
                    },
                ],
            },
        };

        var export = ContentWriterKeywordBundleBuilder.Build(run, serpItems, [], [], CapturedAt);

        Assert.Contains(export.Serp, i => i.Type == SerpItemTypes.RelatedSearches
            && i.RelatedQuestions!.Contains("widget repair cost"));
        Assert.Contains(export.Serp, i => i.Type == SerpItemTypes.PeopleAlsoAsk
            && i.RelatedQuestions!.Contains("What is widget repair?"));
    }

    [Fact]
    public void Build_ExportsCitationCandidatesFromAuthorityPagesOnly()
    {
        var run = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Keyword = "ai bookkeeping",
            TargetSiteUrl = "https://www.example.com/",
        };

        var serpItems = new List<SerpItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                Type = SerpItemTypes.Organic,
                RankAbsolute = 1,
                Url = "https://competitor.com/guide",
                Title = "AI Bookkeeping Guide",
                Domain = "competitor.com",
                Ads = false,
            },
        };

        var export = ContentWriterKeywordBundleBuilder.Build(
            run,
            serpItems,
            [],
            [],
            CapturedAt,
            ["https://www.example.com/resources/"]);

        Assert.Single(export.CitationCandidates);
        Assert.DoesNotContain(export.CitationCandidates, c =>
            c.Source == "organic" && c.Url.Contains("competitor.com", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(export.CitationCandidates, c =>
            c.Source == "authority" && c.Url.Contains("/resources/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_ExportsSourceHeadingsFromTargetSitePage()
    {
        var run = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Keyword = "test",
            TargetSiteUrl = "https://www.geekatyourspot.com/",
        };

        var pageId = Guid.NewGuid();
        var sourcePages = new List<Page>
        {
            new()
            {
                Id = pageId,
                RunId = run.Id,
                Url = "https://www.geekatyourspot.com/",
                IsTargetSite = true,
                Headings =
                [
                    new PageHeading { PageId = pageId, Level = 1, Text = "Geek at Your Spot", Sequence = 1 },
                    new PageHeading { PageId = pageId, Level = 2, Text = "AI Implementation", Sequence = 2 },
                ],
            },
        };

        var export = ContentWriterKeywordBundleBuilder.Build(run, [], [], sourcePages, CapturedAt);

        Assert.Equal(2, export.SourceHeadings.Count);
        Assert.Equal("AI Implementation", export.SourceHeadings[1].Text);
    }

    [Fact]
    public void BuildExport_LegacyHelperStillMapsSerpItems()
    {
        var run = new AnalysisRun
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Keyword = "test",
            TargetSiteUrl = "https://www.example.com/",
        };

        var items = new List<SerpItem>
        {
            new()
            {
                Type = SerpItemTypes.Organic,
                RankGroup = 1,
                RankAbsolute = 1,
                Title = "Example",
                Url = "https://example.com/post",
                Domain = "example.com",
                Description = "Snippet text",
            },
        };

        var export = ContentWriterExportService.BuildExport(run, items);

        Assert.Single(export.Serp);
        Assert.Equal(SerpItemTypes.Organic, export.Serp[0].Type);
        Assert.Equal("Snippet text", export.Serp[0].Snippet);
    }
}

public class TargetSiteUrlNormalizerTests
{
    [Theory]
    [InlineData("geekatyourspot.com", "https://www.geekatyourspot.com/")]
    [InlineData("https://geekatyourspot.com/", "https://www.geekatyourspot.com/")]
    [InlineData("http://GeekAtYourSpot.com/", "https://www.geekatyourspot.com/")]
    [InlineData("HTTPS://WWW.geekatyourspot.com/path?q=1", "https://www.geekatyourspot.com/")]
    [InlineData("https://www.geekatyourspot.com", "https://www.geekatyourspot.com/")]
    [InlineData("  https://Example.COM:443/  ", "https://www.example.com/")]
    public void Normalize_CanonicalShape(string input, string expected)
    {
        Assert.Equal(expected, SiteAnalyzer2.Services.Integrations.TargetSiteUrlNormalizer.Normalize(input));
    }

    [Fact]
    public void Equals_MatchesLegacyAnalysisRunShape()
    {
        Assert.True(SiteAnalyzer2.Services.Integrations.TargetSiteUrlNormalizer.Equals(
            "https://www.geekatyourspot.com/",
            "geekatyourspot.com"));
    }

    [Fact]
    public void Equals_IgnoresSchemeCaseAndTrailingSlash()
    {
        Assert.True(SiteAnalyzer2.Services.Integrations.TargetSiteUrlNormalizer.Equals(
            "http://GEEKATYOURSPOT.com/",
            "https://www.geekatyourspot.com/"));
    }

    [Fact]
    public void Equals_IgnoresWwwPrefix()
    {
        Assert.True(SiteAnalyzer2.Services.Integrations.TargetSiteUrlNormalizer.Equals(
            "https://www.geekatyourspot.com",
            "https://www.geekatyourspot.com/"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url!!!")]
    public void Normalize_Invalid_ReturnsEmpty(string input)
    {
        Assert.Equal(string.Empty, SiteAnalyzer2.Services.Integrations.TargetSiteUrlNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("https://site-analyzer.geekatyourspot.com/", "https://www.site-analyzer.geekatyourspot.com/")]
    [InlineData("https://www.site-analyzer.geekatyourspot.com/", "https://www.site-analyzer.geekatyourspot.com/")]
    [InlineData("site-analyzer.geekatyourspot.com", "https://www.site-analyzer.geekatyourspot.com/")]
    public void Normalize_Subdomain_PreservesHost(string input, string expected)
    {
        Assert.Equal(expected, SiteAnalyzer2.Services.Integrations.TargetSiteUrlNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("https://www.geekatyourspot.com/", true)]
    [InlineData("https://site-analyzer.geekatyourspot.com/", false)]
    [InlineData("https://www.geekatyourspot.com", false)]
    [InlineData("HTTP://WWW.GEEKATYOURSPOT.COM/", false)]
    public void IsValidStoredFormat_MatchesDatabaseCheck(string url, bool expected)
    {
        Assert.Equal(
            expected,
            SiteAnalyzer2.Services.Integrations.TargetSiteUrlNormalizer.IsValidStoredFormat(url));
    }

    [Fact]
    public void Normalize_Output_AlwaysPassesStoredFormatCheck()
    {
        foreach (var input in new[]
                 {
                     "geekatyourspot.com",
                     "https://site-analyzer.geekatyourspot.com/",
                     "https://GEEKATYOURSPOT.com/path",
                 })
        {
            var normalized = SiteAnalyzer2.Services.Integrations.TargetSiteUrlNormalizer.Normalize(input);
            Assert.True(
                SiteAnalyzer2.Services.Integrations.TargetSiteUrlNormalizer.IsValidStoredFormat(normalized),
                $"Normalize({input}) => {normalized}");
        }
    }
}

public class OperatorResearchServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ListContentPillars_ReturnsDistinctKeywordsPerRun()
    {
        await using var db = CreateDb();
        var siteUrl = "https://www.geekatyourspot.com/";
        var projectId = Guid.NewGuid();
        var runA = Guid.NewGuid();
        var runB = Guid.NewGuid();

        db.AnalysisRuns.AddRange(
            new AnalysisRun
            {
                Id = runA,
                ProjectId = projectId,
                Keyword = "ai bookkeeping",
                TargetSiteUrl = siteUrl,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
            },
            new AnalysisRun
            {
                Id = runB,
                ProjectId = projectId,
                Keyword = "managed it services",
                TargetSiteUrl = siteUrl,
                CreatedAt = DateTime.UtcNow,
                GapTopics = ["ai bookkeeping", "pricing"],
            });
        db.CompetitorPages.Add(new CompetitorPage
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RunId = runB,
            Domain = "competitor.com",
            Url = "https://competitor.com/",
            SeedRankAbsolute = 1,
        });
        await db.SaveChangesAsync();

        var service = new OperatorResearchService(db, new SerpRankHistoryService(db));
        var pillars = await service.ListContentPillarsAsync(siteUrl);

        Assert.Equal(2, pillars.Count);
        Assert.Equal("managed it services", pillars[0].Keyword);
        Assert.True(pillars[0].GapTopicsReady);
        Assert.True(pillars[0].CompetitorCrawlComplete);
        Assert.Equal("ai bookkeeping", pillars[1].Keyword);
        Assert.False(pillars[1].GapTopicsReady);
    }

    [Fact]
    public async Task GetResearchFocus_ReportsGatesAndReadiness()
    {
        await using var db = CreateDb();
        var projectId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var pageId = Guid.NewGuid();

        db.AnalysisRuns.Add(new AnalysisRun
        {
            Id = runId,
            ProjectId = projectId,
            Keyword = "ai bookkeeping",
            TargetSiteUrl = "https://www.geekatyourspot.com/",
            MatchedPillarTopic = "ai bookkeeping",
            MatchedPillarIntent = "commercial",
            GapTopics = ["ai bookkeeping", "FAQ section"],
            WritingInstructions = "Target keyword: ai bookkeeping.",
        });
        db.CompetitorPages.Add(new CompetitorPage
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RunId = runId,
            Domain = "competitor.com",
            Url = "https://competitor.com/",
            SeedRankAbsolute = 1,
        });
        var competitorPageId = db.CompetitorPages.Local.Single().Id;
        db.CompetitorPageHeadings.Add(new CompetitorPageHeading
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            CompetitorPageId = competitorPageId,
            Level = 2,
            Text = "Features",
            Sequence = 0,
        });
        var serpItemId = Guid.NewGuid();
        db.SerpItems.Add(new SerpItem
        {
            Id = serpItemId,
            ProjectId = projectId,
            RunId = runId,
            Type = SerpItemTypes.PeopleAlsoAsk,
            RankAbsolute = 2,
        });
        db.SerpRelatedQueries.Add(new SerpRelatedQuery
        {
            Id = Guid.NewGuid(),
            SerpItemId = serpItemId,
            Sequence = 0,
            QueryText = "What is AI bookkeeping?",
            QueryType = SerpRelatedQueryType.PeopleAlsoAsk,
        });
        db.SerpItems.Add(new SerpItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RunId = runId,
            Type = SerpItemTypes.Organic,
            RankAbsolute = 1,
            Url = "https://competitor.com/",
            Ads = false,
        });
        db.Pages.Add(new Page
        {
            Id = pageId,
            ProjectId = projectId,
            RunId = runId,
            Url = "https://www.geekatyourspot.com/",
            IsTargetSite = true,
        });
        db.PageHeadings.Add(new PageHeading
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            PageId = pageId,
            Level = 2,
            Text = "Services",
            Sequence = 0,
        });
        db.Findings.Add(new Finding
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RunId = runId,
            FindingType = FindingType.HeadingStructureGap,
            Severity = "medium",
            PayloadJson = "{}",
        });
        await db.SaveChangesAsync();

        var service = new OperatorResearchService(db, new SerpRankHistoryService(db));
        var focus = await service.GetResearchFocusAsync(runId);

        Assert.NotNull(focus);
        Assert.True(focus!.ResearchReady);
        Assert.Equal(2, focus.GapTopics.Count);
        Assert.Equal(5, focus.Gates.Count);
        Assert.True(focus.Gates.Single(g => g.Id == "gaps").Complete);
        Assert.Equal(1, focus.PackStats.PaaQuestionCount);
        Assert.Equal(1, focus.PackStats.CompetitorPageCount);
        Assert.Equal(1, focus.PackStats.CompetitorHeadingCount);
        Assert.Equal(1, focus.PackStats.SourceHeadingCount);
        Assert.Equal(2, focus.PackStats.GapTopicCount);
        Assert.False(focus.Rankings.HasRecapture);
        Assert.Empty(focus.Rankings.History);
    }

    [Fact]
    public async Task ListByProject_ReportsContentWritingReadyFromResearchGates()
    {
        await using var db = CreateDb();
        var projectId = Guid.NewGuid();
        var readyRunId = Guid.NewGuid();
        var thinRunId = Guid.NewGuid();
        var pageId = Guid.NewGuid();

        db.AnalysisRuns.AddRange(
            new AnalysisRun
            {
                Id = readyRunId,
                ProjectId = projectId,
                Keyword = "ready keyword",
                TargetSiteUrl = "https://www.geekatyourspot.com/",
                GapTopics = ["topic"],
            },
            new AnalysisRun
            {
                Id = thinRunId,
                ProjectId = projectId,
                Keyword = "thin keyword",
                TargetSiteUrl = "https://www.geekatyourspot.com/",
            });

        db.SerpItems.Add(new SerpItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RunId = readyRunId,
            Type = SerpItemTypes.Organic,
            RankAbsolute = 1,
            Url = "https://competitor.com/",
            Ads = false,
        });
        db.CompetitorPages.Add(new CompetitorPage
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RunId = readyRunId,
            Domain = "competitor.com",
            Url = "https://competitor.com/",
            SeedRankAbsolute = 1,
        });
        db.Pages.Add(new Page
        {
            Id = pageId,
            ProjectId = projectId,
            RunId = readyRunId,
            Url = "https://www.geekatyourspot.com/",
            IsTargetSite = true,
        });
        db.PageHeadings.Add(new PageHeading
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            PageId = pageId,
            Level = 2,
            Text = "Services",
            Sequence = 0,
        });
        db.Findings.Add(new Finding
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RunId = readyRunId,
            FindingType = FindingType.HeadingStructureGap,
            Severity = "medium",
            PayloadJson = "{}",
        });
        db.SerpItems.Add(new SerpItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RunId = thinRunId,
            Type = SerpItemTypes.Organic,
            RankAbsolute = 1,
            Url = "https://other.com/",
            Ads = false,
        });
        await db.SaveChangesAsync();

        var exportService = new ContentWriterExportService(db, new OperatorResearchService(db, new SerpRankHistoryService(db)));
        var summaries = await exportService.ListByProjectAsync(projectId);

        Assert.Equal(2, summaries.Count);
        var ready = summaries.Single(s => s.Id == readyRunId);
        var thin = summaries.Single(s => s.Id == thinRunId);
        Assert.True(ready.ContentWritingReady);
        Assert.False(thin.ContentWritingReady);
    }
}

public class SerpRankTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static SerpItem OrganicItem(string domain, string url, int rank) =>
        new()
        {
            Id = Guid.NewGuid(),
            Type = SerpItemTypes.Organic,
            Domain = domain,
            Url = url,
            RankAbsolute = rank,
            Ads = false,
        };

    [Fact]
    public void ResolveFromItems_PicksBestOwnedOrganicPosition()
    {
        var items = new[]
        {
            OrganicItem("competitor.com", "https://competitor.com/a", 1),
            OrganicItem("geekatyourspot.com", "https://www.geekatyourspot.com/services", 12),
            OrganicItem("geekatyourspot.com", "https://geekatyourspot.com/blog/post", 8),
        };

        var result = SerpTargetRankResolver.ResolveFromItems("https://www.geekatyourspot.com/", items);

        Assert.Equal(8, result.Position);
        Assert.Equal("https://geekatyourspot.com/blog/post", result.Url);
    }

    [Fact]
    public void ResolveFromItems_ReturnsNullWhenTargetNotInSerp()
    {
        var items = new[]
        {
            OrganicItem("competitor.com", "https://competitor.com/a", 1),
        };

        var result = SerpTargetRankResolver.ResolveFromItems("https://www.geekatyourspot.com/", items);

        Assert.Null(result.Position);
        Assert.Null(result.Url);
    }

    [Fact]
    public async Task RecordAfterImport_BuildsDeltaOnSecondImport()
    {
        await using var db = CreateDb();
        var projectId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var siteUrl = "https://www.geekatyourspot.com/";

        db.AnalysisRuns.Add(new AnalysisRun
        {
            Id = runId,
            ProjectId = projectId,
            Keyword = "ai bookkeeping",
            TargetSiteUrl = siteUrl,
            SerpCapturedAt = DateTime.UtcNow.AddDays(-7),
        });
        db.SerpItems.AddRange(
            OrganicItem("competitor.com", "https://competitor.com/", 1),
            OrganicItem("geekatyourspot.com", "https://www.geekatyourspot.com/ai", 15));
        foreach (var item in db.SerpItems.Local)
        {
            item.ProjectId = projectId;
            item.RunId = runId;
        }
        await db.SaveChangesAsync();

        var history = new SerpRankHistoryService(db);
        var first = await history.RecordAfterImportAsync(runId);
        Assert.NotNull(first);
        Assert.Equal(15, first!.TargetOrganicPosition);
        Assert.Null(first.RankingsDelta);

        db.SerpItems.RemoveRange(db.SerpItems.ToList());
        await db.SaveChangesAsync();

        var newerItems = new[]
        {
            OrganicItem("competitor.com", "https://competitor.com/", 1),
            OrganicItem("geekatyourspot.com", "https://www.geekatyourspot.com/ai", 9),
        };
        foreach (var item in newerItems)
        {
            item.ProjectId = projectId;
            item.RunId = runId;
        }
        db.SerpItems.AddRange(newerItems);
        db.AnalysisRuns.Local.Single().SerpCapturedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var second = await history.RecordAfterImportAsync(runId);
        Assert.NotNull(second);
        Assert.Equal(9, second!.TargetOrganicPosition);
        Assert.NotNull(second.RankingsDelta);
        Assert.Equal(15, second.RankingsDelta!.PreviousPosition);
        Assert.Equal(9, second.RankingsDelta.CurrentPosition);
        Assert.Equal(6, second.RankingsDelta.PositionChange);

        var summary = await history.GetSummaryAsync(runId);
        Assert.True(summary.HasRecapture);
        Assert.Equal(2, summary.History.Count);
        Assert.Equal(6, summary.LatestDelta!.PositionChange);
    }
}

public class SiteAuditCheckServiceTests
{
    [Fact]
    public void SiteAuditCheckService_FlagsBrokenAndMissingTitle()
    {
        var checks = new SiteAuditCheckService();
        var input = new SiteAuditCheckInput(
            "https://www.example.com/",
            [
                new SiteAuditPageSnapshot(
                    Guid.NewGuid(),
                    "https://www.example.com/broken",
                    404,
                    1,
                    true,
                    ["h1"],
                    [("title", "Broken page")],
                    ["WebPage"]),
                new SiteAuditPageSnapshot(
                    Guid.NewGuid(),
                    "https://www.example.com/no-title",
                    200,
                    1,
                    true,
                    ["h1"],
                    [],
                    ["WebPage"]),
            ],
            [
                new SiteAuditLinkSnapshot("https://www.example.com/", "https://www.example.com/broken", true),
                new SiteAuditLinkSnapshot("https://www.example.com/", "https://www.example.com/no-title", true),
            ]);

        var issues = checks.RunAllChecks(input);

        Assert.Contains(issues, i => i.Code == AuditIssueCode.BrokenPage && i.Severity == AuditSeverity.Error);
        Assert.Contains(issues, i => i.Code == AuditIssueCode.MissingTitleTag);
    }

    [Fact]
    public void SiteAuditRollupService_ComputesHealthScoreAndTopIssues()
    {
        var rollup = new SiteAuditRollupService();
        var pages = new List<SiteAuditPageSnapshot>
        {
            new(Guid.NewGuid(), "https://www.example.com/a", 200, 1, true, ["h1"], [("title", "A")], []),
            new(Guid.NewGuid(), "https://www.example.com/b", 200, 1, true, ["h1"], [], []),
        };
        var issues = new List<AuditIssue>
        {
            new(
                AuditIssueCode.MissingTitleTag,
                SiteAuditCategory.Markups,
                AuditSeverity.Error,
                "Missing title tag",
                "test",
                ["https://www.example.com/b"],
                "fix"),
        };

        var overview = rollup.BuildOverview(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SiteAuditStatuses.Complete,
            pages,
            issues,
            DateTime.UtcNow,
            null);

        Assert.InRange(overview.HealthScore, 1, 99);
        Assert.Equal(1, overview.ErrorsCount);
        Assert.Single(overview.TopIssues);
        Assert.Equal(SiteAuditCategory.Markups, overview.Categories.First(c => c.IssueCount > 0).Category);
    }
}

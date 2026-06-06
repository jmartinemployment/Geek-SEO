using System.Net;
using System.Text;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services;
using GeekSeoBackend.Services.NicheExtraction;
using Microsoft.Extensions.Logging.Abstractions;

namespace GeekSeoBackend.Tests;

public sealed class NicheExtractionTests
{
    private const string GeekAtYourSpotJsonLd = """
        <html><head>
        <script type="application/ld+json">{
          "@context":"https://schema.org",
          "@type":["LocalBusiness","ProfessionalService"],
          "@id":"https://www.geekatyourspot.com/#business",
          "name":"Geek at Your Spot",
          "knowsAbout":[
            "Artificial Intelligence","Process Automation","AI Chatbots",
            "Data Analytics","AI Strategy Consulting","Security and Compliance",
            "Web Application Development"
          ]
        }</script>
        <script type="application/ld+json">[
          {"@type":"WebSite","name":"Geek at Your Spot","url":"https://www.geekatyourspot.com"},
          {
            "@type":"Service",
            "name":"AI Strategy & Implementation Consulting",
            "serviceType":"AI Consulting",
            "hasOfferCatalog":{
              "@type":"OfferCatalog",
              "itemListElement":[
                {"@type":"Offer","itemOffered":{"@type":"Service","name":"Business Objectives Analysis"}},
                {"@type":"Offer","itemOffered":{"@type":"Service","name":"Data Quality Assessment"}},
                {"@type":"Offer","itemOffered":{"@type":"Service","name":"AI Technology Selection"}},
                {"@type":"Offer","itemOffered":{"@type":"Service","name":"AI Implementation Strategy"}}
              ]
            }
          }
        ]</script>
        </head><body></body></html>
        """;

    [Fact]
    public async Task SchemaOrgExtractor_ParsesKnowsAboutAndOfferCatalogBlocks()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(GeekAtYourSpotJsonLd, Encoding.UTF8, "text/html"),
        });
        var factory = new StubHttpClientFactory(handler);
        var extractor = new SchemaOrgExtractor(factory, NullLogger<SchemaOrgExtractor>.Instance);

        var data = await extractor.ExtractAsync("https://www.geekatyourspot.com", browser: null, CancellationToken.None);

        Assert.Equal(7, data.KnowsAboutTopics.Count);
        Assert.Equal(5, data.OfferCatalogTopics.Count);
        Assert.Equal(12, data.ServiceNames.Count);
        Assert.Contains("Business Objectives Analysis", data.OfferCatalogTopics);
        Assert.Contains("AI Consulting", data.OfferCatalogTopics);
    }

    [Fact]
    public void PillarMerger_KeepsTwelveSchemaTopics_WhenCapIsTwelve()
    {
        var merger = new PillarMerger();
        var schema = FixtureTopics.TwelveDistinct.Select(name => new DiscoveredPillar
        {
            Name = name,
            Slug = NicheAnalyzerService.NameToSlug(name),
            Intent = "commercial",
            Source = "schema",
            ChildPageCount = 3,
        }).ToList();

        var result = merger.Merge(schema, [], [], [], []);

        Assert.Equal(12, result.Selected.Count);
        Assert.Empty(result.ExcludedByCap);
        Assert.Equal(PillarMerger.DefaultPillarCap, result.PillarCap);
    }

    [Fact]
    public void PillarMerger_ReportsExcludedTopics_WhenCapIsLower()
    {
        var merger = new PillarMerger();
        var schema = FixtureTopics.TwelveDistinct.Select(name => new DiscoveredPillar
        {
            Name = name,
            Slug = NicheAnalyzerService.NameToSlug(name),
            Intent = "commercial",
            Source = "schema",
            ChildPageCount = 3,
        }).ToList();

        var result = merger.Merge(schema, [], [], [], [], maxPillars: 7);

        Assert.Equal(7, result.Selected.Count);
        Assert.Equal(5, result.ExcludedByCap.Count);
    }

    [Fact]
    public void TopicFusionEngine_SelectsTwelveGeekAtYourSpotSchemaTopics()
    {
        var pool = FixtureTopics.GeekAtYourSpotSchema.Select(name => new TopicCandidate
        {
            Name = name,
            Slug = NicheAnalyzerService.NameToSlug(name),
            Confidence = TopicEvidenceWeights.Schema,
            Evidence =
            [
                new TopicEvidence
                {
                    Source = "schema",
                    Snippet = "schema",
                    Weight = TopicEvidenceWeights.Schema,
                },
            ],
        }).ToList();

        var engine = new TopicFusionEngine(new PillarValidator());
        var fused = engine.Fuse(pool, []);
        var result = engine.ToPillarMergeResult(fused);

        Assert.Equal(12, result.Selected.Count);
        Assert.Equal(TopicFusionEngine.FusionVersion, fused.FusionVersion);
        Assert.Contains(
            result.Selected,
            p => p.Slug.Equals(NicheAnalyzerService.NameToSlug("AI Consulting"), StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            result.Selected,
            p => p.Slug.Equals(NicheAnalyzerService.NameToSlug("AI Strategy Consulting"), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TopicFusionEngine_IncludesAccounting_WithAllSchemaTopics()
    {
        var pool = FixtureTopics.GeekAtYourSpotSchema.Select(name => new TopicCandidate
        {
            Name = name,
            Slug = NicheAnalyzerService.NameToSlug(name),
            Confidence = TopicEvidenceWeights.Schema,
            Evidence =
            [
                new TopicEvidence
                {
                    Source = "schema",
                    Snippet = "schema",
                    Weight = TopicEvidenceWeights.Schema,
                },
            ],
        }).ToList();

        pool.Add(new TopicCandidate
        {
            Name = "Accounting",
            Slug = NicheAnalyzerService.NameToSlug("Accounting"),
            Confidence = TopicEvidenceWeights.PageVertical,
            Evidence =
            [
                new TopicEvidence
                {
                    Source = "page_vertical",
                    Snippet = "homepage H3 section",
                    Weight = TopicEvidenceWeights.PageVertical,
                },
            ],
        });

        var engine = new TopicFusionEngine(new PillarValidator());
        var result = engine.ToPillarMergeResult(engine.Fuse(pool, []));

        Assert.Equal(13, result.Selected.Count);
        Assert.Equal(15, result.PillarCap);
        Assert.Contains(
            result.Selected,
            p => p.Slug.Equals(NicheAnalyzerService.NameToSlug("Accounting"), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PageContentExtractor_ParsesH3VerticalTopics()
    {
        const string html = """
            <html><body>
            <h2>Our Services</h2>
            <h3>Accounting</h3>
            <h4>Automated Bookkeeping</h4>
            <h3>Customer Service</h3>
            </body></html>
            """;

        var (phrases, verticalTopics, _) = PageContentExtractor.ExtractFromHtml(html);

        Assert.Contains("Accounting", verticalTopics);
        Assert.Contains("Customer Service", verticalTopics);
        Assert.Contains("Our Services", phrases);
        Assert.DoesNotContain("Accounting", phrases);
    }

    [Fact]
    public void PageContentExtractor_PromotesH2UnderIndustriesSection()
    {
        const string html = """
            <html><body>
            <h2>Industries We Serve</h2>
            <h2>Accounting</h2>
            <h2>Healthcare</h2>
            <h2>Why Choose Us</h2>
            </body></html>
            """;

        var (phrases, verticalTopics, _) = PageContentExtractor.ExtractFromHtml(html);

        Assert.Contains("Accounting", verticalTopics);
        Assert.Contains("Healthcare", verticalTopics);
        Assert.Contains("Industries We Serve", phrases);
        Assert.Contains("Why Choose Us", phrases);
        Assert.DoesNotContain("Accounting", phrases);
    }

    [Fact]
    public void PageContentExtractor_PromotesStandaloneVerticalH2()
    {
        const string html = """
            <html><body>
            <h2>AI Consulting</h2>
            <h2>How It Works</h2>
            </body></html>
            """;

        var (phrases, verticalTopics, _) = PageContentExtractor.ExtractFromHtml(html);

        Assert.Contains("AI Consulting", verticalTopics);
        Assert.Contains("How It Works", phrases);
        Assert.DoesNotContain("AI Consulting", phrases);
    }

    [Fact]
    public void GscQueryExtractor_ScorePillarMatch_MatchesAccountingQuery()
    {
        var score = GscQueryExtractor.ScorePillarMatch(
            "Accounting",
            NicheAnalyzerService.NameToSlug("Accounting"),
            "small business accounting services miami");

        Assert.True(score >= 2);
    }

    [Fact]
    public void GscQueryExtractor_ApplyToPool_AddsGscEvidence()
    {
        var pool = new List<TopicCandidate>
        {
            new()
            {
                Name = "Accounting",
                Slug = NicheAnalyzerService.NameToSlug("Accounting"),
                Confidence = TopicEvidenceWeights.PageVertical,
                Evidence =
                [
                    new TopicEvidence
                    {
                        Source = "page_vertical",
                        Snippet = "homepage H2/H3 vertical section",
                        Weight = TopicEvidenceWeights.PageVertical,
                    },
                ],
            },
        };

        var overlay = new GscOwnerOverlay(
            Connected: true,
            Skipped: false,
            SkipReason: null,
            QueryRowCount: 100,
            Clusters: [],
            Matches:
            [
                new GscPillarMatch(
                    "accounting services near me",
                    "accounting",
                    500,
                    12.4,
                    ["accounting services near me", "accounting firm"]),
            ]);

        var updated = GscQueryExtractor.ApplyToPool(pool, overlay);
        var accounting = updated.Single(c => c.Slug == NicheAnalyzerService.NameToSlug("Accounting"));

        Assert.Contains(accounting.Evidence, e => e.Source == "gsc");
        Assert.True(accounting.Confidence > TopicEvidenceWeights.PageVertical);
    }

    [Fact]
    public void FusedSiteUnderstandingJson_RoundTripsSnapshot()
    {
        var fused = new FusedSiteUnderstanding
        {
            AllCandidates =
            [
                new TopicCandidate
                {
                    Name = "Accounting",
                    Slug = "accounting",
                    Confidence = 0.55m,
                    Evidence =
                    [
                        new TopicEvidence
                        {
                            Source = "page_vertical",
                            Snippet = "homepage H2/H3 vertical section",
                            Weight = TopicEvidenceWeights.PageVertical,
                        },
                    ],
                },
            ],
            SelectedPillars = [],
            ExcludedCandidates = [],
            ExclusionReasons = new Dictionary<string, string>(),
            FusionVersion = "sul-1.2",
            SignalSourcesPresent = ["page_vertical"],
            PillarCap = 15,
        };

        var json = FusedSiteUnderstandingJson.Serialize(fused);
        var parsed = FusedSiteUnderstandingJson.Parse(json);

        Assert.NotNull(parsed);
        Assert.Equal("sul-1.2", parsed.FusionVersion);
        Assert.Single(parsed.AllCandidates);
        Assert.Equal("Accounting", parsed.AllCandidates[0].Name);
    }

    [Fact]
    public void PageContentExtractor_ParsesListItemsFromHtml()
    {
        const string html = """
            <html><body>
            <h2>Our Services</h2>
            <ul>
              <li>Managed IT Support</li>
              <li>Cloud Migration Planning</li>
            </ul>
            </body></html>
            """;

        var (phrases, _, listItems) = PageContentExtractor.ExtractFromHtml(html);

        Assert.Equal(2, listItems);
        Assert.Contains("Managed IT Support", phrases);
        Assert.Contains("Cloud Migration Planning", phrases);
    }

    [Fact]
    public void UrlPatternExtractor_ParsesServicePathSegments()
    {
        var extractor = new UrlPatternExtractor();
        var urls = new[]
        {
            "https://example.com/services/accounting-software",
            "https://example.com/solutions/cloud-migration",
        };

        var data = extractor.Extract(urls, "https://example.com");

        Assert.Equal(2, data.Topics.Count);
        Assert.Contains(data.Topics, t => t.Slug == "accounting-software");
        Assert.Contains(data.Topics, t => t.Name == "Accounting Software");
    }

    [Fact]
    public void InternalLinkExtractor_ParsesAnchorTextFromHtml()
    {
        const string html = """
            <html><body>
            <a href="/services/managed-it">Managed IT Support</a>
            <a href="/contact">Learn More</a>
            </body></html>
            """;

        var edges = InternalLinkExtractor.ExtractLinksFromHtml(
            html,
            "https://example.com/",
            "https://example.com").ToList();

        Assert.Single(edges);
        Assert.Equal("Managed IT Support", edges[0].AnchorText);
        Assert.Equal("https://example.com/services/managed-it", edges[0].TargetUrl);
    }

    [Fact]
    public void InternalLinkExtractor_InfersTopicFromUrlWhenAnchorIsGeneric()
    {
        const string html = """
            <html><body>
            <a href="/services/accounting">Learn More</a>
            </body></html>
            """;

        var edges = InternalLinkExtractor.ExtractLinksFromHtml(
            html,
            "https://example.com/",
            "https://example.com").ToList();

        Assert.Single(edges);
        Assert.True(edges[0].InferredFromUrlSlug);
        Assert.Equal("Accounting", edges[0].AnchorText);
    }

    [Fact]
    public void TopicCandidatePoolBuilder_StacksInternalLinkAndUrlPatternEvidence()
    {
        var internalLinks = new InternalLinkData(
            [
                new InternalLinkEdge(
                    "https://example.com/",
                    "https://example.com/services/accounting",
                    "Accounting Services"),
            ],
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["https://example.com/services/accounting"] = 2,
            },
            1);

        var urlPatterns = new UrlPatternData(
            [
                new UrlPatternTopic(
                    "Accounting",
                    "accounting",
                    "https://example.com/services/accounting",
                    "accounting"),
            ],
            1);

        var pool = TopicCandidatePoolBuilder.Build(
            new SchemaOrgData([], [], [], null, null, [], [], [], false),
            new SitemapData([], 0, []),
            new NavMenuData([], "skipped"),
            new HomepageHeadings(),
            new PageContentData([], [], 0),
            internalLinks,
            urlPatterns);

        Assert.Equal(2, pool.Count);
        var accountingServices = pool.First(c =>
            c.Slug.Equals(NicheAnalyzerService.NameToSlug("Accounting Services"), StringComparison.OrdinalIgnoreCase));
        var accountingSlug = pool.First(c => c.Slug == "accounting");

        Assert.Contains(accountingServices.Evidence, e => e.Source == "internal_link");
        Assert.Contains(accountingSlug.Evidence, e => e.Source == "url_pattern");
    }

    [Fact]
    public void AnchorTextFilter_RejectsGenericAnchors()
    {
        Assert.False(AnchorTextFilter.IsUsableTopic("Learn More"));
        Assert.True(AnchorTextFilter.IsUsableTopic("Managed IT Support"));
    }

    [Fact]
    public void TopicCandidatePoolBuilder_StacksEvidenceForSameSlug()
    {
        var schema = new SchemaOrgData(
            ["Artificial Intelligence"],
            ["Artificial Intelligence"],
            [],
            null,
            null,
            [],
            [],
            [],
            false);
        var headings = new HomepageHeadings
        {
            Headings =
            [
                new PageHeading { Level = 2, Text = "Artificial Intelligence" },
            ],
        };

        var pool = TopicCandidatePoolBuilder.Build(
            schema,
            new SitemapData([], 0, []),
            new NavMenuData([], "skipped"),
            headings,
            new PageContentData([], [], 0));

        Assert.Single(pool);
        Assert.Equal(0.45m, pool[0].Confidence);
        Assert.Equal(2, pool[0].Evidence.Count);
    }

    [Fact]
    public void SameAsClassifier_RecognizesWikipediaAndLinkedIn()
    {
        var platforms = SameAsClassifier.ResolvePlatforms(
        [
            "https://en.wikipedia.org/wiki/Geek_At_Your_Spot",
            "https://www.linkedin.com/company/geek-at-your-spot",
            "https://example.com/not-an-entity-hub",
        ]);

        Assert.Contains("wikipedia", platforms);
        Assert.Contains("linkedin", platforms);
        Assert.Equal(2, platforms.Count);
        Assert.True(SameAsClassifier.IsEntityResolved(platforms));
    }

    [Fact]
    public async Task SchemaOrgExtractor_ParsesSameAsUrls()
    {
        const string html = """
            <html><head>
            <script type="application/ld+json">{
              "@context":"https://schema.org",
              "@type":"LocalBusiness",
              "name":"Geek at Your Spot",
              "sameAs":[
                "https://en.wikipedia.org/wiki/Geek_At_Your_Spot",
                "https://www.linkedin.com/company/geek-at-your-spot"
              ],
              "knowsAbout":["Managed IT"]
            }</script>
            </head><body></body></html>
            """;

        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html, Encoding.UTF8, "text/html"),
        });
        var extractor = new SchemaOrgExtractor(new StubHttpClientFactory(handler), NullLogger<SchemaOrgExtractor>.Instance);

        var data = await extractor.ExtractAsync("https://www.geekatyourspot.com", browser: null, CancellationToken.None);

        Assert.True(data.EntityResolved);
        Assert.Equal(2, data.SameAsUrls.Count);
        Assert.Contains("wikipedia", data.ResolvedEntityPlatforms);
    }

    [Fact]
    public void TopicCandidatePoolBuilder_AddsSameAsEvidenceToSchemaTopics()
    {
        var schema = new SchemaOrgData(
            ["Managed IT"],
            ["Managed IT"],
            [],
            null,
            "Geek at Your Spot",
            [],
            ["https://en.wikipedia.org/wiki/Geek_At_Your_Spot"],
            ["wikipedia"],
            true);

        var pool = TopicCandidatePoolBuilder.Build(
            schema,
            new SitemapData([], 0, []),
            new NavMenuData([], "skipped"),
            new HomepageHeadings(),
            new PageContentData([], [], 0));

        Assert.Single(pool);
        Assert.Equal(0.65m, pool[0].Confidence);
        Assert.Contains(pool[0].Evidence, e => e.Source == "same_as");
    }

    [Fact]
    public void TopicFusionEngine_ExcludesSingleSourcePagePhraseWithoutCorroboration()
    {
        var pool = new List<TopicCandidate>
        {
            new()
            {
                Name = "Random Body Phrase",
                Slug = NicheAnalyzerService.NameToSlug("Random Body Phrase"),
                Confidence = TopicEvidenceWeights.Page,
                Evidence =
                [
                    new TopicEvidence
                    {
                        Source = "page",
                        Snippet = "homepage body",
                        Weight = TopicEvidenceWeights.Page,
                    },
                ],
            },
            new()
            {
                Name = "Accounting",
                Slug = NicheAnalyzerService.NameToSlug("Accounting"),
                Confidence = TopicEvidenceWeights.PageVertical,
                Evidence =
                [
                    new TopicEvidence
                    {
                        Source = "page_vertical",
                        Snippet = "homepage H3 section",
                        Weight = TopicEvidenceWeights.PageVertical,
                    },
                ],
            },
        };

        var engine = new TopicFusionEngine(new PillarValidator());
        var fused = engine.Fuse(pool, [], maxPillars: 15);
        var selectedSlugs = fused.SelectedPillars.Select(p => p.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(NicheAnalyzerService.NameToSlug("Accounting"), selectedSlugs);
        Assert.DoesNotContain(NicheAnalyzerService.NameToSlug("Random Body Phrase"), selectedSlugs);
        var randomSlug = NicheAnalyzerService.NameToSlug("Random Body Phrase");
        Assert.True(fused.ExclusionReasons.TryGetValue(randomSlug, out var reason));
        Assert.Contains("corroboration", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PillarDemandEnricher_ApplySerpDemotions_KeepsSchemaWithoutFootprint()
    {
        var pillars = new List<DiscoveredPillar>
        {
            new() { Name = "Managed IT", Slug = "managed-it", Source = "schema" },
            new() { Name = "Random Topic", Slug = "random-topic", Source = "page" },
        };
        var serp = new List<PillarSerpEnrichment>
        {
            new("managed-it", false, 0, false, null, [], "test"),
            new("random-topic", false, 0, false, null, [], "test"),
        };

        var kept = PillarDemandEnricher.ApplySerpDemotions(pillars, serp, out var demoted);

        Assert.Single(kept);
        Assert.Equal("managed-it", kept[0].Slug);
        Assert.Single(demoted);
        Assert.Equal("random-topic", demoted[0]);
    }

    [Fact]
    public void PillarDemandEnricher_BuildCompetitors_ExcludesOwnDomain()
    {
        var profileId = Guid.NewGuid();
        var serp = new List<PillarSerpEnrichment>
        {
            new(
                "managed-it",
                true,
                10,
                false,
                null,
                ["competitor.com", "geekatyourspot.com", "www.geekatyourspot.com"],
                "test"),
        };

        var competitors = PillarDemandEnricher.BuildCompetitors(profileId, "geekatyourspot.com", serp);

        Assert.Single(competitors);
        Assert.Equal("competitor.com", competitors[0].Domain);
    }

    [Fact]
    public void PillarDemandEnricher_PickBestKeywordMatch_PrefersExactThenVolume()
    {
        var suggestions = new List<KeywordResult>
        {
            new()
            {
                Keyword = "ai consulting services",
                SearchVolume = 900,
                KeywordDifficulty = 40,
                CpcUsd = 1,
                Competition = "medium",
            },
            new()
            {
                Keyword = "ai consulting",
                SearchVolume = 1200,
                KeywordDifficulty = 35,
                CpcUsd = 1,
                Competition = "medium",
            },
        };

        var match = PillarDemandEnricher.PickBestKeywordMatch("ai consulting", suggestions);

        Assert.NotNull(match);
        Assert.Equal("ai consulting", match!.Keyword);
    }

    private static class FixtureTopics
    {
        internal static IEnumerable<string> TwelveDistinct =>
        [
            "Alpha Platform Engineering",
            "Beta Workflow Automation",
            "Gamma Conversational Agents",
            "Delta Metrics Programs",
            "Epsilon Cloud Programs",
            "Zeta Compliance Programs",
            "Eta Web Engineering",
            "Theta Objectives Review",
            "Iota Data Readiness",
            "Kappa Vendor Selection",
            "Lambda Rollout Planning",
            "Mu Managed Operations",
        ];

        internal static IEnumerable<string> GeekAtYourSpotSchema =>
        [
            "Artificial Intelligence",
            "Process Automation",
            "AI Chatbots",
            "Data Analytics",
            "AI Strategy Consulting",
            "Security and Compliance",
            "Web Application Development",
            "AI Consulting",
            "Business Objectives Analysis",
            "Data Quality Assessment",
            "AI Technology Selection",
            "AI Implementation Strategy",
        ];
    }

    private sealed class StubHttpClientFactory(StubHttpHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}

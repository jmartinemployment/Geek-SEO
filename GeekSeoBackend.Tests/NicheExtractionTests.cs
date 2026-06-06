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
    public void TopicFusionEngine_SelectsElevenGeekAtYourSpotTopics_AfterSimilarityMerge()
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

        Assert.Equal(11, result.Selected.Count);
        Assert.Equal(TopicFusionEngine.FusionVersion, fused.FusionVersion);
        Assert.True(fused.ExclusionReasons.ContainsKey(
            NicheAnalyzerService.NameToSlug("AI Consulting")));
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

        var (phrases, listItems) = PageContentExtractor.ExtractFromHtml(html);

        Assert.Equal(2, listItems);
        Assert.Contains("Managed IT Support", phrases);
        Assert.Contains("Cloud Migration Planning", phrases);
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
            []);
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
            new PageContentData([], 0));

        Assert.Single(pool);
        Assert.Equal(0.45m, pool[0].Confidence);
        Assert.Equal(2, pool[0].Evidence.Count);
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

using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;

namespace GeekSeoBackend.Tests;

public sealed class NichePersistenceTests
{
    [Fact]
    public void ScanFingerprint_IsStableForSameInputs()
    {
        var schema = new SchemaOrgData([], [], [], null, null, [], [], [], false);
        var sitemap = new SitemapData([], 0, []);
        var nav = new NavMenuData([], "test");

        var a = NicheScanFingerprint.Compute("example.com", "sul-2.0", schema, sitemap, nav);
        var b = NicheScanFingerprint.Compute("example.com", "sul-2.0", schema, sitemap, nav);

        Assert.Equal(a.Fingerprint, b.Fingerprint);
        Assert.Equal(1m, a.ChangeScore);
    }

    [Fact]
    public void TopicCandidateMapper_MapsSelectedAndExcluded()
    {
        var profileId = Guid.NewGuid();
        var fused = new SiteTopicProfile
        {
            AllCandidates =
            [
                new TopicCandidate
                {
                    Name = "Roof Repair",
                    Slug = "roof-repair",
                    Evidence = [new TopicEvidence { Source = "schema", Weight = 1m }],
                    Confidence = 0.9m,
                    ContentDepthScore = 0.5m,
                    InternalLinkCount = 2,
                },
                new TopicCandidate
                {
                    Name = "Other",
                    Slug = "other",
                    Evidence = [],
                    Confidence = 0.2m,
                },
            ],
            SelectedPillars =
            [
                new TopicCandidate
                {
                    Name = "Roof Repair",
                    Slug = "roof-repair",
                    Evidence = [],
                    Confidence = 0.9m,
                },
            ],
            ExcludedCandidates = [],
            ExclusionReasons = new Dictionary<string, string> { ["other"] = "low confidence" },
            SulVersion = "sul-2.0",
            SignalSourcesPresent = ["schema"],
        };

        var rows = NicheTopicCandidateMapper.FromSiteTopicProfile(profileId, fused, includeEvidence: true);

        Assert.Equal(2, rows.Count);
        Assert.True(rows[0].IsSelected);
        Assert.False(rows[1].IsSelected);
        Assert.Equal("low confidence", rows[1].ExclusionReason);
        Assert.Equal(profileId, rows[0].NicheProfileId);
    }

    [Fact]
    public void CompetitorBulkInsertMapper_PreservesScope()
    {
        var dto = new NicheCompetitorBulkInsert(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "delray-it.com",
            2,
            30m,
            2,
            "strong",
            "local");

        var entity = NicheBulkInsertMapper.ToEntity(dto);

        Assert.Equal("local", entity.Scope);
    }
}

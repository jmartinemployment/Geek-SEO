using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

public sealed class PillarSelectorContentBackedTests
{
    private static TopicCandidate Candidate(string name, string slug, string source, decimal confidence) => new()
    {
        Name = name,
        Slug = slug,
        Evidence = [new TopicEvidence { Source = source, Weight = confidence }],
        Confidence = confidence,
    };

    [Fact]
    public void Content_backed_heading_candidate_is_selected_despite_confidence_below_threshold()
    {
        Assert.True(TopicEvidenceWeights.Heading < TopicEvidenceWeights.MinPillarConfidence);

        var candidate = Candidate(
            "AI Content Creation Workflow",
            "ai-content-creation-workflow",
            "heading_content_backed",
            TopicEvidenceWeights.Heading);

        var selector = new PillarSelector(new PillarValidator());
        var profile = selector.Select([candidate], []);

        Assert.Contains(profile.SelectedPillars, p => p.Slug == candidate.Slug);
        Assert.DoesNotContain(candidate.Slug, profile.ExclusionReasons.Keys);
    }

    [Fact]
    public void Bare_heading_candidate_below_threshold_is_still_excluded()
    {
        var candidate = Candidate(
            "Marketing",
            "marketing",
            "heading",
            TopicEvidenceWeights.Heading);

        var selector = new PillarSelector(new PillarValidator());
        var profile = selector.Select([candidate], []);

        Assert.DoesNotContain(profile.SelectedPillars, p => p.Slug == candidate.Slug);
        Assert.True(profile.ExclusionReasons.ContainsKey(candidate.Slug));
    }
}

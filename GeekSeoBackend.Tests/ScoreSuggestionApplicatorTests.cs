using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ScoreSuggestionApplicatorTests
{
    [Fact]
    public void ProposeTitle_includes_keyword_and_trims_length()
    {
        var proposed = ScoreSuggestionApplicator.ProposeTitle(
            "Complete guide for small businesses",
            "AI consulting",
            55);

        Assert.Contains("AI consulting", proposed, StringComparison.OrdinalIgnoreCase);
        Assert.True(proposed.Length <= 55);
    }

    [Fact]
    public void TryApplyDeterministic_title_keyword_updates_h1()
    {
        var html = "<h1>Complete guide</h1><p>Body</p>";
        var patched = ScoreSuggestionApplicator.TryApplyDeterministic(
            "title_keyword",
            html,
            "AI consulting",
            55,
            "Body",
            []);

        Assert.NotNull(patched);
        Assert.Contains("AI consulting", patched!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<h1>", patched!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryApplyDeterministic_geo_citations_appends_sources()
    {
        var html = "<h1>Guide</h1><p>Body</p>";
        var organic = new List<SerpOrganicResult>
        {
            new() { Position = 1, Url = "https://example.org/study", Title = "Study", Snippet = "x", Domain = "example.org" },
            new() { Position = 2, Url = "https://gov.example/policy", Title = "Policy", Snippet = "y", Domain = "gov.example" },
        };

        var patched = ScoreSuggestionApplicator.TryApplyDeterministic(
            "geo_citations",
            html,
            "local seo",
            55,
            "Body",
            organic);

        Assert.NotNull(patched);
        Assert.Contains("<h2>Sources</h2>", patched!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://example.org/study", patched!, StringComparison.Ordinal);
    }
}

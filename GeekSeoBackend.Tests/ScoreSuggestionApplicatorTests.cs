using System.Text.RegularExpressions;
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

    [Fact]
    public void HasUsableSerpCitationPicks_returns_false_when_no_organic_results()
    {
        var html = "<h1>Guide</h1><p>Body</p>";
        Assert.False(ScoreSuggestionApplicator.HasUsableSerpCitationPicks(html, []));
    }

    [Fact]
    public void TryAppendSourcesFromDiscovered_appends_list_with_ai_sources()
    {
        var html = "<h1>Guide</h1><p>Body</p>";
        var sources = new List<DiscoveredSource>
        {
            new() { Url = "https://www.cdc.gov/example", Title = "CDC", AnchorText = "CDC guidance" },
            new() { Url = "https://www.nih.gov/example", Title = "NIH", AnchorText = "NIH research" },
        };

        var patched = ScoreSuggestionApplicator.TryAppendSourcesFromDiscovered(html, sources);

        Assert.NotNull(patched);
        Assert.Contains("<h2>Sources</h2>", patched!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://www.cdc.gov/example", patched!, StringComparison.Ordinal);
    }

    [Fact]
    public void TryApplyDeterministic_serp_featured_snippet_inserts_paragraph_after_first_h2()
    {
        var html = "<h1>Guide</h1><h2>Overview</h2><p>Short intro.</p>";
        var patched = ScoreSuggestionApplicator.TryApplyDeterministic(
            "serp_featured_snippet",
            html,
            "widget repair",
            55,
            "Widget repair helps small shops fix broken devices quickly and affordably for local customers.",
            []);

        Assert.NotNull(patched);
        Assert.True(
            Regex.IsMatch(
                patched!,
                @"<h2>Overview</h2>\s*<p>.*widget repair.*</p>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline));
        var answer = ScoreSuggestionApplicator.ProposeFeaturedSnippetDirectAnswer(
            "widget repair",
            "Widget repair helps small shops fix broken devices quickly and affordably for local customers.");
        Assert.InRange(ScoreSuggestionApplicatorTestsWordCount(answer), 40, 60);
    }

    [Fact]
    public void HasDirectAnswerAfterFirstH2_returns_true_when_snippet_sized_paragraph_follows_h2()
    {
        var words = string.Join(' ', Enumerable.Repeat("word", 45));
        var html = $"<h2>Overview</h2><p>{words}</p>";
        Assert.True(ScoreSuggestionApplicator.HasDirectAnswerAfterFirstH2(html));
    }

    private static int ScoreSuggestionApplicatorTestsWordCount(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    [Fact]
    public void TryApplyDeterministic_geo_citations_uses_domain_when_url_missing()
    {
        var html = "<h1>Guide</h1><p>Body</p>";
        var organic = new List<SerpOrganicResult>
        {
            new() { Position = 1, Url = "", Title = "Study", Snippet = "x", Domain = "example.org" },
            new() { Position = 2, Url = "", Title = "Policy", Snippet = "y", Domain = "gov.example" },
        };

        var patched = ScoreSuggestionApplicator.TryApplyDeterministic(
            "geo_citations",
            html,
            "local seo",
            55,
            "Body",
            organic);

        Assert.NotNull(patched);
        Assert.Contains("https://example.org", patched!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HasClosingFaqSection_ignores_h3_outside_faq_section()
    {
        var html = """
            <h2>Implementation phases</h2>
            <h3>Phase one</h3><p>Details</p>
            <h3>Phase two</h3><p>Details</p>
            <h3>Phase three</h3><p>Details</p>
            <h3>Phase four</h3><p>Details</p>
            <h3>Phase five</h3><p>Details</p>
            """;

        Assert.False(ArticleClosingFaqEnricher.HasClosingFaqSection(html));
    }

    [Fact]
    public void TryApplyDeterministic_geo_structure_appends_faq_when_questions_exist_without_closing_section()
    {
        var html = "<h2>Section</h2><h3>Why does this matter?</h3><p>Body</p>";

        var patched = ScoreSuggestionApplicator.TryApplyDeterministic(
            "geo_structure",
            html,
            "local seo",
            55,
            "Body",
            []);

        Assert.NotNull(patched);
        Assert.Contains("Frequently asked questions", patched!, StringComparison.OrdinalIgnoreCase);
    }
}

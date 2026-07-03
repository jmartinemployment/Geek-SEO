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
    public void TryAppendSchemaScripts_appends_tech_article_json_ld()
    {
        var html = "<h1>Customer journey mapping</h1><p>Body copy.</p>";
        var scripts = new[]
        {
            "<script type=\"application/ld+json\">{\"@context\":\"https://schema.org\",\"@type\":\"TechArticle\",\"headline\":\"Customer journey mapping\"}</script>",
        };

        var patched = ScoreSuggestionApplicator.TryAppendSchemaScripts(html, scripts);

        Assert.NotNull(patched);
        Assert.Contains("TechArticle", patched!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("application/ld+json", patched!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryApplyDeterministic_geo_citations_inserts_inline_authoritative_links()
    {
        var html = "<h1>Guide</h1><p>Body</p>";
        var organic = new List<SerpOrganicResult>
        {
            new() { Position = 1, Url = "https://www.cdc.gov/example", Title = "CDC", Snippet = "x", Domain = "cdc.gov" },
            new() { Position = 2, Url = "https://www.ed.gov/policy", Title = "ED", Snippet = "y", Domain = "ed.gov" },
            new() { Position = 3, Url = "https://competitor.com/ai-customer-journey-guide", Title = "AI Customer Journey Guide", Snippet = "z", Domain = "competitor.com" },
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
        Assert.Contains("https://www.cdc.gov/example", patched!, StringComparison.Ordinal);
        Assert.Contains("https://www.ed.gov/policy", patched!, StringComparison.Ordinal);
        Assert.DoesNotContain("competitor.com", patched!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("According to", patched!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HasUsableSerpCitationPicks_returns_false_when_no_organic_results()
    {
        var html = "<h1>Guide</h1><p>Body</p>";
        Assert.False(ScoreSuggestionApplicator.HasUsableSerpCitationPicks(html, []));
    }

    [Fact]
    public void TryAppendSourcesFromDiscovered_inserts_section_even_when_url_already_linked_inline()
    {
        const string wiki = "https://en.wikipedia.org/wiki/Prospecting";
        var html = $"""
            <h1>Guide</h1>
            <p>See <a href="{wiki}">Wikipedia</a> for background.</p>
            <h2>Frequently asked questions</h2>
            <h3>Question?</h3><p>Answer</p>
            """;
        var sources = new List<DiscoveredSource>
        {
            new() { Url = wiki, Title = "Prospecting", AnchorText = "Prospecting — Wikipedia" },
        };

        var patched = ScoreSuggestionApplicator.TryAppendSourcesFromDiscovered(html, sources);

        Assert.NotNull(patched);
        Assert.Contains("<h2>Sources</h2>", patched!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(wiki, patched!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureArticleH1_prepends_heading_when_missing()
    {
        var html = "<h2>Section</h2><p>Body</p>";
        var patched = ScoreSuggestionApplicator.EnsureArticleH1(html, "AI for Prospecting & Lead Intelligence");
        Assert.StartsWith("<h1>", patched, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AI for Prospecting", patched, StringComparison.Ordinal);
    }

    [Fact]
    public void TryAppendSourcesFromDiscovered_inserts_inline_links()
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
    public void TryInsertResearchCitation_inserts_single_inline_link()
    {
        var html = "<h1>Guide</h1><p>Body</p>";
        var patched = ScoreSuggestionApplicator.TryInsertResearchCitation(
            html,
            "https://www.nist.gov/ai",
            "NIST AI guidance");

        Assert.NotNull(patched);
        Assert.Contains("https://www.nist.gov/ai", patched!, StringComparison.Ordinal);
        Assert.Contains("NIST AI guidance", patched!, StringComparison.Ordinal);
    }

    [Fact]
    public void TryInsertResearchCitation_returns_null_when_already_linked()
    {
        var html = "<p>See <a href=\"https://www.nist.gov/ai\">NIST</a>.</p>";
        var patched = ScoreSuggestionApplicator.TryInsertResearchCitation(
            html,
            "https://www.nist.gov/ai",
            "NIST AI guidance");

        Assert.Null(patched);
    }

    [Fact]
    public void TryApplyDeterministic_meta_description_replaces_weak_existing_meta()
    {
        const string keyword = "local seo checklist";
        var html =
            $"<meta name=\"description\" content=\"Short blurb without the phrase.\"><h1>Guide</h1><p>{keyword} helps small businesses improve visibility with a practical step-by-step checklist for maps, reviews, and on-page basics that drive local leads.</p>";
        var plain = $"{keyword} helps small businesses improve visibility with a practical step-by-step checklist for maps, reviews, and on-page basics that drive local leads.";

        var patched = ScoreSuggestionApplicator.TryApplyDeterministic(
            "meta_description",
            html,
            keyword,
            55,
            plain,
            []);

        Assert.NotNull(patched);
        Assert.Contains("name=\"description\"", patched!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(keyword, patched!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Short blurb without the phrase.", patched!, StringComparison.Ordinal);
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
    public void TryApplyDeterministic_serp_ai_overview_inserts_definition_after_h1()
    {
        var html = "<h1>Guide</h1><h2>Overview</h2><p>Short intro.</p>";
        var patched = ScoreSuggestionApplicator.TryApplyDeterministic(
            "serp_ai_overview",
            html,
            "customer journey",
            55,
            "A customer journey maps every touchpoint from awareness through purchase and retention.",
            []);

        Assert.NotNull(patched);
        Assert.True(
            Regex.IsMatch(
                patched!,
                @"<h1>Guide</h1>\s*<p>.*customer journey.*</p>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline));
        var definition = ScoreSuggestionApplicator.ProposeAiOverviewDefinition(
            "customer journey",
            "A customer journey maps every touchpoint from awareness through purchase and retention.");
        Assert.InRange(ScoreSuggestionApplicatorTestsWordCount(definition), 20, 35);
    }

    [Fact]
    public void ProposeFeaturedSnippetDirectAnswer_ignores_unusable_serp_capture()
    {
        const string junk =
            "Can't generate an AI overview right now. AI Overview (function(){window.sn={";
        const string plain =
            "Market intelligence combines data collection and analysis to guide strategic business decisions.";

        var answer = ScoreSuggestionApplicator.ProposeFeaturedSnippetDirectAnswer(
            "market intelligence",
            plain,
            junk);

        Assert.DoesNotContain("function", answer, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AI overview", answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("market intelligence", answer, StringComparison.OrdinalIgnoreCase);
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
            new() { Position = 1, Url = "", Title = "CDC", Snippet = "x", Domain = "cdc.gov" },
            new() { Position = 2, Url = "", Title = "ED", Snippet = "y", Domain = "ed.gov" },
        };

        var patched = ScoreSuggestionApplicator.TryApplyDeterministic(
            "geo_citations",
            html,
            "local seo",
            55,
            "Body",
            organic);

        Assert.NotNull(patched);
        Assert.Contains("https://cdc.gov", patched!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryAppendSourcesFromDiscovered_inserts_before_faq_section()
    {
        var html = """
            <h2>Body</h2><p>Content</p>
            <h2>Frequently asked questions</h2>
            <h3>Question?</h3><p>Answer</p>
            """;
        var sources = new List<DiscoveredSource>
        {
            new() { Url = "https://www.cdc.gov/example", Title = "CDC", AnchorText = "CDC guidance" },
        };

        var patched = ScoreSuggestionApplicator.TryAppendSourcesFromDiscovered(html, sources);

        Assert.NotNull(patched);
        var sourcesIndex = patched!.IndexOf("<h2>Sources</h2>", StringComparison.OrdinalIgnoreCase);
        var faqIndex = patched.IndexOf("Frequently asked questions", StringComparison.OrdinalIgnoreCase);
        Assert.True(sourcesIndex >= 0);
        Assert.True(faqIndex > sourcesIndex);
    }

    [Fact]
    public void HasUsableSerpCitationPicks_ignores_competitor_organic_results()
    {
        var html = "<h1>Guide</h1><p>Body</p>";
        var organic = new List<SerpOrganicResult>
        {
            new() { Position = 1, Url = "https://competitor.com/guide", Title = "Guide", Snippet = "x", Domain = "competitor.com" },
        };

        Assert.False(ScoreSuggestionApplicator.HasUsableSerpCitationPicks(html, organic));
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

    [Fact]
    public void TryApplyEeat_first_hand_experience_inserts_after_first_h2()
    {
        var html = "<h1>Title</h1><h2>Intro</h2><p>Body</p>";
        var context = new EeatApplyContext { Keyword = "customer journey" };

        var patched = ScoreSuggestionApplicator.TryApplyEeat("eeat_first_hand_experience", html, context);

        Assert.NotNull(patched);
        Assert.Contains("In our experience", patched!, StringComparison.OrdinalIgnoreCase);
        Assert.True(patched!.IndexOf("In our experience", StringComparison.OrdinalIgnoreCase)
            > patched.IndexOf("<h2", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TryApplyEeat_author_bio_inserts_before_faq()
    {
        var html = """
            <h2>Section</h2><p>Body</p>
            <h2>Frequently asked questions</h2><h3>Q?</h3><p>A.</p>
            """;
        var context = new EeatApplyContext
        {
            Keyword = "ai",
            OrganizationName = "Geek At Your Spot",
            BusinessSummary = "AI consulting for SMBs.",
        };

        var patched = ScoreSuggestionApplicator.TryApplyEeat("eeat_author_bio", html, context);

        Assert.NotNull(patched);
        Assert.Contains("About the author", patched!, StringComparison.OrdinalIgnoreCase);
        Assert.True(patched!.IndexOf("About the author", StringComparison.OrdinalIgnoreCase)
            < patched.IndexOf("Frequently asked questions", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TryApplyEeat_freshness_signal_inserts_after_h1()
    {
        var html = "<h1>Title</h1><h2>Section</h2><p>Body</p>";
        var context = new EeatApplyContext { Keyword = "test" };

        var patched = ScoreSuggestionApplicator.TryApplyEeat("eeat_freshness_signal", html, context);

        Assert.NotNull(patched);
        Assert.Contains("Last updated:", patched!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryApplyEeat_original_media_uses_featured_image_when_present()
    {
        var html = "<h1>Title</h1><p>Body</p>";
        var context = new EeatApplyContext
        {
            Keyword = "test",
            FeaturedImageUrl = "https://cdn.example.com/hero.jpg",
        };

        var patched = ScoreSuggestionApplicator.TryApplyEeat("eeat_original_media", html, context);

        Assert.NotNull(patched);
        Assert.Contains("hero.jpg", patched!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureArticleSchema_appends_scripts_when_missing()
    {
        const string html = "<h1>Guide</h1><p>Body</p>";
        var scripts = new[]
        {
            "<script type=\"application/ld+json\">{\"@context\":\"https://schema.org\",\"@type\":\"TechArticle\",\"headline\":\"Guide\"}</script>",
        };

        var patched = ContentAutoEnricher.EnsureArticleSchema(html, scripts, out var changed);

        Assert.True(changed);
        Assert.Contains("application/ld+json", patched, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureArticleSchema_is_idempotent_when_schema_present()
    {
        const string html =
            "<h1>Guide</h1><script type=\"application/ld+json\">{\"@type\":\"TechArticle\"}</script>";
        var scripts = new[]
        {
            "<script type=\"application/ld+json\">{\"@type\":\"TechArticle\"}</script>",
        };

        var patched = ContentAutoEnricher.EnsureArticleSchema(html, scripts, out var changed);

        Assert.False(changed);
        Assert.Equal(html, patched);
    }
}

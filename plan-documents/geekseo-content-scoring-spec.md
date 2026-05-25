# Geek SEO — Content Scoring Engine Specification

**Date:** May 2026
**Master plan:** [`GEEKSEO-PLAN-V2.md`](GEEKSEO-PLAN-V2.md) — features #21 (dual SEO+GEO scores), #22 (E-E-A-T advisories)
**Architecture:** Scoring in **GeekSeoBackend** (`Geek-SEO/GeekSeoBackend/`). Persistence via Jeff’s data layer (GeekRepository; not SEO product on GeekAPI). Browser calls GeekSeoBackend only — see [`ARCHITECTURE.md`](ARCHITECTURE.md).
**Scope:** Transparent **SEO** score (6 components, 0–100) + separate **GEO** score (5 dimensions, 0–100 each) for the real-time editor.

---

## 1. Overview

### What Content Scoring Is

Content scoring is the practice of analyzing a piece of written content against the top-ranking pages for a target keyword and producing a numeric grade that reflects how well-optimized the content is. The score serves as a proxy for "if you write content that matches what already ranks, Google is more likely to rank your content too."

Every major SEO content tool does this. Surfer SEO, Clearscope, NeuronWriter, Frase, MarketMuse — all of them fetch the SERP, analyze the top pages, and produce a score. The methodology is called correlation SEO: identify what the top-ranking pages have in common and tell the user to include those signals.

### Why It Matters

Writers who optimize against a content score consistently produce content that ranks faster and higher than writers who don't. This is not because the score is a perfect model of Google's algorithm — it isn't. It works because:

1. Top-ranking pages genuinely do share structural and topical patterns
2. Coverage of the right semantic terms signals topical authority to NLP-based ranking systems
3. Word count, heading density, and readability correlate with user engagement, which is itself a ranking signal
4. The score is a forcing function that gets writers to be more thorough

### How Geek SEO's Approach Differs from Surfer SEO

**Surfer SEO** produces a 0-100 score from a proprietary black-box formula. Users see the number and the keyword recommendations but never know exactly why they scored 67 instead of 75. The formula weighs dozens of factors including True Density (placement-aware keyword frequency), NLP entities, heading structure, and content length — but the exact weighting is undisclosed. Users frequently report that following suggestions doesn't produce the expected score increase, which erodes trust.

**Geek SEO** uses the same correlation-SEO foundation but makes the formula completely transparent. The score is divided into exactly 6 components with known maximum point values. Every point is accounted for. Users see: "my term coverage is 18/35 because I'm missing these 7 terms" and "my heading structure is 8/15 because I need 2 more H2 headings." Users can calculate their potential score before making a change.

This transparency has two product benefits:
- Trust: users who understand the system stay and keep using it
- Action: specific, quantified suggestions drive higher engagement than vague "improve your content" nudges

**Geek SEO also adds:**
- Native local SERP targeting via the `location` parameter — every SERP fetch is location-aware from day one. Surfer buries location targeting in higher plan tiers.
- An IAIProvider-backed term extraction layer — semantic terms come from Claude analyzing each competitor page, not just TF-IDF statistics. The AI layer catches conceptual topics that pure frequency analysis misses.
- A swappable AI provider — if Claude is unavailable or the user wants to use a different model, the interface is swapped at the DI layer without touching the scoring algorithm.

---

## 2. SERP Data Collection

### How ISerpProvider.GetSerpResultsAsync Works

The scoring engine needs to know what the top-ranking pages are before it can benchmark the user's content. `ISerpProvider.GetSerpResultsAsync` is the entry point.

```csharp
// Called by ContentScoringService.GetOrFetchBenchmarksAsync
var request = new SerpRequest
{
    Keyword = "plumber in Boca Raton",
    Location = "Boca Raton, Florida",
    LanguageCode = "en",
    CountryCode = "US",
    ResultCount = 10,
    Device = "desktop"
};

var result = await _serpProvider.GetSerpResultsAsync(request, ct);
```

The primary implementation (`DataForSEOSerpProvider`) calls DataForSEO's Live SERP endpoint. This returns real Google results for the target location, not a national average. The `location` string maps to DataForSEO's location parameter (they support 50,000+ locations). For `"Boca Raton, Florida"` the results are the same pages a user in Boca Raton would see on Google.

### Data Fields Returned Per Result

`SerpResult` carries:

| Field | Type | Source |
|---|---|---|
| `OrganicResults` | `IReadOnlyList<SerpOrganicResult>` | Top 10 organic positions |
| `PeopleAlsoAsk` | `IReadOnlyList<PeopleAlsoAskResult>` | PAA questions + answers |
| `RelatedSearches` | `IReadOnlyList<string>` | Related search queries |
| `FeaturedSnippetText` | `string?` | Featured snippet content if present |
| `Features` | `SerpFeatures` | `has_featured_snippet`, `has_people_also_ask`, `has_local_pack`, `has_image_pack`, etc. — persisted in `seo_serp_results.serp_features` |
| `FetchedAt` | `DateTimeOffset` | When the live pull happened |

Each `SerpOrganicResult` carries: `Position`, `Url`, `Title`, `Snippet`, `Domain`.

The PAA questions feed directly into the Content Brief generator. The related searches are used as supplementary term suggestions. The featured snippet text, when present, indicates that a question-answer structured section in the user's content may help capture that SERP feature.

### Cache Key and TTL

Cache key: composite of `(keyword, location, languageCode)` stored as a unique constraint in `seo_serp_results`.

TTL: 24 hours from `fetched_at`. The `expires_at` column is set to `fetched_at + INTERVAL '24 hours'`.

Cache strategy:
1. GeekSeoBackend loads `seo_serp_results` for `(keyword, location, languageCode)`
2. If `expires_at > NOW()`, use cached row
3. On miss: GeekSeoBackend fetches SERP + crawls competitors, upserts `seo_serp_results` + `seo_competitor_pages`

This means: when multiple users research the same keyword in the same location within 24 hours, only the first request hits DataForSEO. All subsequent users in that window use the cached result. At 100 users doing 10 keyword lookups per day, aggressive cache hit rates can reduce DataForSEO SERP costs by 60-80%.

### Cache Invalidation

SERP results are not force-expired mid-session. If a user opens the content editor and starts scoring, the benchmark is fetched once and held for the duration of that session. Only when the user explicitly changes the target keyword (via `KeywordChanged` SignalR message) are new benchmarks fetched.

The 24-hour TTL is intentionally longer than needed for daily accuracy — SERPs for most keywords don't change hourly or even daily. For volatile news-adjacent keywords, users can force a refresh via the UI (a "Refresh SERP data" button in the competitor panel calls `DELETE /api/seo/serp-cache?keyword=...&location=...` followed by a fresh fetch).

---

## 3. Competitor Page Crawling

### ICrawlerProvider.CrawlPageAsync Specification

After SERP results are fetched, the scoring engine needs the full text of the top-ranking pages to extract terms and calculate benchmarks. `ICrawlerProvider.CrawlPageAsync(url)` fetches a single competitor page and returns a structured `PageContent` record.

```csharp
// PlaywrightCrawlerProvider.cs — GeekSeoBackend only; singleton IBrowser pool (see ARCHITECTURE.md)
public async Task<Result<PageContent>> CrawlPageAsync(string url, CancellationToken ct = default)
{
    var isAllowed = await IsAllowedByRobotsTxtAsync(url, ct);
    if (!isAllowed)
        return Result<PageContent>.Failure($"URL {url} disallowed by robots.txt");

    await _crawlSemaphore.WaitAsync(ct);
    try
    {
        await using var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            { "User-Agent", "GeekSEO-Bot/1.0 (content analysis; contact: jeff@geekatyourspot.com)" }
        });

        var response = await page.GotoAsync(url, new() { Timeout = 15000, WaitUntil = WaitUntilState.DOMContentLoaded });
        var httpStatus = response?.Status ?? 0;
        if (httpStatus >= 400)
            return Result<PageContent>.Failure($"HTTP {httpStatus} for {url}");

        var metaTitle = await page.TitleAsync();
        var headings = await ExtractHeadingsAsync(page);
        var bodyText = await page.EvalOnSelectorAsync<string>("body", "el => el.innerText");
        var wordCount = CountWords(bodyText);
        var structuredDataTypes = await ExtractStructuredDataTypesAsync(page);

        return Result<PageContent>.Success(new PageContent
        {
            Url = url,
            FullText = bodyText,
            MetaTitle = metaTitle,
            WordCount = wordCount,
            HttpStatusCode = httpStatus,
            Headings = headings,
            HasStructuredData = structuredDataTypes.Count > 0,
            StructuredDataTypes = structuredDataTypes,
            CrawledAt = DateTimeOffset.UtcNow
        });
    }
    finally
    {
        _crawlSemaphore.Release();
    }
}
```

**Pool rules:** One `IBrowser` per **GeekSeoBackend** process; max **2** concurrent `CrawlPageAsync` via `SemaphoreSlim`; 30s navigation timeout. Never `Playwright.CreateAsync()` per request.

### What Is Extracted

| Field | Extraction Method | Used For |
|---|---|---|
| `FullText` | `document.body.innerText` (Playwright) | Term extraction, word count |
| `MetaTitle` | `page.TitleAsync()` | Title pattern analysis in briefs |
| `MetaDescription` | `meta[name="description"]` content | Meta pattern analysis |
| `CanonicalUrl` | `link[rel="canonical"]` href | Deduplication (skip canonical ≠ url) |
| `WordCount` | Split on whitespace after innerText extraction | Word count benchmark |
| `HttpStatusCode` | Response status from Playwright goto | Skip 4xx/5xx pages |
| `Headings` | All h1-h6 elements, level + text | Heading count benchmarks |
| `InternalLinks` | `<a href="...">` where same domain | Internal link count |
| `ExternalLinks` | `<a href="...">` where different domain | External link count |
| `Images.AltText` | `<img alt="...">` attributes | Alt text coverage analysis |
| `HasStructuredData` | JSON-LD `<script type="application/ld+json">` present | Schema markup benchmark |
| `StructuredDataTypes` | `@type` values from JSON-LD blocks | Schema type list |

### Competitor Page Cache

Cache key: `(serp_result_id, url)` — unique constraint in `seo_competitor_pages`.

TTL: 72 hours. Competitor pages change less frequently than SERP rankings themselves.

Cache strategy:
1. After SERP results are fetched (or pulled from cache), get all `SerpOrganicResult.Url` values
2. GeekSeoBackend loads `seo_competitor_pages` for `serp_result_id`
3. On miss: `CrawlPage` per URL inside GeekSeoBackend
4. GeekSeoBackend upserts `seo_competitor_pages`
5. Proceed with benchmarking using the full set of 10 competitor pages

If fewer than 3 competitor pages were successfully crawled (due to crawl failures, robots.txt blocks, or JS-heavy pages that Playwright cannot extract text from), the scoring engine logs a warning and proceeds with whatever pages it has. The score is still calculated — it is annotated with a `BenchmarkQuality` field indicating `low_sample_count` so the frontend can display a notice to the user.

### Robots.txt Compliance

The `IsAllowedByRobotsTxtAsync` method fetches `{scheme}://{host}/robots.txt`, parses the file, and checks if the URL path is covered by a `Disallow` rule for `GeekSEO-Bot` or `*`. If robots.txt cannot be fetched (404, timeout), crawling is allowed (open-web assumption). If robots.txt explicitly disallows the path, crawling is skipped and the page is excluded from the benchmark set.

---

## 4. NLP Term Extraction

Term extraction is the process of identifying which words and phrases matter most for a given keyword, based on their prevalence and importance across the top-ranking competitor pages. Geek SEO uses two layers: statistical frequency analysis and AI-based semantic extraction.

### Layer 1: Statistical Frequency Analysis

For each competitor page's `FullText`:

**Tokenization:**
```csharp
// NlpExtractor.cs (in GeekSeoBackend/Services/Scoring/Nlp/)
public static IReadOnlyList<string> Tokenize(string text)
{
    // 1. Lowercase
    // 2. Remove punctuation except hyphens within words (e.g., "step-by-step")
    // 3. Split on whitespace
    // 4. Remove stop words (English stop word list, 400+ terms)
    // 5. Minimum token length: 3 characters
    return tokens;
}
```

Stop words to remove (core list — not exhaustive): "the", "a", "an", "is", "it", "in", "on", "at", "to", "for", "of", "and", "or", "but", "with", "by", "from", "that", "this", "which", "who", "what", "how", "when", "where", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "can", "not", "no", "nor", "so", "yet", "both", "either", "neither", "each", "few", "more", "most", "other", "such", "than", "too", "very", "just", "also", "about", "after", "before", "between", "during", "without", "within", "through", "across", "behind", "beyond", "around", "among", "above", "below", "under".

**N-gram Extraction:**

After tokenization, extract unigrams, bigrams, and trigrams:

```csharp
public static IReadOnlyList<string> ExtractNgrams(IReadOnlyList<string> tokens, int n)
{
    // For n=1: return tokens as-is
    // For n=2: "content marketing tips" → ["content marketing", "marketing tips"]
    // For n=3: "content marketing tips" → ["content marketing tips"]
    // Only include ngrams where all component tokens pass the stop word filter
}
```

**Term Frequency Calculation:**

For each competitor page, produce a `Dictionary<string, int>` mapping each term to its occurrence count in `FullText`. Normalize: divide each count by the total word count of the page to get TF (term frequency, range 0-1). This prevents longer pages from artificially inflating their term scores.

### Layer 2: AI Semantic Extraction

Pure frequency misses concepts that are important but expressed in varied language. A competitor page about "plumbing services in Boca Raton" might discuss "emergency pipe repair," "water heater installation," and "licensed plumber" — these are important topics even if individual words aren't highly frequent.

For each competitor page, call `IAIProvider.CompleteWithSystemAsync` with:

```
System: You are an SEO content analyst. Extract the most important SEO-relevant terms and topics from competitor content. Return ONLY a JSON array of strings — no explanation, no markdown, no other text.

User: Extract the top 25 most important SEO terms, topics, and entities from this content. Include: primary keywords, secondary keywords, named entities (places, brands, certifications), service/product names, and important concepts. Do not include stop words, generic verbs, or filler phrases.

Content:
{competitor_page_text_first_3000_characters}
```

The response is parsed as `string[]`. Each element is added to the term list with a synthetic frequency boost of 2 (treating an AI-identified term as if it appeared twice in the frequency data). This ensures AI-identified important terms surface in the recommendations even if their raw frequency was low.

**Combining Statistical + AI Terms:**

```csharp
public static IReadOnlyDictionary<string, TermData> CombineTermSources(
    IReadOnlyDictionary<string, int> frequencyTerms,
    IReadOnlyList<string> aiTerms,
    int pageWordCount)
{
    var combined = new Dictionary<string, TermData>(StringComparer.OrdinalIgnoreCase);

    foreach (var (term, count) in frequencyTerms)
    {
        combined[term] = new TermData
        {
            Term = term,
            FrequencyCount = count,
            TfScore = (double)count / pageWordCount,
            AiIdentified = false
        };
    }

    foreach (var term in aiTerms)
    {
        if (combined.TryGetValue(term, out var existing))
        {
            combined[term] = existing with { AiIdentified = true };
        }
        else
        {
            combined[term] = new TermData
            {
                Term = term,
                FrequencyCount = 2,  // synthetic boost
                TfScore = 2.0 / pageWordCount,
                AiIdentified = true
            };
        }
    }

    return combined;
}
```

**Performance note:** The AI extraction call is made once per competitor page per SERP result, and the output is stored in `seo_competitor_pages.terms` (JSONB). On subsequent scoring requests for the same keyword, the cached `terms` field is read directly — no AI calls are made per-keystroke or per-score-update. The AI extraction happens only when a new competitor page is crawled.

---

## 5. Benchmark Calculation

Once all 10 competitor pages have been crawled and their terms extracted, the scoring engine calculates benchmarks. Benchmarks are the statistical targets that user content is measured against.

### Term Benchmarks

For each unique term across all 10 competitor pages:

```csharp
public sealed record TermBenchmark
{
    public required string Term { get; init; }
    public required int DocumentFrequency { get; init; }     // how many of 10 pages contain this term
    public required double MinFrequency { get; init; }       // lowest TF score among pages that contain it
    public required double MaxFrequency { get; init; }       // highest TF score
    public required double AvgFrequency { get; init; }       // mean TF score across ALL 10 pages (including 0)
    public required (double Min, double Max) RecommendedRange { get; init; }  // [avg * 0.8, avg * 1.2]
    public required double ImportanceScore { get; init; }    // (documentFrequency / 10) * avgFrequency
    public required bool AiIdentified { get; init; }         // true if any competitor's AI extraction flagged it
}
```

Calculation:

```csharp
public static IReadOnlyList<TermBenchmark> CalculateTermBenchmarks(
    IReadOnlyList<IReadOnlyDictionary<string, TermData>> allPageTerms,
    int totalPages = 10)
{
    var allTerms = allPageTerms
        .SelectMany(page => page.Keys)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    var benchmarks = new List<TermBenchmark>(allTerms.Count);

    foreach (var term in allTerms)
    {
        var pagesWithTerm = allPageTerms
            .Where(page => page.ContainsKey(term))
            .ToList();

        var documentFrequency = pagesWithTerm.Count;

        // If term appears in fewer than 3 of 10 pages, skip it
        // (not a consistent signal, likely noise or brand-specific)
        if (documentFrequency < 3) continue;

        var allTfScores = allPageTerms
            .Select(page => page.TryGetValue(term, out var data) ? data.TfScore : 0.0)
            .ToList();

        var avgFrequency = allTfScores.Average();
        var minFrequency = pagesWithTerm.Min(page => page[term].TfScore);
        var maxFrequency = pagesWithTerm.Max(page => page[term].TfScore);

        var importanceScore = ((double)documentFrequency / totalPages) * avgFrequency;

        // A term appearing in 8/10 pages with avg TF 0.005 has importance 0.004
        // A term in 3/10 pages with avg TF 0.001 has importance 0.0003 — much lower

        var recommendedMin = Math.Max(1, (int)(avgFrequency * totalPages * 0.8));
        var recommendedMax = (int)(avgFrequency * totalPages * 1.2) + 1;

        var isAiIdentified = allPageTerms.Any(page =>
            page.TryGetValue(term, out var data) && data.AiIdentified);

        benchmarks.Add(new TermBenchmark
        {
            Term = term,
            DocumentFrequency = documentFrequency,
            MinFrequency = minFrequency,
            MaxFrequency = maxFrequency,
            AvgFrequency = avgFrequency,
            RecommendedRange = (recommendedMin, recommendedMax),
            ImportanceScore = importanceScore,
            AiIdentified = isAiIdentified
        });
    }

    // Sort by importance score descending — top terms are most actionable
    return benchmarks.OrderByDescending(b => b.ImportanceScore).ToList();
}
```

**Important Terms filter:** After calculating benchmarks, the top 50 terms by `ImportanceScore` are designated "important terms." These are the terms the user is scored against. The full term list may contain 200+ terms, but only the top 50 drive the score. This prevents overwhelming users with hundreds of suggestions.

### Word Count Benchmark

```csharp
public static WordCountBenchmark CalculateWordCountBenchmark(IReadOnlyList<int> competitorWordCounts)
{
    var sorted = competitorWordCounts.Order().ToList();

    // Trim outliers: remove top and bottom 1 if 10 pages
    var trimmed = sorted.Skip(1).Take(sorted.Count - 2).ToList();

    var avg = (int)trimmed.Average();
    var min = (int)(avg * 0.85);
    var max = (int)(avg * 1.15);

    return new WordCountBenchmark
    {
        CompetitorAverage = avg,
        RecommendedMin = min,
        RecommendedMax = max,
        CompetitorCounts = competitorWordCounts
    };
}
```

### Heading Count Benchmarks

```csharp
public static HeadingBenchmarks CalculateHeadingBenchmarks(
    IReadOnlyList<IReadOnlyList<Heading>> allCompetitorHeadings)
{
    var h2Counts = allCompetitorHeadings
        .Select(headings => headings.Count(h => h.Level == "h2"))
        .ToList();

    var h3Counts = allCompetitorHeadings
        .Select(headings => headings.Count(h => h.Level == "h3"))
        .ToList();

    return new HeadingBenchmarks
    {
        AvgH2Count = (int)Math.Round(h2Counts.Average()),
        MinH2Count = (int)(h2Counts.Average() * 0.7),
        MaxH2Count = (int)(h2Counts.Average() * 1.3),
        AvgH3Count = (int)Math.Round(h3Counts.Average()),
        MinH3Count = (int)(h3Counts.Average() * 0.7),
        MaxH3Count = (int)(h3Counts.Average() * 1.3)
    };
}
```

### The SerpBenchmarks Record

All benchmarks are bundled into a single `SerpBenchmarks` record that is passed to `ScoreAsync`:

```csharp
public sealed record SerpBenchmarks
{
    public required string Keyword { get; init; }
    public required string Location { get; init; }
    public required IReadOnlyList<TermBenchmark> ImportantTerms { get; init; }   // top 50
    public required WordCountBenchmark WordCount { get; init; }
    public required HeadingBenchmarks Headings { get; init; }
    public required IReadOnlyList<string> PeopleAlsoAsk { get; init; }
    public required IReadOnlyList<string> RelatedSearches { get; init; }
    public required DateTimeOffset CalculatedAt { get; init; }
    public required int CompetitorPagesAnalyzed { get; init; }
    public required string BenchmarkQuality { get; init; }  // "good" | "low_sample_count"
}
```

---

## 6. Scoring Formula

The total content score is the sum of 6 components, each with a defined maximum. Total maximum: 100 points.

| Component | Max Points | What It Measures |
|---|---|---|
| Term Coverage | 35 | Coverage of important semantic terms relative to benchmarks |
| Word Count | 20 | Content length relative to competitor average |
| Heading Structure | 15 | H1 presence, H2 count in range, H3 count in range |
| Title Tag | 10 | Target keyword in title, title length in optimal range |
| Meta Description | 10 | Target keyword in meta description, meta length in optimal range |
| Readability | 10 | Flesch-Kincaid grade level vs. competitor average |

### Component 1: Term Coverage (35 points)

Term coverage measures what fraction of the "important terms" are present in the user's content at or above the minimum recommended frequency.

```csharp
public static int CalculateTermCoverageScore(
    string contentText,
    IReadOnlyList<TermBenchmark> importantTerms,
    int maxScore = 35)
{
    if (importantTerms.Count == 0) return 0;

    var contentTokens = Tokenize(contentText);
    var contentFrequency = contentTokens
        .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

    int termsInRange = 0;

    foreach (var benchmark in importantTerms)
    {
        var currentCount = contentFrequency.GetValueOrDefault(benchmark.Term, 0);

        // Partial credit: count ngrams that are present but below minimum
        // Full credit: count is within or above the recommended range
        if (currentCount >= benchmark.RecommendedRange.Min)
        {
            termsInRange++;
        }
    }

    // Score = (terms_in_range / total_important_terms) * maxScore
    // Round to nearest integer
    return (int)Math.Round((double)termsInRange / importantTerms.Count * maxScore);
}
```

**Partial credit consideration:** The formula above gives 0 points for any term below the minimum. An alternative with partial credit:
```
score = SUM over all terms:
    (min(currentCount, recommendedMax) / recommendedMax) * (importanceScore / totalImportanceScore) * maxScore
```

Use the simpler version (full credit / no credit) for the initial implementation. The partial credit formula is available but introduces more complexity in the suggestion UI.

### Component 2: Word Count (20 points)

```csharp
public static int CalculateWordCountScore(
    int userWordCount,
    WordCountBenchmark benchmark,
    int maxScore = 20)
{
    if (userWordCount >= benchmark.RecommendedMin && userWordCount <= benchmark.RecommendedMax)
    {
        // Perfect: full score
        return maxScore;
    }

    if (userWordCount < benchmark.RecommendedMin)
    {
        // Under minimum: scaled score
        // At 0 words: 0 points. At exactly min: full points.
        var ratio = (double)userWordCount / benchmark.RecommendedMin;
        return (int)Math.Round(ratio * maxScore);
    }

    // Over maximum: gentle penalty (capped at maxScore, not penalized to 0)
    // Very long content is rarely penalized heavily by Google.
    // Give full score up to 150% of max, then scale down.
    var overageRatio = (double)userWordCount / benchmark.RecommendedMax;
    if (overageRatio <= 1.5)
        return maxScore;

    // Beyond 150% of benchmark max: 15% deduction per 10% over
    var excessFraction = overageRatio - 1.5;
    var deduction = (int)(excessFraction * 10 * 0.15 * maxScore);
    return Math.Max(0, maxScore - deduction);
}
```

### Component 3: Heading Structure (15 points)

```csharp
public static int CalculateHeadingScore(
    IReadOnlyList<Heading> userHeadings,
    HeadingBenchmarks benchmark,
    int maxScore = 15)
{
    int score = 0;

    // H1 present: 5 points (exactly one H1 — not satisfied by H2 alone)
    if (userHeadings.Any(h => h.Level == "h1"))
        score += 5;

    var userH2Count = userHeadings.Count(h => h.Level == "h2");
    var userH3Count = userHeadings.Count(h => h.Level == "h3");

    // H2 count in benchmark range: 5 points
    if (userH2Count >= benchmark.MinH2Count && userH2Count <= benchmark.MaxH2Count)
        score += 5;
    else if (userH2Count > 0)
        score += 2;  // partial credit for having H2s but not enough

    // H3 count in range: 5 points (optional — H3s are not always present in benchmarks)
    if (benchmark.AvgH3Count > 0)
    {
        if (userH3Count >= benchmark.MinH3Count && userH3Count <= benchmark.MaxH3Count)
            score += 5;
        else if (userH3Count > 0)
            score += 2;
    }
    else
    {
        // No H3 benchmark available — give full 5 points regardless of H3 usage
        score += 5;
    }

    return Math.Min(score, maxScore);
}
```

### Component 4: Title Tag (10 points)

Note: The user's "title tag" in the context of the content editor is the `<title>` meta field stored with the document, not the H1. The frontend provides a separate title field in the editor sidebar.

```csharp
public static int CalculateTitleTagScore(
    string? titleTag,
    string targetKeyword,
    int maxScore = 10)
{
    if (string.IsNullOrWhiteSpace(titleTag))
        return 0;

    int score = 0;

    // Keyword present in title: 5 points
    if (titleTag.Contains(targetKeyword, StringComparison.OrdinalIgnoreCase))
        score += 5;
    else
    {
        // Partial: check if keyword words are all present (not as exact phrase)
        var keywordWords = targetKeyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (keywordWords.All(w => titleTag.Contains(w, StringComparison.OrdinalIgnoreCase)))
            score += 3;
    }

    // Title length 50-60 characters: 5 points
    var titleLength = titleTag.Length;
    if (titleLength >= 50 && titleLength <= 60)
        score += 5;
    else if (titleLength >= 40 && titleLength <= 70)
        score += 3;
    else if (titleLength >= 30)
        score += 1;

    return Math.Min(score, maxScore);
}
```

### Component 5: Meta Description (10 points)

```csharp
public static int CalculateMetaDescriptionScore(
    string? metaDescription,
    string targetKeyword,
    int maxScore = 10)
{
    if (string.IsNullOrWhiteSpace(metaDescription))
        return 0;

    int score = 0;

    // Keyword present: 5 points
    if (metaDescription.Contains(targetKeyword, StringComparison.OrdinalIgnoreCase))
        score += 5;
    else
    {
        var keywordWords = targetKeyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (keywordWords.All(w => metaDescription.Contains(w, StringComparison.OrdinalIgnoreCase)))
            score += 3;
    }

    // Length 120-160 characters: 5 points
    var descLength = metaDescription.Length;
    if (descLength >= 120 && descLength <= 160)
        score += 5;
    else if (descLength >= 100 && descLength <= 180)
        score += 3;
    else if (descLength >= 50)
        score += 1;

    return Math.Min(score, maxScore);
}
```

### Component 6: Readability (10 points)

Readability is measured using the Flesch-Kincaid Grade Level formula. The user's content grade level is compared to the average grade level across the 10 competitor pages.

**Flesch-Kincaid Grade Level Formula:**
```
FKGL = 0.39 × (totalWords / totalSentences) + 11.8 × (totalSyllables / totalWords) - 15.59
```

Syllable counting: use a simple English syllable-counting heuristic (count vowel groups, apply exceptions for silent e, common prefixes/suffixes). A perfect syllable counter is not required — approximation is acceptable given the statistical nature of benchmarking.

```csharp
public static int CalculateReadabilityScore(
    string contentText,
    IReadOnlyList<double> competitorFkglScores,
    int maxScore = 10)
{
    var userFkgl = CalculateFleschKincaidGradeLevel(contentText);
    var benchmarkAvg = competitorFkglScores.Average();

    // Ideal: user's FKGL is within ±1.5 grade levels of benchmark average
    var deviation = Math.Abs(userFkgl - benchmarkAvg);

    if (deviation <= 1.5)
        return maxScore;                              // Within range: 10 points
    if (deviation <= 3.0)
        return (int)(maxScore * 0.7);                // Slightly off: 7 points
    if (deviation <= 5.0)
        return (int)(maxScore * 0.4);                // Significantly off: 4 points
    return (int)(maxScore * 0.2);                    // Very far off: 2 points
}
```

**Readability direction note:** Both too complex (high FKGL) and too simple (low FKGL) are penalized relative to the benchmark. If the top-ranking competitors all write at a grade 8 level, writing at grade 12 or grade 4 are both suboptimal. Users are told "write at approximately grade {benchmarkAvg} — your current content is grade {userFkgl}."

### Full ScoreAsync Method

```csharp
// GeekSeoBackend/Services/Scoring/ContentScoringService.cs

public async Task<Result<ContentScoreResult>> ScoreAsync(
    string contentHtml,
    string targetKeyword,
    string location,
    SerpBenchmarks benchmarks,
    CancellationToken ct = default)
{
    // Extract plain text and structure from HTML
    var plainText = _richTextProvider.ExtractPlainText(contentHtml);
    var headings = _richTextProvider.ExtractHeadings(contentHtml);
    var wordCount = _richTextProvider.CountWords(contentHtml);
    var h1Text = _richTextProvider.ExtractFirstH1(contentHtml);

    // Extract title and meta from content (stored as separate fields on the document)
    // These are passed in via the contentHtml or the document record — handled by caller
    // For now, extract from HTML meta tags if present
    var titleTag = ExtractMetaTitle(contentHtml) ?? h1Text;
    var metaDescription = ExtractMetaDescription(contentHtml);

    // Calculate each component
    var termCoverageScore = NlpExtractor.CalculateTermCoverageScore(
        plainText, benchmarks.ImportantTerms);

    var wordCountScore = CalculateWordCountScore(
        wordCount, benchmarks.WordCount);

    var headingScore = CalculateHeadingScore(
        headings, benchmarks.Headings);

    var titleScore = CalculateTitleTagScore(
        titleTag, targetKeyword);

    var metaScore = CalculateMetaDescriptionScore(
        metaDescription, targetKeyword);

    // Readability requires competitor FKGL scores — calculate from competitor pages in cache
    var competitorFkglScores = await GetCompetitorFkglScoresAsync(benchmarks, ct);
    var readabilityScore = CalculateReadabilityScore(plainText, competitorFkglScores);

    var totalScore = termCoverageScore + wordCountScore + headingScore +
                     titleScore + metaScore + readabilityScore;

    var grade = ScoreToGrade(totalScore);

    // Generate suggestions for all components that are not at maximum
    var suggestions = GenerateSuggestions(
        plainText, headings, wordCount, titleTag, metaDescription,
        targetKeyword, benchmarks,
        termCoverageScore, wordCountScore, headingScore, titleScore, metaScore, readabilityScore);

    return Result<ContentScoreResult>.Success(new ContentScoreResult
    {
        TotalScore = totalScore,
        Grade = grade,
        Components = new ScoreComponents
        {
            TermCoverage = new ComponentScore(termCoverageScore, 35),
            WordCount = new ComponentScore(wordCountScore, 20),
            HeadingStructure = new ComponentScore(headingScore, 15),
            TitleTag = new ComponentScore(titleScore, 10),
            MetaDescription = new ComponentScore(metaScore, 10),
            Readability = new ComponentScore(readabilityScore, 10)
        },
        Suggestions = suggestions,
        WordCount = wordCount,
        HeadingCount = headings.Count,
        BenchmarkQuality = benchmarks.BenchmarkQuality,
        ScoredAt = DateTimeOffset.UtcNow
    });
}
```

---

## 7. E-E-A-T Advisory Layer (Not Scored)

The **0–100 score remains 6 components only** (Section 6). E-E-A-T is a separate **advisory list** returned in `ScoreUpdateMessage.EeatAdvisories` — never added to `TotalScore`. This matches parity feature #18 without breaking transparent scoring math.

### Rules (deterministic checks on HTML + metadata)

| Advisory code | Trigger | User-facing action |
|---|---|---|
| `author_byline` | No author name in content and no `Article` schema `author` | Add author byline with credentials in intro or footer |
| `first_hand_experience` | No first-person experiential phrases in body (heuristic: "we installed", "our team", "I tested") | Add a short "our experience" section with specific details |
| `citations` | Fewer than 2 external links to authoritative domains | Link to 2+ primary sources (.gov, .edu, industry authorities) |
| `author_schema` | No `Person` or `Article` author in JSON-LD | Add `author` to Article schema |
| `about_page_link` | No link to `/about` or author bio URL | Link to about/team page |
| `date_freshness` | No visible publish/update date when competitors show dates | Add "Last updated {month year}" near top |

```csharp
public static IReadOnlyList<EeatAdvisory> GenerateEeatAdvisories(
    string contentHtml, string plainText, IReadOnlyList<SerpOrganicResult> competitors)
{
    var advisories = new List<EeatAdvisory>();
    if (!HasAuthorSignal(contentHtml, plainText))
        advisories.Add(new("author_byline", "Add an author byline with relevant credentials.", 0));
    // ... remaining checks per table
    return advisories;
}
```

GeekSeoBackend includes E-E-A-T advisories in `ScoreUpdate` (no extra API calls).

### SERP feature guidance (paired with E-E-A-T in UI)

`GetSerpFeatureGuidanceAsync` reads cached `SerpFeatures` from `seo_serp_results` and returns actionable copy (featured snippet → 40–60 word answer after first H2; local pack → NAP block; etc.). Included in `ScoreUpdateMessage.SerpFeatures`.

---

## 8. Real-Time WebSocket Flow

### Complete End-to-End Timeline

```
t=0ms    User types a character in TipTap editor
         TipTap fires onUpdate event
         Previous debounce timer is cleared (if any)

t=800ms  Debounce timer fires (no keystrokes in last 800ms)
         useContentScoring hook calls:
         connection.invoke("ContentChanged", documentId, contentHtml, targetKeyword)

t=800ms  SignalR message arrives at SeoScoringHub.ContentChanged(...)
         on GeekSeoBackend → ScoringOrchestrator

t=801ms  ScoringOrchestrator calls ISeoDataClient to load SERP/competitor cache
         ├── CACHE HIT:
         │   Benchmarks from SeoDataClient (~5ms) → ScoreContent only
         └── CACHE MISS:
             FetchBenchmarks (DataForSEO + Playwright + Claude)
             GeekSeoBackend upserts cache tables
             First-fetch latency: 20-40 seconds (once per keyword/location TTL)

t=810ms  (cache hit path) GeekSeoBackend.ScoreContent runs
         Tokenization, n-gram extraction, frequency calculation: ~50ms
         6-component scoring: ~10ms
         Suggestion generation: ~10ms
         Total compute: ~70ms

t=880ms  Hub.Clients.Group($"doc:{documentId}").SendAsync("ScoreUpdate", update)
         SignalR pushes message to all clients in the document group

t=882ms  Frontend receives ScoreUpdate message
         React state updated
         ScoreSidebar re-renders with new score, grade, components, suggestions
         Animated score ring transitions to new value (CSS transition 300ms)

Total latency (cache hit): ~82ms from debounce fire to UI update
Total latency (cache miss, first keyword): 25-45 seconds (show loading state)
```

### Handling First-Load Scoring

When a user opens a content document (`/app/content/[id]`), the initial score must be computed immediately. This is not a real-time keystroke event — it is a page load.

```typescript
// app/content/[id]/page.tsx — useEffect on mount

useEffect(() => {
  // Request an initial score as soon as the SignalR connection is established
  const handleConnected = () => {
    connection.invoke('ContentChanged', documentId, document.contentHtml, document.targetKeyword);
  };
  connection.onreconnected(handleConnected);
}, [documentId]);
```

If the SERP cache is cold (document is opened for the first time), the `ScoreSidebar` shows a loading state with a message: "Analyzing top-ranking competitors for '{keyword}'... This takes 20-30 seconds on first load." The loading state resolves when the first `ScoreUpdate` message arrives.

### Benchmark Refresh Trigger

Benchmarks are re-fetched only when the target keyword changes. This prevents expensive SERP API calls on every keystroke. The user changes the keyword in the editor sidebar → frontend calls `connection.invoke("KeywordChanged", documentId, newKeyword, location)` → hub broadcasts `BenchmarkRefreshing` to the client → frontend shows a mini-loading state in the competitor panel → hub eagerly starts the benchmark refresh in the background → next `ContentChanged` call uses the new benchmarks.

### Group Management

SignalR groups are keyed by `doc:{documentId}`. Each browser tab that opens a document joins the group. If two browser tabs are open on the same document, both receive score updates from either one's keystrokes. This is a feature, not a bug — users can have their phone and laptop both showing the score.

Hub cleanup: when a client disconnects (`OnDisconnectedAsync`), their connection ID is automatically removed from all groups by SignalR. No explicit cleanup is needed.

### Score Update Throttling

The hub does not impose additional server-side rate limiting beyond the client's 800ms debounce. If a client sends `ContentChanged` messages more frequently (e.g., a bug in the debounce), the hub processes each one. This is acceptable — scoring is CPU-light when benchmarks are cached. If needed, a per-user rate limit of 2 scoring requests per second can be added using `IMemoryCache` without architectural changes.

---

## 9. Suggestion Generation

Suggestions are generated for every component that is below its maximum score. They are sorted by `pointValue` descending — the suggestions that would produce the largest score increase appear first.

### Suggestion Record

```csharp
public sealed record SuggestionItem
{
    public required string Component { get; init; }    // "termCoverage", "wordCount", etc.
    public required int PointValue { get; init; }       // potential points to gain
    public required string ActionText { get; init; }    // human-readable instruction
    public required string CurrentValue { get; init; }  // what the content currently has
    public required string TargetValue { get; init; }   // what it needs to have
}
```

### Suggestion Generation Per Component

**Term Coverage Suggestions:**

```csharp
// Generate one suggestion per missing term, up to 15 suggestions
// Sort missing terms by importance score descending
foreach (var benchmark in importantTerms
    .Where(t => GetCurrentCount(plainText, t.Term) < t.RecommendedRange.Min)
    .OrderByDescending(t => t.ImportanceScore)
    .Take(15))
{
    var currentCount = GetCurrentCount(plainText, benchmark.Term);
    var targetCount = (int)Math.Ceiling(benchmark.RecommendedRange.Min);

    suggestions.Add(new SuggestionItem
    {
        Component = "termCoverage",
        PointValue = CalculateMarginalPointGain(benchmark, currentCount, targetCount),
        ActionText = currentCount == 0
            ? $"Add the term \"{benchmark.Term}\" to your content (used by {benchmark.DocumentFrequency}/10 top pages)"
            : $"Use \"{benchmark.Term}\" {targetCount - currentCount} more time(s) (current: {currentCount}, target: {targetCount})",
        CurrentValue = currentCount.ToString(),
        TargetValue = $"{benchmark.RecommendedRange.Min}-{benchmark.RecommendedRange.Max}"
    });
}
```

**Word Count Suggestion:**

```csharp
if (wordCount < benchmark.WordCount.RecommendedMin)
{
    suggestions.Add(new SuggestionItem
    {
        Component = "wordCount",
        PointValue = wordCountMaxScore - wordCountScore,
        ActionText = $"Add approximately {benchmark.WordCount.RecommendedMin - wordCount} more words (current: {wordCount}, target: {benchmark.WordCount.RecommendedMin}-{benchmark.WordCount.RecommendedMax})",
        CurrentValue = wordCount.ToString(),
        TargetValue = $"{benchmark.WordCount.RecommendedMin}-{benchmark.WordCount.RecommendedMax}"
    });
}
else if (wordCount > benchmark.WordCount.RecommendedMax * 1.5)
{
    suggestions.Add(new SuggestionItem
    {
        Component = "wordCount",
        PointValue = wordCountMaxScore - wordCountScore,
        ActionText = $"Consider reducing content length. Current {wordCount} words is significantly longer than the {benchmark.WordCount.RecommendedMax}-word benchmark",
        CurrentValue = wordCount.ToString(),
        TargetValue = $"{benchmark.WordCount.RecommendedMin}-{benchmark.WordCount.RecommendedMax}"
    });
}
```

**Heading Structure Suggestions:**

```csharp
if (!userHeadings.Any(h => h.Level == "h1"))
{
    suggestions.Add(new SuggestionItem
    {
        Component = "headingStructure",
        PointValue = 5,
        ActionText = "Add an H1 heading to your content (currently missing)",
        CurrentValue = "0 H1 headings",
        TargetValue = "1 H1 heading"
    });
}

var userH2Count = userHeadings.Count(h => h.Level == "h2");
if (userH2Count < benchmark.Headings.MinH2Count)
{
    var needed = benchmark.Headings.MinH2Count - userH2Count;
    suggestions.Add(new SuggestionItem
    {
        Component = "headingStructure",
        PointValue = headingScore < 15 ? Math.Min(5, 15 - headingScore) : 0,
        ActionText = $"Add {needed} more H2 subheading(s) (current: {userH2Count}, benchmark: {benchmark.Headings.MinH2Count}-{benchmark.Headings.MaxH2Count})",
        CurrentValue = $"{userH2Count} H2 headings",
        TargetValue = $"{benchmark.Headings.MinH2Count}-{benchmark.Headings.MaxH2Count} H2 headings"
    });
}
```

**Title Tag Suggestions:**

```csharp
if (string.IsNullOrWhiteSpace(titleTag))
{
    suggestions.Add(new SuggestionItem
    {
        Component = "titleTag",
        PointValue = titleMaxScore,
        ActionText = "Add a title tag containing your target keyword",
        CurrentValue = "No title tag",
        TargetValue = $"50-60 character title with \"{targetKeyword}\""
    });
}
else
{
    if (!titleTag.Contains(targetKeyword, StringComparison.OrdinalIgnoreCase))
    {
        suggestions.Add(new SuggestionItem
        {
            Component = "titleTag",
            PointValue = 5,
            ActionText = $"Include your target keyword \"{targetKeyword}\" in your title tag",
            CurrentValue = titleTag,
            TargetValue = $"Title containing \"{targetKeyword}\""
        });
    }

    var titleLen = titleTag.Length;
    if (titleLen < 50)
    {
        suggestions.Add(new SuggestionItem
        {
            Component = "titleTag",
            PointValue = 5,
            ActionText = $"Extend your title to 50-60 characters (currently {titleLen} characters)",
            CurrentValue = $"{titleLen} characters",
            TargetValue = "50-60 characters"
        });
    }
    else if (titleLen > 60)
    {
        suggestions.Add(new SuggestionItem
        {
            Component = "titleTag",
            PointValue = 5,
            ActionText = $"Shorten your title to 50-60 characters (currently {titleLen} — Google truncates at ~60)",
            CurrentValue = $"{titleLen} characters",
            TargetValue = "50-60 characters"
        });
    }
}
```

**Meta Description Suggestions:**

```csharp
if (string.IsNullOrWhiteSpace(metaDescription))
{
    suggestions.Add(new SuggestionItem
    {
        Component = "metaDescription",
        PointValue = metaMaxScore,
        ActionText = "Add a meta description between 120-160 characters that includes your target keyword",
        CurrentValue = "No meta description",
        TargetValue = $"120-160 character description with \"{targetKeyword}\""
    });
}
// ... (keyword presence and length checks same pattern as title)
```

**Readability Suggestion:**

```csharp
if (readabilityScore < 10)
{
    var direction = userFkgl > benchmarkFkglAvg ? "simpler" : "more sophisticated";
    var gradeTarget = (int)Math.Round(benchmarkFkglAvg);

    suggestions.Add(new SuggestionItem
    {
        Component = "readability",
        PointValue = 10 - readabilityScore,
        ActionText = $"Write at a Grade {gradeTarget} reading level — your content is Grade {(int)Math.Round(userFkgl)} ({(userFkgl > benchmarkFkglAvg ? "too complex" : "too simple")} for this keyword)",
        CurrentValue = $"Grade {(int)Math.Round(userFkgl)}",
        TargetValue = $"Grade {gradeTarget - 1}-{gradeTarget + 1}"
    });
}
```

### Final Suggestion Sort

After generating all suggestions:

```csharp
var sortedSuggestions = suggestions
    .OrderByDescending(s => s.PointValue)
    .ThenBy(s => s.Component)  // tiebreak: consistent ordering by component name
    .ToList();
```

The ScoreSidebar renders the top 10 suggestions. The remaining suggestions are accessible via a "Show all {count} suggestions" expandable section.

---

## 10. Grade Scale

The letter grade is a user-friendly summary of the numeric score. It is displayed prominently in the `ScoreSidebar` alongside the score ring.

| Score Range | Grade | Label | Color |
|---|---|---|---|
| 90-100 | A | Excellent | Green (`#22c55e`) |
| 75-89 | B | Good | Light green (`#84cc16`) |
| 60-74 | C | Needs Work | Yellow (`#eab308`) |
| 40-59 | D | Poor | Orange (`#f97316`) |
| 0-39 | F | Critical | Red (`#ef4444`) |

```csharp
public static string ScoreToGrade(int score) => score switch
{
    >= 90 => "A",
    >= 75 => "B",
    >= 60 => "C",
    >= 40 => "D",
    _     => "F"
};
```

### Grade Display in UI

The `ScoreSidebar` shows:
- A circular progress ring (SVG, animated) filled to the score percentage
- The grade letter in the center of the ring (large, bold)
- The numeric score below the grade (smaller)
- The grade label text below the numeric score ("Good", "Needs Work", etc.)
- Color-coded: ring stroke, grade letter, and label all use the grade color

On score change (WebSocket update), the ring animates from old score to new score using CSS transitions (`transition: stroke-dashoffset 300ms ease-in-out`). The grade letter fades out and in if the grade changes.

---

## 11. Comparison to Surfer SEO

### Methodology Foundation: Shared

Both Surfer SEO and Geek SEO use correlation-based content scoring. Both fetch the top SERP results, extract text from ranking pages, identify common terms and topics, and measure whether the user's content covers those signals.

Neither tool claims to reverse-engineer Google's algorithm. Both are measuring what correlates with ranking performance, not what causes it. This caveat is worth being explicit about: correlation SEO is not a guarantee, it's a probability signal.

### Where Geek SEO Differs

**Formula transparency:**

Surfer shows a 0-100 score and a list of keyword recommendations. The weight of each recommendation on the final score is not disclosed. Users have reported adding all suggested terms and seeing the score go up by only 3 points, with no explanation of why.

Geek SEO shows exactly which 6 components produce the score and exactly how many points each component is worth. Users know before editing that "if I add these 5 terms, my term coverage goes from 18/35 to 26/35, and my total score goes from 62 to 70." This is a fundamental difference in user trust architecture.

**AI provider layer:**

Surfer uses a proprietary NLP pipeline for term and entity extraction. It is not transparent about which NLP model or methodology it uses.

Geek SEO uses `IAIProvider` (Claude Sonnet primary) for semantic term extraction. The AI layer is swappable — if Anthropic raises prices or changes the API, OpenAI or Gemini can be substituted at the DI layer. The prompt used for extraction is documented and auditable. The term list produced by the AI is stored in `seo_competitor_pages.terms` and can be inspected in the database for debugging.

**Local SERP targeting:**

Surfer's location targeting is available but not prominently featured at lower plan tiers. For a "plumber in Boca Raton" keyword, running the analysis against national SERP results produces different (less useful) benchmarks than running it against Boca Raton-specific results. The pages that rank for that keyword in Boca Raton are locally-oriented service pages, not national plumbing directories.

Geek SEO passes the `location` parameter on every SERP request from day one, at every plan tier. This is architecturally simpler than Surfer's approach because the DataForSEO API handles geo-targeting natively.

**Surfer's "True Density" vs. Geek SEO's term frequency:**

Surfer's True Density is a placement-aware metric: it weights keyword occurrences differently depending on whether they appear in the title, H1, heading tags, or body text. A keyword in the H1 counts more than one in the body.

Geek SEO's term coverage score counts occurrences in plain text regardless of placement. The heading structure score handles H1/H2/H3 presence separately. This is a deliberate simplification that makes the formula clearer and the suggestions more actionable — users are told "add this term 3 more times anywhere in your content" rather than "add it in a heading context."

Placement-weighted term scoring (Surfer True Density) is **not cloned** — heading structure is scored separately (§4.3).

**Benchmark sample size:**

Surfer analyzes 50+ ranking pages in some plans. Geek SEO benchmarks against the top 10 organic results.

The practical difference is small. The top 10 results are the most consistent signal — pages 11-50 have significant variance and include many near-duplicate results. Analyzing 10 pages produces stable benchmarks with lower API cost and faster first-load times.

**What Surfer does that Geek SEO handles differently (see master plan §1.1 Not cloned):**

- **True Density** — not cloned; transparent term coverage + separate heading score
- **50-competitor grid in scoring UI** — deep 50-result view is parity #7 (`/app/serp/[keyword]`); live editor benchmarks top 10 for latency

**Covered elsewhere in master plan (not in this spec):** Topical Map (#3), multi-LLM AI Visibility (#10), Plagiarism (#6), Content Guard (#24).

---

## 9. GEO Score (parity #25)

Separate from the SEO total. Computed in `ContentScoringService` alongside SEO; both returned in `ScoreUpdate` SignalR payload.

| Dimension | Max | What it measures |
|-----------|-----|------------------|
| **Authority** | 20 | Citations, expert quotes, named sources, E-E-A-T signals |
| **Readability** | 20 | Clarity for AI extraction (short paragraphs, definitions, lists) |
| **Structure** | 20 | Headings, FAQ blocks, direct-answer paragraphs for AI snippets |
| **Citations** | 20 | Outbound links to authoritative sources |
| **Depth** | 20 | Comprehensive coverage vs SERP benchmark word count and subtopics |

**GEO total:** 0–100 (sum of dimensions). **GEO grade:** A–F from same thresholds as SEO.  
**UI:** `GeoScorePanel` in editor sidebar; suggestions parallel SEO `SuggestionItem[]` with `component: 'geo'`.

Implementation steps: master plan §11 steps 41, 48.

---

## Appendix A: ContentScoreResult Record

```csharp
public sealed record ContentScoreResult
{
    public required int TotalScore { get; init; }
    public required string Grade { get; init; }
    public required ScoreComponents Components { get; init; }
    public required GeoScoreComponents GeoComponents { get; init; }  // parity #25
    public required int GeoTotalScore { get; init; }
    public required string GeoGrade { get; init; }
    public required IReadOnlyList<SuggestionItem> Suggestions { get; init; }
    public required IReadOnlyList<EeatAdvisory> EeatAdvisories { get; init; }  // parity #18 — not scored
    public required int WordCount { get; init; }
    public required int HeadingCount { get; init; }
    public required string BenchmarkQuality { get; init; }
    public required DateTimeOffset ScoredAt { get; init; }
}

public sealed record ScoreComponents
{
    public required ComponentScore TermCoverage { get; init; }     // max 35
    public required ComponentScore WordCount { get; init; }        // max 20
    public required ComponentScore HeadingStructure { get; init; } // max 15
    public required ComponentScore TitleTag { get; init; }         // max 10
    public required ComponentScore MetaDescription { get; init; }  // max 10
    public required ComponentScore Readability { get; init; }      // max 10
}

public sealed record ComponentScore(int Score, int MaxScore)
{
    public double Percentage => MaxScore > 0 ? (double)Score / MaxScore * 100 : 0;
}
```

## Appendix B: SerpBenchmarks Cache Storage Schema

The `SerpBenchmarks` object is not stored directly in PostgreSQL. It is reconstructed each time from:
1. `seo_serp_results` (SERP results for the keyword + location)
2. `seo_competitor_pages` (crawled competitor pages, with `terms` JSONB field containing pre-computed term data)

This avoids storing a large denormalized JSONB blob per benchmark set while keeping the cache lookup efficient. The benchmark reconstruction happens in `GetOrFetchBenchmarksAsync` and takes approximately 20-50ms when all data is cached.

## Appendix C: NlpExtractor Static Class

```csharp
// GeekSeoBackend/Services/Scoring/Nlp/NlpExtractor.cs

namespace GeekSeoBackend.Services.Scoring.Nlp;

public static class NlpExtractor
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "is", "it", "in", "on", "at", "to", "for", "of", "and",
        "or", "but", "with", "by", "from", "that", "this", "which", "who", "what",
        "how", "when", "where", "are", "was", "were", "be", "been", "being", "have",
        "has", "had", "do", "does", "did", "will", "would", "could", "should", "may",
        "might", "can", "not", "no", "nor", "so", "yet", "both", "either", "neither",
        "each", "few", "more", "most", "other", "such", "than", "too", "very", "just",
        "also", "about", "after", "before", "between", "during", "without", "within",
        "through", "across", "behind", "beyond", "around", "among", "above", "below",
        "under", "your", "our", "their", "its", "my", "his", "her", "we", "they",
        "you", "he", "she", "i", "me", "him", "us", "them", "if", "then", "else",
        "as", "up", "out", "all", "any", "get", "got", "use", "used", "using",
        "make", "made", "know", "need", "needs", "like", "just", "into", "over"
    };

    public static IReadOnlyList<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        return text
            .ToLowerInvariant()
            .Split([' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '"', '\'',
                    '(', ')', '[', ']', '{', '}', '/', '\\', '|', '@', '#', '$',
                    '%', '^', '&', '*', '+', '=', '<', '>', '`', '~'], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length >= 3 && !StopWords.Contains(token))
            .ToList();
    }

    public static IReadOnlyList<string> ExtractBigrams(IReadOnlyList<string> tokens)
    {
        if (tokens.Count < 2) return [];
        return Enumerable.Range(0, tokens.Count - 1)
            .Select(i => $"{tokens[i]} {tokens[i + 1]}")
            .ToList();
    }

    public static IReadOnlyList<string> ExtractTrigrams(IReadOnlyList<string> tokens)
    {
        if (tokens.Count < 3) return [];
        return Enumerable.Range(0, tokens.Count - 2)
            .Select(i => $"{tokens[i]} {tokens[i + 1]} {tokens[i + 2]}")
            .ToList();
    }

    public static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public static double CalculateFleschKincaidGradeLevel(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;

        var sentences = text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries).Length;
        var words = CountWords(text);
        if (sentences == 0 || words == 0) return 0;

        var allWords = text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var syllables = allWords.Sum(CountSyllables);

        return 0.39 * ((double)words / sentences) + 11.8 * ((double)syllables / words) - 15.59;
    }

    private static int CountSyllables(string word)
    {
        if (string.IsNullOrEmpty(word)) return 0;

        word = word.ToLowerInvariant().Trim(['.', ',', '!', '?', ';', ':']);
        if (word.Length <= 3) return 1;

        // Remove trailing silent 'e'
        if (word.EndsWith('e')) word = word[..^1];

        // Count vowel groups
        var vowels = new HashSet<char>(['a', 'e', 'i', 'o', 'u', 'y']);
        var count = 0;
        var prevWasVowel = false;

        foreach (var c in word)
        {
            var isVowel = vowels.Contains(c);
            if (isVowel && !prevWasVowel) count++;
            prevWasVowel = isVowel;
        }

        return Math.Max(1, count);
    }
}
```

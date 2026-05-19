# Geek SEO — Master Product Plan

**Version:** 1.0  
**Date:** May 2026  
**Author:** Jeff Martin, Geek At Your Spot  
**Domain:** seo.geekatyourspot.com (subject to change)  
**Backend:** GeekBackend (.NET 10) — new `GeekSEO` module  
**Database:** Supabase instance `mpnruwauxsqbrxvlksnf` — new `geek_seo` schema  

---

## 1. Product Overview

### What It Is

Geek SEO is an AI-powered SEO content optimization SaaS that helps business owners and content writers rank higher on Google and appear in AI-generated answers (ChatGPT, Perplexity, Google AI Overviews). It provides a real-time content editor that scores documents against live SERP competitors as you type, generates structured content briefs from SERP analysis, writes AI drafts via Claude, and tracks keyword rankings through Google Search Console.

### Who It Is For

Primary: Small business owners who want to rank locally and nationally but have no SEO background. They need plain-English guidance, not NLP jargon.

Secondary: Freelance content writers and solo SEO practitioners who need a full-featured content optimization suite at a price below Surfer SEO's $99+/month standard tier.

The product is dogfooded on geekatyourspot.com before selling to clients, establishing proof of ROI in a real small-business context.

### Positioning Table

| Dimension | Surfer SEO | Frase.io | NeuronWriter | Clearscope | Geek SEO |
|---|---|---|---|---|---|
| Entry price | $49/mo (annual) | $15/mo | $23/mo | $129/mo | $29/mo |
| Target user | SEO professionals | Content teams | Budget SEOs | Enterprise | Small business owners |
| Content scoring | Correlation-based, opaque | SEO + GEO dual score | NLP + AI score | F-A++ letter grade | Transparent 6-component breakdown |
| AI provider | Proprietary | Proprietary | GPT-based | Watson + Google NLP + OpenAI | IAIProvider (Claude primary, swappable) |
| Local SEO support | None | None | None | None | Native local SERP targeting |
| Transparent formula | No | No | No | No | Yes — exact point breakdown per component |
| Billing trust | Complained about on Trustpilot | Clean | Clean | Clean | No-gotchas guarantee |
| GSC integration | Yes (Topical Map required) | Yes | No | Yes | Yes — rank tracking + re-crawl alerts |
| Real-time scoring | Yes | Yes | Yes | Yes | Yes via SignalR WebSocket |
| API access | $299/mo plan | All plans | $93/mo plan | None | $59/mo and above |

### Key Differentiators from Market Research

**Transparent scoring formula.** Every competitor produces a score number without explaining the exact weighting. Geek SEO shows users the precise point breakdown: "your term coverage is 18/35 points because you're missing these 7 terms." Users who understand the scoring stay (lower churn) and trust the product (higher NPS).

**Native local SERP targeting.** No competitor has meaningful local SEO content features. Geek SEO passes a location parameter on every SERP request from day one, enabling content optimization for "plumber in Boca Raton" vs. "best plumber" — directly serving the small-business wedge.

**Provider-interface architecture.** IAIProvider, ISerpProvider, IKeywordProvider, ICrawlerProvider, IRichTextProvider, and IPdfProvider are all interface-based. Claude can be swapped for OpenAI. DataForSEO can be swapped for Serper. No business logic is coupled to any vendor. This is a maintainability and vendor-risk hedge.

**No-gotchas pricing.** Surfer's Trustpilot page is dominated by billing complaints. Geek SEO commits to monthly price equals what you pay, instant cancellation, and stable feature sets per plan.

**Honest billing as a lead generation strategy.** There are measurable search queries for "Surfer SEO cancel" and "Surfer SEO alternatives." This frustration is a direct acquisition channel.

### Dogfood GTM Strategy

Phase one is internal use on geekatyourspot.com. Every content page on the site is created and optimized through Geek SEO before any external sales. This produces:

1. Real performance data — content scores correlated with actual GSC ranking improvements
2. Bug discovery in a real small-business context
3. Screenshot-worthy before/after proof for the product landing page
4. A demo account with real data, not seeded test data

Once five to ten content pieces have documented ranking improvements, sales outreach begins with Geek At Your Spot clients and referrals. The product story is "we built this tool to grow our own site and it worked — now we're offering it to our clients."

---

## 2. System Architecture

### Full System Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         BROWSER / CLIENT                                │
│                                                                         │
│  Next.js 16 / React 19 (Vercel)                                         │
│  ┌─────────────────────┐  ┌─────────────────────┐                      │
│  │  App Router Pages   │  │  TipTap Editor      │                      │
│  │  /app/content/[id]  │  │  + ScoreSidebar     │                      │
│  │  /app/keywords      │  │  (WebSocket client) │                      │
│  │  /app/audit/[id]    │  └──────────┬──────────┘                      │
│  │  /app/rankings      │             │ SignalR WS                       │
│  └──────────┬──────────┘             │                                  │
│             │ HTTPS REST             │                                  │
└─────────────┼──────────────────────┬─┘                                  │
              │                      │                                     │
              ▼                      ▼                                     │
┌─────────────────────────────────────────────────────────────────────────┤
│                      GEEKAPI  (Railway .NET 10)                         │
│                                                                         │
│  Controllers/Seo/           Hubs/                  Middleware/          │
│  ├── ProjectsController     └── SeoContentHub      └── SeoFeatureGate  │
│  ├── ContentController           (SignalR)              (PayPal tier    │
│  ├── BriefController                                     check)         │
│  ├── WritingController                                                  │
│  ├── KeywordsController                                                 │
│  ├── AuditController                                                    │
│  ├── RankController                                                     │
│  ├── ReportsController                                                  │
│  ├── SubscriptionController                                             │
│  └── GscController                                                      │
│                                                                         │
│  Consumes GeekApplication via DI — never references GeekRepository      │
└──────────────────────────┬──────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                   GEEKAPPLICATION  (in-process class library)           │
│                                                                         │
│  Interfaces/Seo/            Services/Seo/                              │
│  ├── IContentScoringService ├── ContentScoringService                  │
│  ├── IContentBriefService   ├── ContentBriefService                    │
│  ├── IAIWritingService      ├── AIWritingService                       │
│  ├── IKeywordResearch...    ├── KeywordResearchService                 │
│  ├── ISiteAuditService      ├── SiteAuditService                       │
│  ├── IPageAuditService      ├── PageAuditService                       │
│  ├── IRankTrackingService   ├── RankTrackingService                    │
│  ├── IReportService         ├── ReportService                          │
│  ├── ISubscriptionService   ├── SubscriptionService                    │
│  └── IGscService            └── GscService                             │
│                                                                         │
│  Provider Interfaces (consumed by services, implemented in Repository) │
│  IAIProvider / ISerpProvider / IKeywordProvider / ICrawlerProvider     │
│  IRichTextProvider / IPdfProvider                                       │
└──────────────────────────┬──────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                  GEEKREPOSITORY  (in-process class library)             │
│                                                                         │
│  Repositories/Seo/          Providers/Seo/                             │
│  ├── ContentDocumentRepo    ├── ClaudeProvider          → Anthropic API │
│  ├── KeywordRepository      ├── DataForSEOSerpProvider  → DataForSEO   │
│  ├── SerpCacheRepository    ├── DataForSEOKeywordProv.  → DataForSEO   │
│  ├── AuditRepository        ├── PlaywrightCrawlerProv.  → Playwright   │
│  ├── RankRepository         ├── TipTapProvider          → TipTap       │
│  ├── SubscriptionRepository └── PuppeteerProvider       → Puppeteer    │
│  └── GscRepository                                                      │
│                                                                         │
│  All repositories use Dapper (auth-pattern, same as existing auth repos)│
└──────────────────────────┬──────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────────┐
│              SUPABASE PostgreSQL  (instance mpnruwauxsqbrxvlksnf)      │
│              Schema: geek_seo                                           │
│                                                                         │
│  seo_projects         seo_content_documents    seo_keywords             │
│  seo_keyword_clusters seo_serp_results         seo_competitor_pages     │
│  seo_page_audits      seo_site_audits          seo_site_audit_pages     │
│  seo_rank_tracking    seo_gsc_connections      seo_subscriptions        │
│  seo_reports          seo_alerts                                        │
└─────────────────────────────────────────────────────────────────────────┘
```

### WebSocket Content Scoring Flow

```
TipTap onUpdate event fires
    │
    ▼
Frontend debounce: 800ms (clears on next keystroke, fires only when user pauses)
    │
    ▼
SignalR emit: hub.sendAsync("ContentChanged", { documentId, contentHtml, targetKeyword })
    │
    ▼
SeoContentScoringHub.OnContentChanged(documentId, contentHtml, targetKeyword)
    │
    ├── Retrieve document from IContentDocumentRepository (get current target keyword)
    │
    ├── Check ISerpCacheRepository: is SERP cache for this keyword:location still valid?
    │   ├── HIT  → use cached benchmarks (no external API call)
    │   └── MISS → ISerpProvider.GetSerpResultsAsync(keyword, location)
    │               → store result in seo_serp_results (TTL 24h)
    │               → ICrawlerProvider.CrawlPageAsync for any competitor URLs not in cache
    │               → store in seo_competitor_pages (TTL 72h)
    │
    ├── IContentScoringService.ScoreAsync(contentHtml, targetKeyword, cachedBenchmarks)
    │   → Run 6-component scoring algorithm (see geekseo-content-scoring-spec.md)
    │
    └── Hub broadcast to group(documentId):
        ScoreUpdate {
            score: 72,
            grade: "C",
            components: { termCoverage: 18, wordCount: 15, headingStructure: 10, ... },
            suggestions: [ { component, pointValue, actionText, currentValue, targetValue } ],
            timestamp: "2026-05-18T14:32:00Z"
        }
    │
    ▼
Frontend ScoreSidebar receives ScoreUpdate → re-renders score ring + suggestion list
```

### GSC OAuth Flow

```
User clicks "Connect Google Search Console" in /app/settings
    │
    ▼
Frontend: GET /api/seo/gsc/auth-url?projectId={id}
    │
    ▼
GscController → GscService.GetAuthorizationUrlAsync(userId, projectId)
    → Build Google OAuth 2.0 URL:
      https://accounts.google.com/o/oauth2/v2/auth
      ?client_id={GOOGLE_CLIENT_ID}
      &redirect_uri={BASE_URL}/api/seo/gsc/callback
      &response_type=code
      &scope=https://www.googleapis.com/auth/webmasters.readonly
      &access_type=offline
      &prompt=consent
      &state={base64(userId:projectId:csrfToken)}
    │
    ▼
Browser redirects to Google consent screen
    │
    ▼
User approves → Google redirects to /api/seo/gsc/callback?code=...&state=...
    │
    ▼
GscController.Callback(code, state)
    → Validate state (csrf token, userId match)
    → POST to https://oauth2.googleapis.com/token (exchange code for tokens)
    → Receive: access_token, refresh_token, expires_in
    → AES-256-GCM encrypt refresh_token with ENCRYPTION_KEY env var
    → Store in seo_gsc_connections (encrypted_refresh_token, site_url, user_id, project_id)
    → Redirect to /app/settings?gsc=connected
    │
    ▼
Daily background job (IHostedService in GeekAPI):
    → For each row in seo_gsc_connections:
        → Decrypt refresh_token
        → GET new access_token via refresh grant
        → Call Google Search Console searchanalytics.query API
          dimensions: ["query", "page", "date"]
          startDate: yesterday, endDate: yesterday
        → Upsert results into seo_rank_tracking
```

### PayPal Webhook Flow

```
User clicks "Subscribe" on pricing page
    │
    ▼
Frontend loads PayPal JS SDK (client-side)
    → PayPal.Buttons({ createSubscription, onApprove })
    → createSubscription calls PayPal with planId for selected tier
    → PayPal returns subscriptionId in onApprove callback
    │
    ▼
Frontend: POST /api/seo/subscription/confirm
    body: { subscriptionId, tier }
    │
    ▼
SubscriptionController → SubscriptionService.ConfirmAsync(userId, subscriptionId, tier)
    → GET https://api.paypal.com/v1/billing/subscriptions/{subscriptionId}
    → Verify status = "ACTIVE", plan_id matches expected plan for tier
    → Insert into seo_subscriptions (status: "pending_webhook")
    → Return 200 OK
    │
    ▼
PayPal fires webhook to /api/seo/subscription/webhook
    BILLING.SUBSCRIPTION.ACTIVATED  → UPDATE seo_subscriptions SET status = 'active'
    BILLING.SUBSCRIPTION.CANCELLED  → UPDATE seo_subscriptions SET status = 'cancelled'
    BILLING.SUBSCRIPTION.SUSPENDED  → UPDATE seo_subscriptions SET status = 'suspended'
    PAYMENT.SALE.COMPLETED          → UPDATE seo_subscriptions SET current_period_end (calculate from cycle)
    PAYMENT.SALE.DENIED             → UPDATE seo_subscriptions SET status = 'payment_failed'
    │
    ▼
SeoFeatureGateMiddleware (on every authenticated request):
    → ISubscriptionService.GetActiveTierAsync(userId)
    → Check route against feature gate table
    → 402 Payment Required if feature not in tier
```

### Site Audit Flow

```
User enters site URL on /app/audit → clicks "Start Audit"
    │
    ▼
POST /api/seo/audit/site  body: { projectId, siteUrl }
    │
    ▼
AuditController → ISiteAuditService.StartAuditAsync(projectId, siteUrl)
    → INSERT INTO seo_site_audits (status: "running", started_at: now)
    → Enqueue background job (IBackgroundTaskQueue)
    → Return 202 Accepted with auditId
    │
    ▼
Background job: SiteAuditWorker.ExecuteAsync(auditId)
    → ICrawlerProvider.CrawlSiteAsync(siteUrl, maxPages: 100)
        → Playwright: fetch sitemap.xml or crawl from root
        → For each page: extract HTML, meta, headings, links, word count, status code
        → Respect robots.txt (check Disallow rules before fetching each URL)
        → Store each page as ICrawlResult
    │
    ├── For each crawled page:
    │   → IPageAuditService.AuditPageAsync(url, crawlResult)
    │   → Run 20-point on-page analysis:
    │     title tag present + keyword-like terms, meta description, H1 count,
    │     canonical tag, broken internal links, image alt text missing,
    │     page speed indicators (large inline scripts, render-blocking CSS),
    │     structured data presence, duplicate title detection
    │   → INSERT INTO seo_site_audit_pages (auditId, url, score, issues jsonb)
    │
    ├── Aggregate overall_score = AVG(page scores)
    │
    └── UPDATE seo_site_audits SET status = "complete", overall_score, pages_crawled, completed_at
    │
    ▼
Frontend polls GET /api/seo/audit/{auditId}/status every 5 seconds
    → When status = "complete", redirect to /app/audit/{auditId}
    → AuditResultsTable renders issues grouped by severity
```

---

## 3. Provider Interface Contracts

### IAIProvider

```csharp
// GeekApplication/Interfaces/Seo/IAIProvider.cs

namespace GeekApplication.Interfaces.Seo;

public interface IAIProvider
{
    Task<Result<AIResponse>> CompleteAsync(AIRequest request, CancellationToken ct = default);
    Task<Result<AIResponse>> CompleteWithSystemAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
    string ProviderName { get; }
}

public sealed record AIRequest
{
    public required string SystemPrompt { get; init; }
    public required string UserPrompt { get; init; }
    public string Model { get; init; } = "claude-sonnet-4-5";
    public int MaxTokens { get; init; } = 4096;
    public double Temperature { get; init; } = 0.7;
    public bool EnableCaching { get; init; } = true;
}

public sealed record AIResponse
{
    public required string Content { get; init; }
    public required string Model { get; init; }
    public required int InputTokens { get; init; }
    public required int OutputTokens { get; init; }
    public required string StopReason { get; init; }
    public bool CacheHit { get; init; }
}
```

### ISerpProvider

```csharp
// GeekApplication/Interfaces/Seo/ISerpProvider.cs

namespace GeekApplication.Interfaces.Seo;

public interface ISerpProvider
{
    Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default);
    string ProviderName { get; }
}

public sealed record SerpRequest
{
    public required string Keyword { get; init; }
    public string Location { get; init; } = "United States";
    public string LanguageCode { get; init; } = "en";
    public string CountryCode { get; init; } = "US";
    public int ResultCount { get; init; } = 10;
    public string Device { get; init; } = "desktop";
}

public sealed record SerpResult
{
    public required string Keyword { get; init; }
    public required string Location { get; init; }
    public required IReadOnlyList<SerpOrganicResult> OrganicResults { get; init; }
    public IReadOnlyList<PeopleAlsoAskResult> PeopleAlsoAsk { get; init; } = [];
    public IReadOnlyList<string> RelatedSearches { get; init; } = [];
    public string? FeaturedSnippetText { get; init; }
    public required DateTimeOffset FetchedAt { get; init; }
}

public sealed record SerpOrganicResult
{
    public required int Position { get; init; }
    public required string Url { get; init; }
    public required string Title { get; init; }
    public required string Snippet { get; init; }
    public string? Domain { get; init; }
}

public sealed record PeopleAlsoAskResult
{
    public required string Question { get; init; }
    public string? Answer { get; init; }
}
```

### IKeywordProvider

```csharp
// GeekApplication/Interfaces/Seo/IKeywordProvider.cs

namespace GeekApplication.Interfaces.Seo;

public interface IKeywordProvider
{
    Task<Result<IReadOnlyList<KeywordResult>>> GetKeywordDataAsync(KeywordRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<KeywordResult>>> GetKeywordSuggestionsAsync(string seedKeyword, string location, int count, CancellationToken ct = default);
    Task<Result<IReadOnlyList<KeywordCluster>>> ClusterKeywordsAsync(IReadOnlyList<string> keywords, string location, CancellationToken ct = default);
    string ProviderName { get; }
}

public sealed record KeywordRequest
{
    public required IReadOnlyList<string> Keywords { get; init; }
    public string Location { get; init; } = "United States";
    public string LanguageCode { get; init; } = "en";
}

public sealed record KeywordResult
{
    public required string Keyword { get; init; }
    public required int SearchVolume { get; init; }
    public required double KeywordDifficulty { get; init; }
    public required double CpcUsd { get; init; }
    public required string Competition { get; init; }
    public IReadOnlyList<MonthlySearchVolume> MonthlyTrend { get; init; } = [];
    public IReadOnlyList<string> SerpFeatures { get; init; } = [];
}

public sealed record MonthlySearchVolume
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required int Volume { get; init; }
}

public sealed record KeywordCluster
{
    public required string ClusterName { get; init; }
    public required string PillarKeyword { get; init; }
    public required IReadOnlyList<string> Keywords { get; init; }
    public required double AverageVolume { get; init; }
    public required double AverageDifficulty { get; init; }
}
```

### ICrawlerProvider

```csharp
// GeekApplication/Interfaces/Seo/ICrawlerProvider.cs

namespace GeekApplication.Interfaces.Seo;

public interface ICrawlerProvider
{
    Task<Result<PageContent>> CrawlPageAsync(string url, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PageContent>>> CrawlSiteAsync(string siteUrl, int maxPages, CancellationToken ct = default);
    Task<bool> IsAllowedByRobotsTxtAsync(string url, CancellationToken ct = default);
    string ProviderName { get; }
}

public sealed record PageContent
{
    public required string Url { get; init; }
    public required string FullText { get; init; }
    public required string? MetaTitle { get; init; }
    public required string? MetaDescription { get; init; }
    public required string? CanonicalUrl { get; init; }
    public required int WordCount { get; init; }
    public required int HttpStatusCode { get; init; }
    public required IReadOnlyList<Heading> Headings { get; init; }
    public required IReadOnlyList<string> InternalLinks { get; init; }
    public required IReadOnlyList<string> ExternalLinks { get; init; }
    public required IReadOnlyList<ImageAltData> Images { get; init; }
    public required bool HasStructuredData { get; init; }
    public required IReadOnlyList<string> StructuredDataTypes { get; init; }
    public required DateTimeOffset CrawledAt { get; init; }
}

public sealed record Heading
{
    public required string Level { get; init; }   // "h1", "h2", "h3", etc.
    public required string Text { get; init; }
}

public sealed record ImageAltData
{
    public required string Src { get; init; }
    public string? AltText { get; init; }
    public bool HasAlt => !string.IsNullOrWhiteSpace(AltText);
}

public sealed record CrawlRequest
{
    public required string Url { get; init; }
    public bool FollowLinks { get; init; } = false;
    public int MaxPages { get; init; } = 1;
    public bool RespectRobotsTxt { get; init; } = true;
}
```

### IRichTextProvider

```csharp
// GeekApplication/Interfaces/Seo/IRichTextProvider.cs

namespace GeekApplication.Interfaces.Seo;

// Minimal interface — TipTap is primarily a frontend concern.
// Backend uses this to extract plain text and structure from HTML.
public interface IRichTextProvider
{
    string ExtractPlainText(string html);
    IReadOnlyList<Heading> ExtractHeadings(string html);
    int CountWords(string html);
    string? ExtractFirstH1(string html);
    string ProviderName { get; }
}
```

### IPdfProvider

```csharp
// GeekApplication/Interfaces/Seo/IPdfProvider.cs

namespace GeekApplication.Interfaces.Seo;

public interface IPdfProvider
{
    Task<Result<PdfResult>> GeneratePdfAsync(PdfRequest request, CancellationToken ct = default);
    string ProviderName { get; }
}

public sealed record PdfRequest
{
    public required string HtmlContent { get; init; }
    public required string FileName { get; init; }
    public PdfPageFormat PageFormat { get; init; } = PdfPageFormat.A4;
    public string? HeaderHtml { get; init; }
    public string? FooterHtml { get; init; }
    public PdfMargins Margins { get; init; } = new();
}

public sealed record PdfResult
{
    public required byte[] Bytes { get; init; }
    public required string FileName { get; init; }
    public required long FileSizeBytes { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
}

public sealed record PdfMargins
{
    public string Top { get; init; } = "20mm";
    public string Bottom { get; init; } = "20mm";
    public string Left { get; init; } = "15mm";
    public string Right { get; init; } = "15mm";
}

public enum PdfPageFormat { A4, Letter, Legal }
```

---

## 4. Database Schema

All tables reside in the `geek_seo` schema on Supabase instance `mpnruwauxsqbrxvlksnf`. Row-Level Security is enabled on every table. Users can only access rows where `user_id` matches `auth.uid()`.

```sql
-- Enable schema
CREATE SCHEMA IF NOT EXISTS geek_seo;

-- Enable UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================
-- seo_projects
-- ============================================================
CREATE TABLE geek_seo.seo_projects (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id             UUID NOT NULL,
    name                TEXT NOT NULL,
    url                 TEXT NOT NULL,
    gsc_connected       BOOLEAN NOT NULL DEFAULT FALSE,
    default_location    TEXT NOT NULL DEFAULT 'United States',
    default_language    TEXT NOT NULL DEFAULT 'en',
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_projects_user_id ON geek_seo.seo_projects (user_id);

ALTER TABLE geek_seo.seo_projects ENABLE ROW LEVEL SECURITY;
CREATE POLICY seo_projects_user_policy ON geek_seo.seo_projects
    USING (user_id = auth.uid());

-- ============================================================
-- seo_content_documents
-- ============================================================
CREATE TABLE geek_seo.seo_content_documents (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    user_id             UUID NOT NULL,
    title               TEXT NOT NULL DEFAULT 'Untitled Document',
    content_html        TEXT NOT NULL DEFAULT '',
    target_keyword      TEXT NOT NULL DEFAULT '',
    target_location     TEXT NOT NULL DEFAULT 'United States',
    seo_score           INTEGER NOT NULL DEFAULT 0,
    word_count          INTEGER NOT NULL DEFAULT 0,
    score_components    JSONB NOT NULL DEFAULT '{}',
    last_scored_at      TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_content_docs_project ON geek_seo.seo_content_documents (project_id);
CREATE INDEX idx_seo_content_docs_user    ON geek_seo.seo_content_documents (user_id);
CREATE INDEX idx_seo_content_docs_score   ON geek_seo.seo_content_documents (seo_score DESC);

ALTER TABLE geek_seo.seo_content_documents ENABLE ROW LEVEL SECURITY;
CREATE POLICY seo_content_documents_user_policy ON geek_seo.seo_content_documents
    USING (user_id = auth.uid());

-- ============================================================
-- seo_keywords
-- ============================================================
CREATE TABLE geek_seo.seo_keywords (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    keyword             TEXT NOT NULL,
    location            TEXT NOT NULL DEFAULT 'United States',
    search_volume       INTEGER NOT NULL DEFAULT 0,
    keyword_difficulty  NUMERIC(5, 2) NOT NULL DEFAULT 0,
    cpc_usd             NUMERIC(10, 4) NOT NULL DEFAULT 0,
    competition         TEXT,
    serp_features       JSONB NOT NULL DEFAULT '[]',
    monthly_trend       JSONB NOT NULL DEFAULT '[]',
    cluster_id          UUID REFERENCES geek_seo.seo_keyword_clusters(id) ON DELETE SET NULL,
    cached_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at          TIMESTAMPTZ NOT NULL DEFAULT (NOW() + INTERVAL '7 days'),
    UNIQUE (project_id, keyword, location)
);

CREATE INDEX idx_seo_keywords_project    ON geek_seo.seo_keywords (project_id);
CREATE INDEX idx_seo_keywords_keyword    ON geek_seo.seo_keywords (keyword);
CREATE INDEX idx_seo_keywords_expires    ON geek_seo.seo_keywords (expires_at);
CREATE INDEX idx_seo_keywords_difficulty ON geek_seo.seo_keywords (keyword_difficulty);

-- ============================================================
-- seo_keyword_clusters
-- ============================================================
CREATE TABLE geek_seo.seo_keyword_clusters (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    name                TEXT NOT NULL,
    pillar_keyword      TEXT NOT NULL,
    keywords            JSONB NOT NULL DEFAULT '[]',
    average_volume      INTEGER NOT NULL DEFAULT 0,
    average_difficulty  NUMERIC(5, 2) NOT NULL DEFAULT 0,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_keyword_clusters_project ON geek_seo.seo_keyword_clusters (project_id);

ALTER TABLE geek_seo.seo_keyword_clusters ENABLE ROW LEVEL SECURITY;
CREATE POLICY seo_keyword_clusters_user_policy ON geek_seo.seo_keyword_clusters
    USING (project_id IN (SELECT id FROM geek_seo.seo_projects WHERE user_id = auth.uid()));

-- ============================================================
-- seo_serp_results (cache table — no RLS, project-scoped access via service layer)
-- ============================================================
CREATE TABLE geek_seo.seo_serp_results (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    keyword             TEXT NOT NULL,
    location            TEXT NOT NULL,
    language_code       TEXT NOT NULL DEFAULT 'en',
    results             JSONB NOT NULL,
    people_also_ask     JSONB NOT NULL DEFAULT '[]',
    related_searches    JSONB NOT NULL DEFAULT '[]',
    featured_snippet    TEXT,
    fetched_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at          TIMESTAMPTZ NOT NULL DEFAULT (NOW() + INTERVAL '24 hours'),
    UNIQUE (keyword, location, language_code)
);

CREATE INDEX idx_seo_serp_keyword  ON geek_seo.seo_serp_results (keyword, location);
CREATE INDEX idx_seo_serp_expires  ON geek_seo.seo_serp_results (expires_at);

-- ============================================================
-- seo_competitor_pages (cache table — 72h TTL for crawled competitor HTML)
-- ============================================================
CREATE TABLE geek_seo.seo_competitor_pages (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    serp_result_id      UUID NOT NULL REFERENCES geek_seo.seo_serp_results(id) ON DELETE CASCADE,
    url                 TEXT NOT NULL,
    domain              TEXT,
    meta_title          TEXT,
    meta_description    TEXT,
    content_text        TEXT NOT NULL DEFAULT '',
    word_count          INTEGER NOT NULL DEFAULT 0,
    headings            JSONB NOT NULL DEFAULT '[]',
    terms               JSONB NOT NULL DEFAULT '{}',
    internal_link_count INTEGER NOT NULL DEFAULT 0,
    external_link_count INTEGER NOT NULL DEFAULT 0,
    image_count         INTEGER NOT NULL DEFAULT 0,
    images_missing_alt  INTEGER NOT NULL DEFAULT 0,
    has_structured_data BOOLEAN NOT NULL DEFAULT FALSE,
    structured_data_types JSONB NOT NULL DEFAULT '[]',
    http_status         INTEGER NOT NULL DEFAULT 200,
    crawled_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at          TIMESTAMPTZ NOT NULL DEFAULT (NOW() + INTERVAL '72 hours'),
    UNIQUE (serp_result_id, url)
);

CREATE INDEX idx_seo_competitor_serp    ON geek_seo.seo_competitor_pages (serp_result_id);
CREATE INDEX idx_seo_competitor_url     ON geek_seo.seo_competitor_pages (url);
CREATE INDEX idx_seo_competitor_expires ON geek_seo.seo_competitor_pages (expires_at);

-- ============================================================
-- seo_page_audits
-- ============================================================
CREATE TABLE geek_seo.seo_page_audits (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    user_id             UUID NOT NULL,
    url                 TEXT NOT NULL,
    score               INTEGER NOT NULL DEFAULT 0,
    issues              JSONB NOT NULL DEFAULT '[]',
    metadata            JSONB NOT NULL DEFAULT '{}',
    audited_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_page_audits_project ON geek_seo.seo_page_audits (project_id);
CREATE INDEX idx_seo_page_audits_url     ON geek_seo.seo_page_audits (url);
CREATE INDEX idx_seo_page_audits_user    ON geek_seo.seo_page_audits (user_id);

ALTER TABLE geek_seo.seo_page_audits ENABLE ROW LEVEL SECURITY;
CREATE POLICY seo_page_audits_user_policy ON geek_seo.seo_page_audits
    USING (user_id = auth.uid());

-- ============================================================
-- seo_site_audits
-- ============================================================
CREATE TABLE geek_seo.seo_site_audits (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    status              TEXT NOT NULL DEFAULT 'pending'
                            CHECK (status IN ('pending', 'running', 'complete', 'failed')),
    pages_crawled       INTEGER NOT NULL DEFAULT 0,
    overall_score       NUMERIC(5, 2),
    error_message       TEXT,
    started_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at        TIMESTAMPTZ
);

CREATE INDEX idx_seo_site_audits_project ON geek_seo.seo_site_audits (project_id);
CREATE INDEX idx_seo_site_audits_status  ON geek_seo.seo_site_audits (status);

ALTER TABLE geek_seo.seo_site_audits ENABLE ROW LEVEL SECURITY;
CREATE POLICY seo_site_audits_user_policy ON geek_seo.seo_site_audits
    USING (project_id IN (SELECT id FROM geek_seo.seo_projects WHERE user_id = auth.uid()));

-- ============================================================
-- seo_site_audit_pages
-- ============================================================
CREATE TABLE geek_seo.seo_site_audit_pages (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    audit_id            UUID NOT NULL REFERENCES geek_seo.seo_site_audits(id) ON DELETE CASCADE,
    url                 TEXT NOT NULL,
    score               INTEGER NOT NULL DEFAULT 0,
    http_status         INTEGER NOT NULL DEFAULT 200,
    word_count          INTEGER NOT NULL DEFAULT 0,
    issues              JSONB NOT NULL DEFAULT '[]',
    metadata            JSONB NOT NULL DEFAULT '{}'
);

CREATE INDEX idx_seo_site_audit_pages_audit ON geek_seo.seo_site_audit_pages (audit_id);
CREATE INDEX idx_seo_site_audit_pages_score ON geek_seo.seo_site_audit_pages (score);

-- ============================================================
-- seo_rank_tracking
-- ============================================================
CREATE TABLE geek_seo.seo_rank_tracking (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    keyword             TEXT NOT NULL,
    page_url            TEXT,
    position            NUMERIC(6, 2),
    impressions         INTEGER NOT NULL DEFAULT 0,
    clicks              INTEGER NOT NULL DEFAULT 0,
    ctr                 NUMERIC(6, 4),
    date                DATE NOT NULL,
    source              TEXT NOT NULL DEFAULT 'gsc',
    UNIQUE (project_id, keyword, date)
);

CREATE INDEX idx_seo_rank_tracking_project ON geek_seo.seo_rank_tracking (project_id);
CREATE INDEX idx_seo_rank_tracking_keyword ON geek_seo.seo_rank_tracking (keyword);
CREATE INDEX idx_seo_rank_tracking_date    ON geek_seo.seo_rank_tracking (date DESC);
CREATE INDEX idx_seo_rank_tracking_pos     ON geek_seo.seo_rank_tracking (position);

ALTER TABLE geek_seo.seo_rank_tracking ENABLE ROW LEVEL SECURITY;
CREATE POLICY seo_rank_tracking_user_policy ON geek_seo.seo_rank_tracking
    USING (project_id IN (SELECT id FROM geek_seo.seo_projects WHERE user_id = auth.uid()));

-- ============================================================
-- seo_gsc_connections
-- ============================================================
CREATE TABLE geek_seo.seo_gsc_connections (
    id                       UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id                  UUID NOT NULL,
    project_id               UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    encrypted_refresh_token  BYTEA NOT NULL,
    encryption_iv            BYTEA NOT NULL,
    encryption_tag           BYTEA NOT NULL,
    site_url                 TEXT NOT NULL,
    google_email             TEXT,
    connected_at             TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_synced_at           TIMESTAMPTZ,
    UNIQUE (project_id)
);

CREATE INDEX idx_seo_gsc_connections_user    ON geek_seo.seo_gsc_connections (user_id);
CREATE INDEX idx_seo_gsc_connections_project ON geek_seo.seo_gsc_connections (project_id);

ALTER TABLE geek_seo.seo_gsc_connections ENABLE ROW LEVEL SECURITY;
CREATE POLICY seo_gsc_connections_user_policy ON geek_seo.seo_gsc_connections
    USING (user_id = auth.uid());

-- ============================================================
-- seo_subscriptions
-- ============================================================
CREATE TABLE geek_seo.seo_subscriptions (
    id                       UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id                  UUID NOT NULL,
    paypal_subscription_id   TEXT NOT NULL UNIQUE,
    paypal_plan_id           TEXT NOT NULL,
    tier                     TEXT NOT NULL CHECK (tier IN ('starter', 'professional', 'team', 'agency')),
    status                   TEXT NOT NULL DEFAULT 'pending_webhook'
                                 CHECK (status IN ('pending_webhook', 'active', 'cancelled', 'suspended', 'payment_failed')),
    current_period_start     TIMESTAMPTZ,
    current_period_end       TIMESTAMPTZ,
    cancelled_at             TIMESTAMPTZ,
    created_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at               TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_subscriptions_user   ON geek_seo.seo_subscriptions (user_id);
CREATE INDEX idx_seo_subscriptions_status ON geek_seo.seo_subscriptions (status);
CREATE INDEX idx_seo_subscriptions_paypal ON geek_seo.seo_subscriptions (paypal_subscription_id);

ALTER TABLE geek_seo.seo_subscriptions ENABLE ROW LEVEL SECURITY;
CREATE POLICY seo_subscriptions_user_policy ON geek_seo.seo_subscriptions
    USING (user_id = auth.uid());

-- ============================================================
-- seo_reports
-- ============================================================
CREATE TABLE geek_seo.seo_reports (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    user_id             UUID NOT NULL,
    type                TEXT NOT NULL CHECK (type IN ('site_audit', 'page_audit', 'rank_tracking', 'content', 'keyword')),
    title               TEXT NOT NULL,
    file_path           TEXT,
    file_size_bytes     BIGINT,
    status              TEXT NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'generating', 'complete', 'failed')),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_reports_project ON geek_seo.seo_reports (project_id);
CREATE INDEX idx_seo_reports_user    ON geek_seo.seo_reports (user_id);

ALTER TABLE geek_seo.seo_reports ENABLE ROW LEVEL SECURITY;
CREATE POLICY seo_reports_user_policy ON geek_seo.seo_reports
    USING (user_id = auth.uid());

-- ============================================================
-- seo_alerts
-- ============================================================
CREATE TABLE geek_seo.seo_alerts (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_id          UUID NOT NULL REFERENCES geek_seo.seo_projects(id) ON DELETE CASCADE,
    user_id             UUID NOT NULL,
    type                TEXT NOT NULL CHECK (type IN ('rank_drop', 'audit_complete', 'recrawl_due', 'score_change')),
    threshold           JSONB NOT NULL DEFAULT '{}',
    enabled             BOOLEAN NOT NULL DEFAULT TRUE,
    last_sent_at        TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_seo_alerts_project ON geek_seo.seo_alerts (project_id);
CREATE INDEX idx_seo_alerts_user    ON geek_seo.seo_alerts (user_id);
CREATE INDEX idx_seo_alerts_type    ON geek_seo.seo_alerts (type);

ALTER TABLE geek_seo.seo_alerts ENABLE ROW LEVEL SECURITY;
CREATE POLICY seo_alerts_user_policy ON geek_seo.seo_alerts
    USING (user_id = auth.uid());
```

---

## 5. Backend Module Spec

### GeekApplication/Interfaces/Seo/ — Service Interfaces

```csharp
// IContentScoringService.cs
public interface IContentScoringService
{
    Task<Result<ContentScoreResult>> ScoreAsync(
        string contentHtml, string targetKeyword, string location,
        SerpBenchmarks benchmarks, CancellationToken ct = default);

    Task<Result<SerpBenchmarks>> GetOrFetchBenchmarksAsync(
        string keyword, string location, CancellationToken ct = default);
}

// IContentBriefService.cs
public interface IContentBriefService
{
    Task<Result<ContentBrief>> GenerateBriefAsync(
        string keyword, string location, int competitorCount,
        CancellationToken ct = default);
}

// IAIWritingService.cs
public interface IAIWritingService
{
    Task<Result<string>> GenerateOutlineAsync(
        string keyword, ContentBrief brief, CancellationToken ct = default);

    Task<Result<string>> GenerateDraftAsync(
        string keyword, string outline, ContentBrief brief,
        int targetWordCount, CancellationToken ct = default);

    Task<Result<string>> OptimizeContentAsync(
        string contentHtml, string targetKeyword,
        ContentScoreResult currentScore, CancellationToken ct = default);
}

// IKeywordResearchService.cs
public interface IKeywordResearchService
{
    Task<Result<IReadOnlyList<KeywordResult>>> ResearchAsync(
        string seedKeyword, string location, int resultCount,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<KeywordCluster>>> ClusterAsync(
        Guid projectId, IReadOnlyList<string> keywords,
        string location, CancellationToken ct = default);
}

// ISiteAuditService.cs
public interface ISiteAuditService
{
    Task<Result<Guid>> StartAuditAsync(Guid projectId, string siteUrl, CancellationToken ct = default);
    Task<Result<SiteAuditStatus>> GetStatusAsync(Guid auditId, CancellationToken ct = default);
    Task<Result<SiteAuditReport>> GetReportAsync(Guid auditId, CancellationToken ct = default);
}

// IPageAuditService.cs
public interface IPageAuditService
{
    Task<Result<PageAuditResult>> AuditPageAsync(
        Guid projectId, string url, CancellationToken ct = default);
    Task<Result<PageAuditResult>> AuditPageFromContentAsync(
        Guid projectId, string url, PageContent crawlResult,
        CancellationToken ct = default);
}

// IRankTrackingService.cs
public interface IRankTrackingService
{
    Task<Result<IReadOnlyList<RankSnapshot>>> GetRankHistoryAsync(
        Guid projectId, string keyword, DateTimeOffset from, DateTimeOffset to,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<RankSnapshot>>> GetTopKeywordsAsync(
        Guid projectId, int count, CancellationToken ct = default);

    Task<Result> SyncGscDataAsync(Guid projectId, CancellationToken ct = default);
}

// IReportService.cs
public interface IReportService
{
    Task<Result<Guid>> GenerateSiteAuditReportAsync(
        Guid auditId, Guid userId, CancellationToken ct = default);

    Task<Result<Guid>> GenerateRankTrackingReportAsync(
        Guid projectId, Guid userId, DateRange range,
        CancellationToken ct = default);

    Task<Result<string>> GetReportDownloadUrlAsync(
        Guid reportId, CancellationToken ct = default);
}

// ISubscriptionService.cs
public interface ISubscriptionService
{
    Task<Result<SubscriptionTier>> GetActiveTierAsync(Guid userId, CancellationToken ct = default);
    Task<Result> ConfirmSubscriptionAsync(Guid userId, string paypalSubscriptionId, SubscriptionTier tier, CancellationToken ct = default);
    Task<Result> HandleWebhookEventAsync(PayPalWebhookEvent webhookEvent, CancellationToken ct = default);
    Task<Result> CancelSubscriptionAsync(Guid userId, CancellationToken ct = default);
    bool IsFeatureAllowed(SubscriptionTier tier, string featureName);
}

// IGscService.cs
public interface IGscService
{
    Task<Result<string>> GetAuthorizationUrlAsync(Guid userId, Guid projectId, CancellationToken ct = default);
    Task<Result> HandleCallbackAsync(string code, string state, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GscDataPoint>>> FetchSearchAnalyticsAsync(Guid projectId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
    Task<Result> DisconnectAsync(Guid projectId, CancellationToken ct = default);
}
```

### GeekApplication/Interfaces/Seo/ — Repository Interfaces

```csharp
// IContentDocumentRepository.cs
public interface IContentDocumentRepository
{
    Task<Result<ContentDocument>> GetByIdAsync(Guid documentId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ContentDocument>>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<ContentDocument>> CreateAsync(CreateContentDocumentRequest request, CancellationToken ct = default);
    Task<Result> UpdateContentAsync(Guid documentId, string contentHtml, int wordCount, CancellationToken ct = default);
    Task<Result> UpdateScoreAsync(Guid documentId, int score, JsonDocument components, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid documentId, CancellationToken ct = default);
}

// IKeywordRepository.cs
public interface IKeywordRepository
{
    Task<Result<IReadOnlyList<CachedKeyword>>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<CachedKeyword?>> GetCachedAsync(Guid projectId, string keyword, string location, CancellationToken ct = default);
    Task<Result> UpsertAsync(UpsertKeywordRequest request, CancellationToken ct = default);
    Task<Result> BulkUpsertAsync(IReadOnlyList<UpsertKeywordRequest> requests, CancellationToken ct = default);
}

// ISerpCacheRepository.cs
public interface ISerpCacheRepository
{
    Task<Result<SerpCacheEntry?>> GetAsync(string keyword, string location, string languageCode, CancellationToken ct = default);
    Task<Result> UpsertAsync(UpsertSerpCacheRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CachedCompetitorPage>>> GetCompetitorPagesAsync(Guid serpResultId, CancellationToken ct = default);
    Task<Result> UpsertCompetitorPageAsync(UpsertCompetitorPageRequest request, CancellationToken ct = default);
    Task<int> DeleteExpiredAsync(CancellationToken ct = default);
}

// IAuditRepository.cs
public interface IAuditRepository
{
    Task<Result<SiteAuditRecord>> GetSiteAuditAsync(Guid auditId, CancellationToken ct = default);
    Task<Result<Guid>> CreateSiteAuditAsync(Guid projectId, CancellationToken ct = default);
    Task<Result> UpdateSiteAuditStatusAsync(Guid auditId, string status, int pagesCrawled, decimal? overallScore, string? error, CancellationToken ct = default);
    Task<Result> InsertSiteAuditPageAsync(InsertAuditPageRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAuditPageRecord>>> GetSiteAuditPagesAsync(Guid auditId, CancellationToken ct = default);
    Task<Result<PageAuditRecord>> GetPageAuditAsync(Guid auditId, CancellationToken ct = default);
    Task<Result<Guid>> CreatePageAuditAsync(CreatePageAuditRequest request, CancellationToken ct = default);
}

// IRankRepository.cs
public interface IRankRepository
{
    Task<Result<IReadOnlyList<RankSnapshot>>> GetHistoryAsync(Guid projectId, string keyword, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Result<IReadOnlyList<RankSnapshot>>> GetTopKeywordsAsync(Guid projectId, int count, CancellationToken ct = default);
    Task<Result> BulkUpsertAsync(IReadOnlyList<RankSnapshot> snapshots, CancellationToken ct = default);
    Task<Result<IReadOnlyList<string>>> GetTrackedKeywordsAsync(Guid projectId, CancellationToken ct = default);
}

// ISubscriptionRepository.cs
public interface ISubscriptionRepository
{
    Task<Result<SubscriptionRecord?>> GetActiveAsync(Guid userId, CancellationToken ct = default);
    Task<Result> InsertAsync(InsertSubscriptionRequest request, CancellationToken ct = default);
    Task<Result> UpdateStatusAsync(string paypalSubscriptionId, string status, DateTimeOffset? periodEnd, CancellationToken ct = default);
    Task<Result> CancelAsync(Guid userId, CancellationToken ct = default);
}

// IGscRepository.cs
public interface IGscRepository
{
    Task<Result<GscConnection?>> GetConnectionAsync(Guid projectId, CancellationToken ct = default);
    Task<Result> UpsertConnectionAsync(UpsertGscConnectionRequest request, CancellationToken ct = default);
    Task<Result> UpdateLastSyncedAsync(Guid projectId, CancellationToken ct = default);
    Task<Result> DeleteConnectionAsync(Guid projectId, CancellationToken ct = default);
}
```

### GeekAPI/Hubs/SeoContentScoringHub.cs

```csharp
// GeekAPI/Hubs/SeoContentScoringHub.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using GeekApplication.Interfaces.Seo;

namespace GeekAPI.Hubs;

[Authorize]
public sealed class SeoContentScoringHub(
    IContentScoringService scoringService,
    IContentDocumentRepository documentRepository) : Hub
{
    // Client joins the group for their document to receive score updates
    public async Task JoinDocument(string documentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"doc:{documentId}");
    }

    public async Task LeaveDocument(string documentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"doc:{documentId}");
    }

    // Called by client after 800ms debounce when content changes
    public async Task ContentChanged(string documentId, string contentHtml, string targetKeyword)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return;

        // Verify document belongs to this user
        var docResult = await documentRepository.GetByIdAsync(Guid.Parse(documentId));
        if (!docResult.IsSuccess || docResult.Value!.UserId != userId) return;

        var doc = docResult.Value;
        var keyword = string.IsNullOrWhiteSpace(targetKeyword)
            ? doc.TargetKeyword
            : targetKeyword;

        if (string.IsNullOrWhiteSpace(keyword)) return;

        // Fetch or use cached SERP benchmarks — no external API call if cache is warm
        var benchmarksResult = await scoringService.GetOrFetchBenchmarksAsync(
            keyword, doc.TargetLocation);

        if (!benchmarksResult.IsSuccess) return;

        var scoreResult = await scoringService.ScoreAsync(
            contentHtml, keyword, doc.TargetLocation, benchmarksResult.Value!);

        if (!scoreResult.IsSuccess) return;

        var update = new ScoreUpdateMessage
        {
            DocumentId = documentId,
            Score = scoreResult.Value!.TotalScore,
            Grade = scoreResult.Value.Grade,
            Components = scoreResult.Value.Components,
            Suggestions = scoreResult.Value.Suggestions,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Push to all clients subscribed to this document group
        await Clients.Group($"doc:{documentId}").SendAsync("ScoreUpdate", update);
    }

    // Called by client when target keyword changes — triggers fresh SERP fetch
    public async Task KeywordChanged(string documentId, string newKeyword, string location)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return;

        var docResult = await documentRepository.GetByIdAsync(Guid.Parse(documentId));
        if (!docResult.IsSuccess || docResult.Value!.UserId != userId) return;

        // Invalidate cache signal — next ContentChanged will re-fetch benchmarks
        await Clients.Caller.SendAsync("BenchmarkRefreshing", new { documentId, keyword = newKeyword });

        // Eagerly kick off benchmark refresh in background
        _ = scoringService.GetOrFetchBenchmarksAsync(newKeyword, location);
    }

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public sealed record ScoreUpdateMessage
{
    public required string DocumentId { get; init; }
    public required int Score { get; init; }
    public required string Grade { get; init; }
    public required object Components { get; init; }
    public required IReadOnlyList<object> Suggestions { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
```

### GeekAPI/Controllers/Seo/ — Route Signatures

```csharp
// ProjectsController.cs
[ApiController, Route("api/seo/projects"), Authorize]
public sealed class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpGet]             public Task<IActionResult> List(CancellationToken ct);
    [HttpGet("{id}")]     public Task<IActionResult> Get(Guid id, CancellationToken ct);
    [HttpPost]            public Task<IActionResult> Create([FromBody] CreateProjectRequest req, CancellationToken ct);
    [HttpPut("{id}")]     public Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest req, CancellationToken ct);
    [HttpDelete("{id}")]  public Task<IActionResult> Delete(Guid id, CancellationToken ct);
}

// ContentController.cs
[ApiController, Route("api/seo/content"), Authorize]
public sealed class ContentController(IContentDocumentRepository repo, IContentScoringService scoring) : ControllerBase
{
    [HttpGet]                public Task<IActionResult> List([FromQuery] Guid projectId, CancellationToken ct);
    [HttpGet("{id}")]        public Task<IActionResult> Get(Guid id, CancellationToken ct);
    [HttpPost]               public Task<IActionResult> Create([FromBody] CreateContentDocumentRequest req, CancellationToken ct);
    [HttpPut("{id}/content")] public Task<IActionResult> UpdateContent(Guid id, [FromBody] UpdateContentRequest req, CancellationToken ct);
    [HttpPost("{id}/score")]  public Task<IActionResult> ScoreNow(Guid id, CancellationToken ct);
    [HttpDelete("{id}")]     public Task<IActionResult> Delete(Guid id, CancellationToken ct);
}

// BriefController.cs
[ApiController, Route("api/seo/briefs"), Authorize]
public sealed class BriefController(IContentBriefService briefService) : ControllerBase
{
    [HttpPost("generate")] public Task<IActionResult> Generate([FromBody] GenerateBriefRequest req, CancellationToken ct);
}

// WritingController.cs
[ApiController, Route("api/seo/writing"), Authorize]
public sealed class WritingController(IAIWritingService writingService) : ControllerBase
{
    [HttpPost("outline")]  public Task<IActionResult> GenerateOutline([FromBody] GenerateOutlineRequest req, CancellationToken ct);
    [HttpPost("draft")]    public Task<IActionResult> GenerateDraft([FromBody] GenerateDraftRequest req, CancellationToken ct);
    [HttpPost("optimize")] public Task<IActionResult> OptimizeContent([FromBody] OptimizeContentRequest req, CancellationToken ct);
}

// KeywordsController.cs
[ApiController, Route("api/seo/keywords"), Authorize]
public sealed class KeywordsController(IKeywordResearchService keywordService) : ControllerBase
{
    [HttpPost("research")]   public Task<IActionResult> Research([FromBody] KeywordResearchRequest req, CancellationToken ct);
    [HttpPost("cluster")]    public Task<IActionResult> Cluster([FromBody] ClusterKeywordsRequest req, CancellationToken ct);
    [HttpGet("project/{projectId}")] public Task<IActionResult> GetProjectKeywords(Guid projectId, CancellationToken ct);
}

// AuditController.cs
[ApiController, Route("api/seo/audit"), Authorize]
public sealed class AuditController(ISiteAuditService siteAudit, IPageAuditService pageAudit) : ControllerBase
{
    [HttpPost("site")]              public Task<IActionResult> StartSiteAudit([FromBody] StartSiteAuditRequest req, CancellationToken ct);
    [HttpGet("site/{auditId}")]     public Task<IActionResult> GetSiteAudit(Guid auditId, CancellationToken ct);
    [HttpGet("site/{auditId}/status")] public Task<IActionResult> GetSiteAuditStatus(Guid auditId, CancellationToken ct);
    [HttpGet("site/{auditId}/pages")] public Task<IActionResult> GetSiteAuditPages(Guid auditId, [FromQuery] string? severity, CancellationToken ct);
    [HttpPost("page")]              public Task<IActionResult> AuditPage([FromBody] AuditPageRequest req, CancellationToken ct);
}

// RankController.cs
[ApiController, Route("api/seo/rankings"), Authorize]
public sealed class RankController(IRankTrackingService rankService) : ControllerBase
{
    [HttpGet("{projectId}/keywords")]    public Task<IActionResult> GetKeywords(Guid projectId, CancellationToken ct);
    [HttpGet("{projectId}/history")]     public Task<IActionResult> GetHistory(Guid projectId, [FromQuery] string keyword, [FromQuery] string from, [FromQuery] string to, CancellationToken ct);
    [HttpGet("{projectId}/top")]         public Task<IActionResult> GetTopKeywords(Guid projectId, [FromQuery] int count, CancellationToken ct);
    [HttpPost("{projectId}/sync")]       public Task<IActionResult> SyncGsc(Guid projectId, CancellationToken ct);
}

// ReportsController.cs
[ApiController, Route("api/seo/reports"), Authorize]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("{projectId}")]        public Task<IActionResult> List(Guid projectId, CancellationToken ct);
    [HttpPost("site-audit")]        public Task<IActionResult> GenerateSiteAuditReport([FromBody] GenerateReportRequest req, CancellationToken ct);
    [HttpPost("rank-tracking")]     public Task<IActionResult> GenerateRankReport([FromBody] GenerateRankReportRequest req, CancellationToken ct);
    [HttpGet("{reportId}/download") public Task<IActionResult> Download(Guid reportId, CancellationToken ct);
}

// SubscriptionController.cs
[ApiController, Route("api/seo/subscription")]
public sealed class SubscriptionController(ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpGet, Authorize]            public Task<IActionResult> GetCurrentTier(CancellationToken ct);
    [HttpPost("confirm"), Authorize] public Task<IActionResult> ConfirmSubscription([FromBody] ConfirmSubscriptionRequest req, CancellationToken ct);
    [HttpDelete, Authorize]         public Task<IActionResult> Cancel(CancellationToken ct);
    [HttpPost("webhook"), AllowAnonymous] public Task<IActionResult> Webhook([FromBody] JsonElement payload, CancellationToken ct);
}

// GscController.cs
[ApiController, Route("api/seo/gsc"), Authorize]
public sealed class GscController(IGscService gscService) : ControllerBase
{
    [HttpGet("auth-url")]           public Task<IActionResult> GetAuthUrl([FromQuery] Guid projectId, CancellationToken ct);
    [HttpGet("callback"), AllowAnonymous] public Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken ct);
    [HttpDelete("{projectId}")]     public Task<IActionResult> Disconnect(Guid projectId, CancellationToken ct);
    [HttpGet("{projectId}/status")] public Task<IActionResult> GetStatus(Guid projectId, CancellationToken ct);
}
```

### GeekAPI/Middleware/SeoFeatureGateMiddleware.cs

```csharp
// GeekAPI/Middleware/SeoFeatureGateMiddleware.cs

using GeekApplication.Interfaces.Seo;

namespace GeekAPI.Middleware;

public sealed class SeoFeatureGateMiddleware(
    RequestDelegate next,
    ISubscriptionService subscriptionService)
{
    // Feature to minimum tier mapping
    private static readonly Dictionary<string, SubscriptionTier> FeatureGates = new()
    {
        { "/api/seo/gsc",      SubscriptionTier.Professional },
        { "/api/seo/rankings", SubscriptionTier.Professional },
        { "/api/seo/audit/site", SubscriptionTier.Professional },
        { "/api/seo/reports",  SubscriptionTier.Professional },
        { "/api/seo/keywords/cluster", SubscriptionTier.Starter },
        { "/api/seo/writing/draft",    SubscriptionTier.Starter },
        { "/api/seo/writing/outline",  SubscriptionTier.Starter },
        { "/api/seo/writing/optimize", SubscriptionTier.Starter },
    };

    public async Task InvokeAsync(HttpContext context)
    {
        // Only gate authenticated SEO API routes
        if (!context.Request.Path.StartsWithSegments("/api/seo") ||
            context.User?.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        // Check if this path has a feature gate
        var matchedGate = FeatureGates
            .Where(kvp => context.Request.Path.StartsWithSegments(kvp.Key))
            .OrderByDescending(kvp => kvp.Key.Length)
            .FirstOrDefault();

        if (matchedGate.Key is null)
        {
            await next(context);
            return;
        }

        var userIdClaim = context.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            context.Response.StatusCode = 401;
            return;
        }

        var tierResult = await subscriptionService.GetActiveTierAsync(userId);
        var activeTier = tierResult.IsSuccess ? tierResult.Value! : SubscriptionTier.None;

        if (activeTier < matchedGate.Value)
        {
            context.Response.StatusCode = 402;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "subscription_required",
                requiredTier = matchedGate.Value.ToString().ToLower(),
                currentTier = activeTier.ToString().ToLower(),
                upgradeUrl = "/pricing"
            });
            return;
        }

        await next(context);
    }
}

public enum SubscriptionTier { None = 0, Starter = 1, Professional = 2, Team = 3, Agency = 4 }
```

---

## 6. Frontend Page Inventory

All pages use Next.js 15 App Router. Authenticated pages live under `(app)` route group with layout that validates session via GeekBackend OAuth 2.1 PKCE.

### Public Routes

| Route | Component | Description |
|---|---|---|
| `/` | `LandingPage` | Hero, feature overview, testimonials (dogfood proof), pricing teaser, CTA |
| `/pricing` | `PricingPage` | Four-tier table, PayPal checkout buttons, feature comparison table |
| `/login` | `LoginPage` | GeekBackend OAuth PKCE flow |
| `/signup` | `SignupPage` | Registration + immediate OAuth |

### Authenticated Routes (`/app/*`)

| Route | Primary Component | Data Sources |
|---|---|---|
| `/app/dashboard` | `DashboardPage` | seo_projects, seo_content_documents, seo_rank_tracking |
| `/app/content` | `ContentListPage` | seo_content_documents (by project) |
| `/app/content/[id]` | `ContentEditorPage` | SignalR WebSocket + REST content API |
| `/app/briefs/new` | `ContentBriefPage` | /api/seo/briefs/generate |
| `/app/write` | `AIWritingPage` | /api/seo/writing/outline, /draft, /optimize |
| `/app/keywords` | `KeywordResearchPage` | /api/seo/keywords/research, /cluster |
| `/app/audit` | `SiteAuditListPage` | seo_site_audits by project |
| `/app/audit/[id]` | `SiteAuditResultsPage` | seo_site_audit_pages |
| `/app/audit/page` | `PageAuditPage` | /api/seo/audit/page |
| `/app/rankings` | `RankTrackingPage` | seo_rank_tracking |
| `/app/reports` | `ReportsPage` | seo_reports |
| `/app/settings` | `SettingsPage` | subscription, GSC, account |

### Key Component Interfaces

```typescript
// ContentEditor — TipTap + WebSocket score updates
interface ContentEditorProps {
  documentId: string;
  initialContent: string;
  targetKeyword: string;
  location: string;
  onSave: (content: string) => Promise<void>;
}

// ScoreSidebar — receives ScoreUpdate from SignalR
interface ScoreSidebarProps {
  score: number;                          // 0-100
  grade: 'A' | 'B' | 'C' | 'D' | 'F';
  components: ScoreComponents;
  suggestions: SuggestionItem[];
  isLoading: boolean;
}

interface ScoreComponents {
  termCoverage:     { score: number; maxScore: 35; terms: TermSuggestion[] };
  wordCount:        { score: number; maxScore: 20; current: number; min: number; max: number };
  headingStructure: { score: number; maxScore: 15; h1: boolean; h2Count: number; h3Count: number };
  titleTag:         { score: number; maxScore: 10; hasKeyword: boolean; length: number };
  metaDescription:  { score: number; maxScore: 10; hasKeyword: boolean; length: number };
  readability:      { score: number; maxScore: 10; fleschKincaid: number; benchmarkGrade: number };
}

interface SuggestionItem {
  component: string;
  pointValue: number;
  actionText: string;
  currentValue: string | number;
  targetValue: string | number;
}

// SerpCompetitorPanel
interface SerpCompetitorPanelProps {
  keyword: string;
  location: string;
  competitors: CompetitorRow[];
  isLoading: boolean;
}

interface CompetitorRow {
  position: number;
  url: string;
  domain: string;
  wordCount: number;
  score: number | null;
  h2Count: number;
  h3Count: number;
}

// RankHistoryChart
interface RankHistoryChartProps {
  data: RankDataPoint[];
  keyword: string;
}

interface RankDataPoint {
  date: string;
  position: number | null;
  impressions: number;
  clicks: number;
}

// SubscriptionGate — wraps any feature that requires a paid tier
interface SubscriptionGateProps {
  requiredTier: 'starter' | 'professional' | 'team' | 'agency';
  currentTier: 'none' | 'starter' | 'professional' | 'team' | 'agency';
  children: React.ReactNode;
  featureName: string;
}

// GscConnectButton
interface GscConnectButtonProps {
  projectId: string;
  isConnected: boolean;
  onConnected: () => void;
}
```

### SignalR WebSocket Client Hook

```typescript
// hooks/useContentScoring.ts

import { useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { ScoreUpdateMessage } from '@/types/scoring';

export function useContentScoring(
  documentId: string,
  onScoreUpdate: (update: ScoreUpdateMessage) => void
) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.NEXT_PUBLIC_API_URL}/hubs/seo-scoring`, {
        accessTokenFactory: () => getAccessToken(),
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ScoreUpdate', (update: ScoreUpdateMessage) => {
      onScoreUpdate(update);
    });

    connection.on('BenchmarkRefreshing', () => {
      // Show "refreshing benchmarks" indicator in ScoreSidebar
    });

    connection.start()
      .then(() => connection.invoke('JoinDocument', documentId))
      .catch(console.error);

    connectionRef.current = connection;

    return () => {
      connection.invoke('LeaveDocument', documentId).catch(() => {});
      connection.stop();
    };
  }, [documentId, onScoreUpdate]);

  const notifyContentChanged = useCallback(
    (contentHtml: string, targetKeyword: string) => {
      if (debounceRef.current) clearTimeout(debounceRef.current);

      debounceRef.current = setTimeout(() => {
        connectionRef.current?.invoke(
          'ContentChanged', documentId, contentHtml, targetKeyword
        ).catch(console.error);
      }, 800);
    },
    [documentId]
  );

  const notifyKeywordChanged = useCallback(
    (newKeyword: string, location: string) => {
      connectionRef.current?.invoke(
        'KeywordChanged', documentId, newKeyword, location
      ).catch(console.error);
    },
    [documentId]
  );

  return { notifyContentChanged, notifyKeywordChanged };
}
```

---

## 7. Content Scoring Algorithm Summary

Full specification is in `geekseo-content-scoring-spec.md`. Summary:

**Input:** HTML content string + target keyword + location string

**Process:**
1. Fetch SERP results for keyword + location via ISerpProvider (cache key: `{keyword}:{location}:{languageCode}`, TTL 24 hours in `seo_serp_results`)
2. For each organic result URL, check `seo_competitor_pages` cache (TTL 72 hours). On cache miss, call ICrawlerProvider.CrawlPageAsync. Store result.
3. IAIProvider extracts semantic terms from each competitor page text
4. Calculate benchmarks: avg word count, avg H2 count, term frequency ranges across top 10 competitors
5. Run 6-component scoring against benchmarks (see spec for formula)
6. Generate prioritized suggestion list (sorted by point value descending)

**Output:** `ContentScoreResult` with total score 0-100, letter grade (A-F), per-component breakdown, and ordered suggestion array

**Caching:** SERP results cached 24 hours. Competitor pages cached 72 hours. During a writing session, only the first content score request triggers external API calls. All subsequent scoring during the same session uses cached benchmarks.

---

## 8. PayPal Integration

### Subscription Plans

| Tier | Monthly Price | PayPal Plan ID (env var) |
|---|---|---|
| Starter | $29/month | `PAYPAL_PLAN_ID_STARTER` |
| Professional | $59/month | `PAYPAL_PLAN_ID_PROFESSIONAL` |
| Team | $89/month | `PAYPAL_PLAN_ID_TEAM` |
| Agency | $149/month | `PAYPAL_PLAN_ID_AGENCY` |

PayPal subscription plan IDs are created in the PayPal Developer Dashboard and stored as environment variables. Never hardcode them.

### Checkout Flow

```
1. User visits /pricing, selects a tier
2. PayPal JS SDK renders the PayPal button (client-side only, no server call yet)
3. User clicks the PayPal button → PayPal popup opens
4. createSubscription callback fires:
   → POST /api/seo/subscription/confirm is NOT called here
   → Return the PayPal plan ID for the selected tier
   → PayPal creates the subscription in their system
5. onApprove callback fires with { subscriptionID }:
   → POST /api/seo/subscription/confirm { subscriptionId, tier }
   → Backend verifies subscription state via PayPal API before storing
   → Status stored as 'pending_webhook'
6. PayPal fires BILLING.SUBSCRIPTION.ACTIVATED webhook within seconds:
   → POST /api/seo/subscription/webhook
   → Backend updates status to 'active'
   → Features become available immediately
```

### Webhook Handler Requirements

The webhook endpoint must be unauthenticated (PayPal calls it, not the user). Validate PayPal webhook signatures using PayPal's verification API to prevent spoofing.

```csharp
// SubscriptionController.cs — Webhook method

[HttpPost("webhook"), AllowAnonymous]
public async Task<IActionResult> Webhook([FromBody] JsonElement payload, CancellationToken ct)
{
    // Verify PayPal webhook signature
    var webhookId = Environment.GetEnvironmentVariable("PAYPAL_WEBHOOK_ID") ?? "";
    var headers = new PayPalWebhookHeaders(
        transmissionId: Request.Headers["PAYPAL-TRANSMISSION-ID"],
        timestamp: Request.Headers["PAYPAL-TRANSMISSION-TIME"],
        certUrl: Request.Headers["PAYPAL-CERT-URL"],
        authAlgo: Request.Headers["PAYPAL-AUTH-ALGO"],
        transmissionSig: Request.Headers["PAYPAL-TRANSMISSION-SIG"]
    );

    var rawBody = await new StreamReader(Request.Body).ReadToEndAsync(ct);
    var isValid = await subscriptionService.VerifyWebhookSignatureAsync(webhookId, headers, rawBody, ct);
    if (!isValid) return Unauthorized();

    var eventType = payload.GetProperty("event_type").GetString();
    var webhookEvent = new PayPalWebhookEvent(eventType!, payload);
    var result = await subscriptionService.HandleWebhookEventAsync(webhookEvent, ct);

    return result.IsSuccess ? Ok() : StatusCode(500);
}
```

### Feature Gate Tiers

| Feature | Starter ($29) | Professional ($59) | Team ($89) | Agency ($149) |
|---|---|---|---|---|
| Content editor | 20 docs/mo | 60 docs/mo | 150 docs/mo | Unlimited |
| Real-time scoring | Yes | Yes | Yes | Yes |
| Content briefs | 20/mo | 60/mo | 150/mo | Unlimited |
| AI writing (outline) | Yes | Yes | Yes | Yes |
| AI writing (full draft) | 5/mo | 20/mo | 50/mo | Unlimited |
| Keyword research | 50 lookups/mo | 200/mo | 500/mo | Unlimited |
| Keyword clustering | Yes | Yes | Yes | Yes |
| Site-wide audit | No | 1 site | 3 sites | Unlimited |
| Single-page audit | 5/mo | 20/mo | Unlimited | Unlimited |
| GSC + rank tracking | No | Yes | Yes | Yes |
| PDF reports | No | Yes | Yes | Yes |
| Re-crawl alerts | No | Yes | Yes | Yes |

---

## 9. GSC Integration

### OAuth 2.0 Consent Flow

Required scope: `https://www.googleapis.com/auth/webmasters.readonly`

The GscService constructs the authorization URL, stores a CSRF token (base64-encoded `userId:projectId:csrfNonce` in the state parameter), and handles the callback. On successful callback, the code is exchanged for tokens via a POST to `https://oauth2.googleapis.com/token`.

### Token Storage

The refresh token must never be stored in plaintext. AES-256-GCM encryption:

```csharp
// GeekApplication/Services/Seo/GscService.cs — EncryptRefreshToken

private (byte[] CipherText, byte[] IV, byte[] Tag) EncryptRefreshToken(string refreshToken)
{
    var key = Convert.FromBase64String(
        Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
        ?? throw new InvalidOperationException("ENCRYPTION_KEY not set"));

    using var aes = new AesGcm(key, 16);  // 128-bit tag
    var plaintext = Encoding.UTF8.GetBytes(refreshToken);
    var iv = new byte[12];       // 96-bit nonce for GCM
    var ciphertext = new byte[plaintext.Length];
    var tag = new byte[16];

    RandomNumberGenerator.Fill(iv);
    aes.Encrypt(iv, plaintext, ciphertext, tag);

    return (ciphertext, iv, tag);
}

private string DecryptRefreshToken(byte[] ciphertext, byte[] iv, byte[] tag)
{
    var key = Convert.FromBase64String(
        Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
        ?? throw new InvalidOperationException("ENCRYPTION_KEY not set"));

    using var aes = new AesGcm(key, 16);
    var plaintext = new byte[ciphertext.Length];
    aes.Decrypt(iv, ciphertext, tag, plaintext);

    return Encoding.UTF8.GetString(plaintext);
}
```

### Daily GSC Sync (IHostedService)

```csharp
// GeekAPI/BackgroundServices/GscSyncService.cs

public sealed class GscSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<GscSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once at startup, then every 24 hours
        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = GetNextRunTime();
            await Task.Delay(nextRun, stoppingToken);

            await SyncAllProjectsAsync(stoppingToken);
        }
    }

    private async Task SyncAllProjectsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var gscRepo = scope.ServiceProvider.GetRequiredService<IGscRepository>();
        var rankService = scope.ServiceProvider.GetRequiredService<IRankTrackingService>();

        // Get all active GSC connections across all users
        // For each: decrypt token, call GSC API, store results
        // Failures are logged per-project but do not stop the job
    }

    private static TimeSpan GetNextRunTime()
    {
        var now = DateTimeOffset.UtcNow;
        var nextRun = now.Date.AddDays(1).AddHours(2); // 2 AM UTC daily
        return nextRun - now;
    }
}
```

### GSC API Call Pattern

```csharp
// GeekApplication/Services/Seo/GscService.cs — FetchSearchAnalyticsAsync

public async Task<Result<IReadOnlyList<GscDataPoint>>> FetchSearchAnalyticsAsync(
    Guid projectId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
{
    var connectionResult = await _gscRepository.GetConnectionAsync(projectId, ct);
    if (!connectionResult.IsSuccess) return Result<IReadOnlyList<GscDataPoint>>.NotFound("GSC not connected");

    var connection = connectionResult.Value!;
    var refreshToken = DecryptRefreshToken(
        connection.EncryptedRefreshToken,
        connection.EncryptionIv,
        connection.EncryptionTag);

    // Exchange refresh token for access token
    var accessToken = await GetAccessTokenAsync(refreshToken, ct);

    // Call Search Analytics API
    var requestBody = new
    {
        startDate = startDate.ToString("yyyy-MM-dd"),
        endDate = endDate.ToString("yyyy-MM-dd"),
        dimensions = new[] { "query", "page", "date" },
        rowLimit = 25000,
        startRow = 0
    };

    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

    var response = await http.PostAsJsonAsync(
        $"https://www.googleapis.com/webmasters/v3/sites/{Uri.EscapeDataString(connection.SiteUrl)}/searchAnalytics/query",
        requestBody, ct);

    response.EnsureSuccessStatusCode();
    // Deserialize and map to IReadOnlyList<GscDataPoint>
    // Each row: keys[0]=query, keys[1]=page, keys[2]=date, clicks, impressions, ctr, position
}
```

---

## 10. Flat Implementation Steps

These steps are ordered for maximum forward momentum with no blocked dependencies. Each step is concrete enough to execute without additional architectural decisions.

1. Create `geek_seo` schema in Supabase instance `mpnruwauxsqbrxvlksnf`. Run the full DDL from Section 4. Verify all 14 tables exist with correct columns and indexes.

2. Create `GeekApplication/Interfaces/Seo/` directory. Add all six provider interfaces: `IAIProvider.cs`, `ISerpProvider.cs`, `IKeywordProvider.cs`, `ICrawlerProvider.cs`, `IRichTextProvider.cs`, `IPdfProvider.cs` with all request/response records from Section 3.

3. Create `GeekApplication/Interfaces/Seo/` service interfaces: `IContentScoringService.cs`, `IContentBriefService.cs`, `IAIWritingService.cs`, `IKeywordResearchService.cs`, `ISiteAuditService.cs`, `IPageAuditService.cs`, `IRankTrackingService.cs`, `IReportService.cs`, `ISubscriptionService.cs`, `IGscService.cs`.

4. Create `GeekApplication/Interfaces/Seo/` repository interfaces: `IContentDocumentRepository.cs`, `IKeywordRepository.cs`, `ISerpCacheRepository.cs`, `IAuditRepository.cs`, `IRankRepository.cs`, `ISubscriptionRepository.cs`, `IGscRepository.cs`.

5. Create `GeekApplication/Models/Seo/` with all domain model records: `ContentDocument`, `ContentBrief`, `ContentScoreResult`, `SerpBenchmarks`, `KeywordResult`, `KeywordCluster`, `PageContent`, `SiteAuditReport`, `SiteAuditStatus`, `RankSnapshot`, `GscDataPoint`, `GscConnection`, `SubscriptionRecord`, `PayPalWebhookEvent`.

6. Add `SubscriptionTier` enum to `GeekApplication/Constants/Seo/SubscriptionTier.cs`.

7. Create `GeekRepository/Repositories/Seo/` directory. Implement `ContentDocumentRepository` using Dapper. All methods return `Result<T>`. Connection via `IDbConnectionFactory` (same pattern as existing auth repositories).

8. Implement `KeywordRepository` in `GeekRepository/Repositories/Seo/`. Include `BulkUpsertAsync` using Dapper's `ExecuteAsync` with a list parameter.

9. Implement `SerpCacheRepository` in `GeekRepository/Repositories/Seo/`. The `UpsertAsync` method uses `INSERT ... ON CONFLICT (keyword, location, language_code) DO UPDATE SET ...`. Include `DeleteExpiredAsync` for cache cleanup.

10. Implement `AuditRepository` in `GeekRepository/Repositories/Seo/`. Site audit pages use batch insert via Dapper with a loop (no bulk Dapper extension required — Supabase handles it fine).

11. Implement `RankRepository` in `GeekRepository/Repositories/Seo/`. The `BulkUpsertAsync` uses `INSERT ... ON CONFLICT (project_id, keyword, date) DO UPDATE SET position = EXCLUDED.position, impressions = EXCLUDED.impressions, clicks = EXCLUDED.clicks`.

12. Implement `SubscriptionRepository` in `GeekRepository/Repositories/Seo/`. The `GetActiveAsync` query filters on `status IN ('active', 'pending_webhook')` and orders by `created_at DESC LIMIT 1`.

13. Implement `GscRepository` in `GeekRepository/Repositories/Seo/`. The `UpsertConnectionAsync` stores `encrypted_refresh_token` as `BYTEA` using Dapper's `byte[]` parameter.

14. Create `GeekRepository/Providers/Seo/ClaudeProvider.cs`. Implement `IAIProvider` using the official Anthropic `anthropic-sdk` NuGet package. Include prompt caching headers on system prompts (`cache_control: { type: "ephemeral" }`). Model defaults to `claude-sonnet-4-5`.

15. Create `GeekRepository/Providers/Seo/DataForSEOSerpProvider.cs`. Implement `ISerpProvider` using the official `dataforseo/CSharpClient` from GitHub. Use the Live SERP endpoint for real-time results. API credentials from environment variables `DATAFORSEO_LOGIN` and `DATAFORSEO_PASSWORD`.

16. Create `GeekRepository/Providers/Seo/DataForSEOKeywordProvider.cs`. Implement `IKeywordProvider` using the same DataForSEO C# client. Use Keywords Data API for volume/CPC and Labs API for difficulty. Include `GetKeywordSuggestionsAsync` via the Keywords for Keywords endpoint (up to 20 seed terms, returns up to 20,000 suggestions).

17. Create `GeekRepository/Providers/Seo/PlaywrightCrawlerProvider.cs`. Implement `ICrawlerProvider`. Install `Microsoft.Playwright` NuGet package. `CrawlPageAsync` launches a headless Chromium instance, navigates to the URL, extracts: `document.title`, `meta[name=description]`, all heading tags, `document.body.innerText`, all `<a>` links, all `<img alt>` attributes, JSON-LD script tags, link canonical. Respects `robots.txt` via `IsAllowedByRobotsTxtAsync`.

18. Create `GeekRepository/Providers/Seo/TipTapProvider.cs`. Implement `IRichTextProvider`. Use `HtmlAgilityPack` NuGet for HTML parsing. `ExtractPlainText` strips all tags. `ExtractHeadings` selects all h1-h6 nodes. `CountWords` splits `innerText` by whitespace.

19. Create `GeekRepository/Providers/Seo/PuppeteerProvider.cs`. Implement `IPdfProvider`. Use `PuppeteerSharp` NuGet. `GeneratePdfAsync` launches headless Chromium, sets page content from `PdfRequest.HtmlContent`, calls `page.PdfAsync()` with A4/Letter format, returns bytes.

20. Register all providers and repositories in `GeekRepository/ServiceCollectionExtensions.cs`:
    ```csharp
    services.AddScoped<IAIProvider, ClaudeProvider>();
    services.AddScoped<ISerpProvider, DataForSEOSerpProvider>();
    services.AddScoped<IKeywordProvider, DataForSEOKeywordProvider>();
    services.AddScoped<ICrawlerProvider, PlaywrightCrawlerProvider>();
    services.AddScoped<IRichTextProvider, TipTapProvider>();
    services.AddScoped<IPdfProvider, PuppeteerProvider>();
    services.AddScoped<IContentDocumentRepository, ContentDocumentRepository>();
    // ... all other repositories
    ```

21. Create `GeekApplication/Services/Seo/ContentScoringService.cs`. Implement `IContentScoringService`. `GetOrFetchBenchmarksAsync` checks `ISerpCacheRepository`, calls `ISerpProvider` on miss, then calls `ICrawlerProvider` for any competitor URLs not in `seo_competitor_pages` cache. `ScoreAsync` runs the full 6-component algorithm from `geekseo-content-scoring-spec.md`.

22. Create `GeekApplication/Services/Seo/ContentBriefService.cs`. Implement `IContentBriefService`. `GenerateBriefAsync` calls `ISerpProvider.GetSerpResultsAsync`, crawls top `competitorCount` pages via `ICrawlerProvider`, aggregates headings/questions/topics, then calls `IAIProvider` with prompt: "You are an SEO content strategist. Based on the following SERP analysis for the keyword '{keyword}', generate a comprehensive content brief including: recommended title, meta description, H1, H2 outline with 8-12 headings, key questions to answer, important terms to include, recommended word count, and content angle. SERP data: {serpJson}".

23. Create `GeekApplication/Services/Seo/AIWritingService.cs`. Implement `IAIWritingService`. `GenerateOutlineAsync` calls `IAIProvider` with the content brief context. `GenerateDraftAsync` calls `IAIProvider` with the outline and a word count target, instructing Claude to write complete paragraphs for each section. `OptimizeContentAsync` calls `IAIProvider` with the current content, score, and top 5 suggestions, asking for specific additions/rewrites to improve the score.

24. Create `GeekApplication/Services/Seo/KeywordResearchService.cs`. Implement `IKeywordResearchService`. `ResearchAsync` checks `IKeywordRepository` cache first (TTL 7 days), calls `IKeywordProvider.GetKeywordDataAsync` on miss, stores results. `ClusterAsync` calls `IKeywordProvider.ClusterKeywordsAsync` and stores clusters in `seo_keyword_clusters`.

25. Create `GeekApplication/Services/Seo/SiteAuditService.cs`. Implement `ISiteAuditService`. `StartAuditAsync` inserts a record with `status: "pending"`, enqueues a background task via `IBackgroundTaskQueue`, returns the new `auditId`. The background worker calls `ICrawlerProvider.CrawlSiteAsync`, then `IPageAuditService.AuditPageFromContentAsync` for each page, updates progress.

26. Create `GeekApplication/Services/Seo/PageAuditService.cs`. Implement `IPageAuditService`. `AuditPageAsync` calls `ICrawlerProvider.CrawlPageAsync`, then calls the scoring logic. `AuditPageFromContentAsync` accepts an already-crawled `PageContent` and runs the 20-point on-page checklist (see site audit flow in Section 2). Each issue has a `severity` field: `critical`, `warning`, or `info`.

27. Create `GeekApplication/Services/Seo/RankTrackingService.cs`. Implement `IRankTrackingService`. `SyncGscDataAsync` calls `IGscService.FetchSearchAnalyticsAsync` for yesterday's data and calls `IRankRepository.BulkUpsertAsync`. `GetRankHistoryAsync` queries `IRankRepository` and returns sorted by date.

28. Create `GeekApplication/Services/Seo/ReportService.cs`. Implement `IReportService`. `GenerateSiteAuditReportAsync` builds an HTML string from the audit data, calls `IPdfProvider.GeneratePdfAsync`, stores the PDF bytes to Supabase Storage bucket `seo-reports`, stores the file path in `seo_reports`. `GetReportDownloadUrlAsync` generates a signed URL from Supabase Storage.

29. Create `GeekApplication/Services/Seo/SubscriptionService.cs`. Implement `ISubscriptionService`. `GetActiveTierAsync` calls `ISubscriptionRepository.GetActiveAsync` and maps the `tier` string to `SubscriptionTier` enum. `ConfirmSubscriptionAsync` calls PayPal's GET subscription API to verify status before inserting. `HandleWebhookEventAsync` switches on `event_type` and calls the appropriate repository update. `IsFeatureAllowed` uses a static dictionary of `(SubscriptionTier, string feature) → bool`.

30. Create `GeekApplication/Services/Seo/GscService.cs`. Implement `IGscService`. `GetAuthorizationUrlAsync` builds the Google OAuth URL with CSRF state. `HandleCallbackAsync` validates state, exchanges code, encrypts refresh token, upserts in `IGscRepository`. `FetchSearchAnalyticsAsync` decrypts token, refreshes access token, calls GSC API.

31. Register all application services in `GeekApplication/ServiceCollectionExtensions.cs`:
    ```csharp
    services.AddScoped<IContentScoringService, ContentScoringService>();
    services.AddScoped<IContentBriefService, ContentBriefService>();
    // ... all other services
    ```

32. Create `GeekAPI/Hubs/SeoContentScoringHub.cs` as specified in Section 5. Register SignalR hub in `GeekAPI/Program.cs`: `app.MapHub<SeoContentScoringHub>("/hubs/seo-scoring")`.

33. Create `GeekAPI/Middleware/SeoFeatureGateMiddleware.cs` as specified in Section 5. Register in `GeekAPI/Program.cs` before `app.UseEndpoints`.

34. Create all 10 SEO controllers in `GeekAPI/Controllers/Seo/` as specified in Section 5. Each controller is thin — validates the request, calls the service, maps `Result<T>` to HTTP response (`IsSuccess` → 200/201/204, `NotFound` → 404, `Failure` → 500).

35. Create `GeekAPI/BackgroundServices/GscSyncService.cs` as specified in Section 9. Register as `IHostedService` in `GeekAPI/Program.cs`.

36. Create `GeekAPI/BackgroundServices/SiteAuditWorker.cs` — a hosted service that dequeues site audit jobs from `IBackgroundTaskQueue` and executes them. The queue is an `IHostedService` with a `Channel<Func<CancellationToken, Task>>` backing it.

37. Add environment variables to Railway GeekBackend service:
    - `DATAFORSEO_LOGIN`, `DATAFORSEO_PASSWORD`
    - `ANTHROPIC_API_KEY`
    - `PAYPAL_CLIENT_ID`, `PAYPAL_CLIENT_SECRET`, `PAYPAL_WEBHOOK_ID`
    - `PAYPAL_PLAN_ID_STARTER`, `PAYPAL_PLAN_ID_PROFESSIONAL`, `PAYPAL_PLAN_ID_TEAM`, `PAYPAL_PLAN_ID_AGENCY`
    - `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`
    - `RESEND_API_KEY`

38. Create the Next.js frontend project at `/Volumes/Seagate/development/Geek-SEO/frontend/`. Run `npx create-next-app@latest geekseo --typescript --tailwind --app`. Install: `@microsoft/signalr`, `@tiptap/react`, `@tiptap/starter-kit`, `@tiptap/extension-*` (required extensions), `recharts`, `@paypal/react-paypal-js`, `react-hook-form`, `zod`, `@hookform/resolvers`, `motion`, `resend`.

39. Configure `tailwind.config.ts` for Tailwind v4. Install shadcn: `npx shadcn-ui@latest init`. Add components: `button`, `card`, `dialog`, `dropdown-menu`, `input`, `label`, `progress`, `select`, `separator`, `sheet`, `skeleton`, `slider`, `switch`, `table`, `tabs`, `textarea`, `toast`, `tooltip`.

40. Implement `GeekBackend OAuth 2.1 PKCE` authentication flow in the frontend. Store access token in memory (not localStorage). Refresh token in httpOnly cookie. Middleware in `(app)` layout validates session on every authenticated route and redirects to `/login` if token is invalid or expired.

41. Create the `ContentEditorPage` at `/app/content/[id]`. Initialize TipTap editor. Wire `onUpdate` to `notifyContentChanged` from the `useContentScoring` hook (800ms debounce is inside the hook, not in the component). Layout: 70% editor, 30% `ScoreSidebar`. `ScoreSidebar` renders an animated score ring (SVG), letter grade, per-component mini progress bars, and a sorted suggestion list.

42. Create the `SerpCompetitorPanel` component. It fetches `GET /api/seo/serp?keyword=...&location=...` and renders a table of top 10 competitor pages with position, domain, word count, estimated score, and heading counts. Accessible below the editor or in a slide-out sheet on mobile.

43. Create the `KeywordResearchPage`. The search input calls `POST /api/seo/keywords/research`. Results render in a sortable table with columns: keyword, volume, difficulty (color-coded bar), CPC, SERP features. A "Cluster Keywords" button sends selected rows to `POST /api/seo/keywords/cluster` and renders the cluster view.

44. Create the `SiteAuditListPage` and `SiteAuditResultsPage`. List page shows all audits with status badge. Results page shows overall score gauge (Recharts `RadialBarChart`), issues grouped in tabs by severity (critical/warning/info), and a full sortable table of pages with their individual scores.

45. Create the `RankTrackingPage`. A `Recharts LineChart` shows position over time for the selected keyword (Y-axis inverted: position 1 at top). A summary cards row shows: impressions (7-day), clicks (7-day), average position, CTR. A table lists all tracked keywords with last position, position delta (up/down arrow), and weekly impressions.

46. Create the `SettingsPage` with three tabs: Account (name, email, password), GSC (`GscConnectButton` + connection status), Subscription (current plan display, `SubscriptionGate`-wrapped upgrade buttons, cancel link). The cancel link calls `DELETE /api/seo/subscription` and shows a confirmation dialog before proceeding.

47. Create PayPal subscription flow. Load `@paypal/react-paypal-js` `PayPalScriptProvider` in the pricing page. Render four `PayPalButtons` components, one per tier. Each button's `createSubscription` returns the corresponding PayPal plan ID from environment variable. `onApprove` calls `POST /api/seo/subscription/confirm`.

48. Create `AlertsService` in `GeekApplication/Services/Seo/AlertsService.cs`. Runs as a daily hosted service. For each alert in `seo_alerts` where `enabled = true`: check condition (rank dropped by threshold, audit completed, etc.), call `Resend` API if condition is met and `last_sent_at` is more than 24 hours ago, update `last_sent_at`.

49. Create the landing page at `/`. Sections: Hero with animated content editor mockup, Features grid (6 cards), Competitive positioning table vs. Surfer/Frase/NeuronWriter, Transparent scoring explainer (animated breakdown diagram), Pricing section (four tiers), Social proof section (dogfood GSC screenshots showing ranking improvements), FAQ, CTA footer. Build this after at least one real geekatyourspot.com article has been scored and improved — use real screenshot evidence.

50. Set up Google Search Console credentials. Create a project in Google Cloud Console. Enable the `Google Search Console API`. Create OAuth 2.0 credentials (Web application type). Set the authorized redirect URI to `{API_BASE_URL}/api/seo/gsc/callback`. Store `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` in Railway environment variables.

51. Create the four PayPal subscription plans in the PayPal Developer Dashboard (sandbox first). Each plan: billing cycle = monthly, fixed price matching the tier. Note the plan IDs and store in Railway environment variables. Create the webhook endpoint pointing to `{API_BASE_URL}/api/seo/subscription/webhook`. Subscribe to all four webhook event types. Note the `PAYPAL_WEBHOOK_ID`.

52. Write Playwright end-to-end tests for the critical path: (a) register account → verify email → login, (b) create project → create content document → receive score update via WebSocket, (c) keyword research → view results, (d) connect GSC → verify connection stored. Tests run against a local dev stack.

53. Deploy GeekBackend to Railway with all new environment variables set. Run `dotnet ef migrations script > seo_migrations.sql` to generate the migration SQL for the new repositories (SEO tables use Dapper + raw DDL, not EF migrations, so apply the DDL from Step 1 directly on Supabase). Verify `/health` endpoint returns 200.

54. Deploy Next.js frontend to Vercel. Set environment variables: `NEXT_PUBLIC_API_URL` (Railway GeekBackend URL), `NEXT_PUBLIC_PAYPAL_CLIENT_ID`, `NEXTAUTH_SECRET`, `NEXTAUTH_URL`. Verify the full auth flow works end-to-end (login → dashboard → content editor → SignalR WebSocket connects).

55. Internal dogfood run on `geekatyourspot.com`. Create 5 content documents targeting local keywords (e.g., "web design Boca Raton", "WordPress developer Broward County"). Score each document, apply suggestions, publish to WordPress, connect GSC, and wait 2-4 weeks for ranking data. Document the before/after GSC impressions/position in a spreadsheet that feeds the landing page proof section.

# SERP Data & Keyword Data API Comparison
# Geek SEO — AI-Powered SEO SaaS (MVP Research)

**Last Updated:** May 2026
**Purpose:** Evaluate SERP and keyword data API providers for an AI-powered SEO content optimization SaaS (Surfer SEO competitor), built by a solo/small team targeting small businesses. Budget constraints and free tiers are primary decision factors.

---

## Critical Industry Alert (May 2026)

**Google v. SerpApi — Active Federal Lawsuit**

Google filed suit against SerpApi in December 2025 under the DMCA, alleging circumvention of its "SearchGuard" anti-scraping system. SerpApi filed a motion to dismiss; the hearing is scheduled for May 19, 2026. If Google prevails, the ruling could threaten the entire third-party SERP scraping industry — potentially affecting SerpApi, Serper.dev, Zenserp, Scale SERP, ValueSERP, and similar services. DataForSEO has not been named in the suit.

This is not FUD — it is an active proceeding in the U.S. District Court for the Northern District of California. Evaluate legal risk as part of provider selection for any production SaaS.

---

## Part 1: SERP Data APIs

### Comparison Table

| Provider | Free Tier | Entry Price | Cost/1K Queries (standard) | Real-Time? | Async Queue? | C# SDK | Node.js SDK | Legal Risk | Notable Weakness |
|---|---|---|---|---|---|---|---|---|---|
| **DataForSEO** | $1 credit on signup | $50 min deposit | $0.60/1K (standard queue) | Yes ($2.00/1K) | Yes ($0.60/1K, ~5 min) | Official | Official (npm) | Low | Async model adds UX latency; complex pricing tiers |
| **Serper.dev** | 2,500 free credits (one-time) | $50 for 50K credits ($1.00/1K) | $0.30/1K at scale | Yes (~1-2 sec) | No | No official | No official | Medium (scraping) | Thin SDK ecosystem; no .NET support |
| **SerpApi** | 100 searches/month (no expiry) | $75/mo for 5,000 searches | $15/1K at Developer tier | Yes | No | No official | Official | High (active Google lawsuit) | Most expensive option; credits expire monthly; active lawsuit |
| **ValueSERP / TrajectData** | 100 free searches (trial) | ~$40/mo for 25,000 searches ($1.60/1K) | $1.00–$2.59/1K depending on plan | Yes | Yes (batch) | No official | No official | Medium (scraping) | Rebranding to TrajectData creates documentation confusion |
| **Scale SERP / TrajectData** | 100 free searches (trial) | ~$19/mo (limited) | $0.29/1K at high volume | Yes | Yes | No official | No official | Medium (scraping) | Same TrajectData rebrand issues; Scale SERP is high-volume tier |
| **Bright Data** | No | $10/mo (micro package) | $1.50/1K (PAYG), ~$1.30/1K on plan | Yes | No | No official | No official | Low (proxy infra, not scraping) | Complex pricing; expensive for low volume; enterprise-focused |
| **Zenserp** | 50 requests/month | $29.99/mo for 5,000 searches ($6.00/1K) | $3.40/1K at $169.99/mo tier | Yes | No | No official | No official | Medium (scraping) | Most expensive per-query of small providers; smaller team |
| **Brave Search API** | ~$5/mo credit (~1K searches) | $5/1K (same on all tiers) | $5.00/1K (flat) | Yes | No | No official | No official | None (first-party) | Not Google data — different result set; not suitable for Google-rank-focused tools |

---

### Provider Deep Dives — SERP APIs

#### 1. DataForSEO

**Pricing (pay-as-you-go — no subscription required):**
- Standard Queue (batch, ~5 min turnaround): **$0.60/1K queries** ($0.0006/query)
- Priority Queue (~1 min): **$1.20/1K**
- Live Mode (real-time, ~2 sec): **$2.00/1K** ($0.002/query)
- Minimum deposit: $50. No monthly fee. $1 credit given on signup for testing.

**Data fields returned:**
Organic results (position, title, URL, snippet, sitelinks, rich_snippet, ratings, thumbnail), Featured Snippets (title, featured_title, table content), People Also Ask (questions, expandable answers, seed_question parameter), Knowledge Graph, Maps pack, Images, News, Shopping, Video, Related Searches, AI Overviews (Bing), and 30+ additional SERP feature types. The "Advanced" endpoint returns the full set; "Regular" returns a leaner payload.

**Geographic coverage:** 50,000+ locations, 1,000+ languages, all major Google country domains.

**Data freshness:** Live pull from Google on every request (no cache older than the request itself).

**Rate limits:** No published hard rate limits; scales to enterprise volume.

**SDK support:**
- C#/.NET: Official C# client on GitHub (`dataforseo/CSharpClient`)
- Node.js/TypeScript: Official npm package (`dataforseo-client`); also a TypeScript MCP server (`dataforseo/mcp-server-typescript`)
- Code examples in curl, PHP, Python, Node.js, C# in all documentation

**Reliability:** Established provider, widely used in production SEO tooling. No named involvement in Google lawsuit.

**Weaknesses:**
- The standard queue's 5-minute async model is unsuitable for user-facing real-time features (switch to Live at 3.3x cost)
- Pricing page has many sub-products; easy to accidentally use a more expensive endpoint
- $50 minimum initial deposit (reasonable but worth noting)

**Additional APIs relevant to an SEO SaaS:**
- Keywords Data API (search volume, CPC, competition) — covered in Part 2
- DataForSEO Labs API (keyword difficulty, competitor analysis)
- On-Page API (content analysis, page scoring)
- Backlinks API

---

#### 2. Serper.dev

**Pricing (credit packs — no subscription):**
- Free: 2,500 credits (one-time, never expires)
- Starter: $50 for 50,000 credits = **$1.00/1K**
- Standard: $375 for 500,000 credits = **$0.75/1K**
- Scale: $1,250 for 2.5M credits = **$0.50/1K**
- Ultimate: $3,750 for 12.5M credits = **$0.30/1K**
- Credits valid for 6 months from purchase

**Data fields returned:**
Organic results (position, title, link, snippet, date, sitelinks), Featured Snippets / Answer Box, People Also Ask (questions + snippets), Knowledge Graph, Related Searches, Top Stories, Images, News. Returns structured JSON in ~1-2 seconds.

**Geographic coverage:** gl (country) and hl (language) parameters; 100+ countries.

**Data freshness:** Live, sub-2 second response.

**Rate limits:** Up to 300 queries per second (documented); designed for AI agent workloads.

**SDK support:**
- No official .NET/C# SDK
- No official Node.js SDK
- REST API only; straightforward JSON; easy to wrap yourself

**Reliability:** Widely adopted for LLM/AI agent use cases. No formal uptime SLA published. Not named in Google lawsuit (as of May 2026).

**Weaknesses:**
- No official SDKs for any language; you build the HTTP wrapper
- Legal risk exists (scraping architecture, not named in lawsuit but same model)
- Less data richness than DataForSEO (no On-Page, backlinks, or keyword data in same ecosystem)
- Credits expire after 6 months

---

#### 3. SerpApi (serpapi.com)

**Pricing (monthly subscription — searches expire at cycle end, no rollover):**
- Free: 100 searches/month (permanent, no credit card)
- Developer: $75/mo for 5,000 searches = **$15.00/1K**
- Production: ~$130/mo for 15,000 searches = ~$8.67/1K
- Big Data: ~$275/mo for 30,000 searches = ~$9.17/1K
- Enterprise: $3,750/mo base + $0.008/additional search

**Data fields returned:**
Organic results (position, title, link, snippet, sitelinks inline/expanded, rich snippets, ratings, author, date, thumbnail, cached page link, favicon), Featured Snippets, People Also Ask (related_questions), Knowledge Graph, Ads (top + bottom), Images, News, Shopping, Video, Maps, Related Searches, and more. Very complete structured JSON.

**Geographic coverage:** 200+ country Google domains (google_domain parameter), city/region via location or uule parameter, device targeting (desktop/mobile/tablet).

**Data freshness:** Live, synchronous, sub-2 second.

**Rate limits:** Plan-specific guaranteed throughput (e.g., 1,000 searches/hour on 5K plan).

**SDK support:**
- Official SDKs: Python, Ruby, Node.js, Go, PHP
- No official .NET/C# SDK
- Third-party C# wrapper available on NuGet but not officially maintained by SerpApi

**Reliability:** Strongest reputation for documentation quality and response consistency. 4.8/5 on G2, 5.0/5 on Capterra across 64 reviews.

**Weaknesses:**
- Most expensive option: $15/1K at entry tier vs. $0.60/1K at DataForSEO
- Searches expire monthly — 0% rollover inflates effective cost for variable workloads
- Active federal lawsuit: Google v. SerpApi, DMCA claim, hearing May 19, 2026 — existential risk if Google prevails
- No C# SDK; no free tier above 100/month

---

#### 4. ValueSERP / Scale SERP (trajectdata.com)

**Background:** TrajectData rebranded from ValueSERP (budget tier) and Scale SERP (high-volume tier). Both legacy domains redirect to trajectdata.com.

**ValueSERP Pricing (monthly subscription):**
- Free trial: 100 searches (no credit card)
- Entry: ~$40/mo for 25,000 searches = ~$1.60/1K
- Volume: Scales down to ~$1.00/1K at higher tiers

**Scale SERP Pricing:**
- Entry: ~$19/mo (limited searches)
- Annual tiers: ~$66/mo (10K credits), ~$159/mo (50K), ~$479/mo (250K)
- Volume: as low as $0.29/1K at highest tiers

**Data fields returned:**
Organic results, Featured Snippets, People Also Ask, Knowledge Graph, Related Searches, Images, News, Shopping, Maps. Both support batch mode (up to 15,000 parallel searches).

**Geographic coverage:** Standard Google geo parameters.

**Data freshness:** Live, real-time results.

**Rate limits:** Batch supports 15,000 parallel searches.

**SDK support:** No official SDKs for any language. REST only.

**Weaknesses:**
- No official SDKs
- Rebrand/documentation consolidation creates confusion
- Smaller support team than DataForSEO or SerpApi
- Medium legal risk (same scraping model)

---

#### 5. Bright Data SERP API

**Pricing:**
- Pay-As-You-Go: $1.50/1K (no minimum)
- Micro: $10/mo (~5,500 requests at $1.80/1K effective)
- Growth: $499/mo (~217K requests at $2.30/1K effective)
- Business: $999/mo (~492K requests at $2.03/1K effective)
- Enterprise: Custom

**Data fields returned:** Standard organic SERP data, SERP features. Less structured data than DataForSEO out of the box; more of a raw HTML extraction layer with parsing.

**Geographic coverage:** 195+ countries, 150M+ IP pool (residential, ISP, datacenter, mobile). Strongest geo-coverage of all providers — best for hyper-local SERP data.

**Data freshness:** Live.

**Rate limits:** Unlimited concurrent requests.

**SDK support:** No official .NET or Node.js SDKs for SERP specifically. Enterprise-focused with custom integrations.

**Reliability:** Established, large infrastructure. Not named in Google lawsuit (proxy infrastructure, not scraping software).

**Weaknesses:**
- Expensive per-query vs. DataForSEO or Serper at small scale
- Complexity is enterprise-oriented; not MVP-friendly
- Pricing is less transparent; often negotiated
- Over-engineered for a small SEO SaaS MVP

---

#### 6. Zenserp

**Pricing (monthly subscription):**
- Free: 50 requests/month (permanent)
- Small: ~$29.99/mo for 5,000 searches = **$6.00/1K**
- Mid: ~$79.99/mo for 20,000 searches = **$4.00/1K**
- Large: ~$169.99/mo for 50,000 searches = **$3.40/1K**
- 20% discount on annual plans

**Data fields returned:** Organic results, Featured Snippets, Knowledge Panel, Related Searches, Ads. Basic SERP coverage; less comprehensive SERP feature support than DataForSEO or SerpApi.

**Geographic coverage:** Standard Google geo parameters.

**SDK support:** No official SDKs.

**Weaknesses:**
- Most expensive per-query of the small providers ($3.40–$6.00/1K vs. $0.60–$1.00/1K for DataForSEO/Serper)
- Small team / smaller infrastructure
- Thin feature set relative to cost
- Medium legal risk

---

#### 7. Brave Search API (honorable mention)

**Pricing:**
- Free: ~$5/month credit (roughly 1,000 searches)
- Standard: $5/1K searches (flat, no volume discount)

**Why it matters:** First-party search API — zero scraping, zero legal risk. Brave operates its own web index.

**Why it may not fit:** This returns Brave's index, not Google's. For an SEO SaaS where users want to optimize for Google rankings, Brave Search API is not a substitute for Google SERP data. However, it could be a supplementary signal source.

---

## Part 2: Keyword Data APIs

### Comparison Table

| Provider | Free Tier | Entry Price | Cost/1K Keywords | Data Points | Keyword Difficulty? | C# SDK | Note |
|---|---|---|---|---|---|---|---|
| **DataForSEO Keywords Data API** | $1 credit at signup | $50 min deposit | ~$0.05–$0.50/1K keywords | Volume, CPC, competition, trends, SERP features | Via Labs API | Official | Best value; Google Ads data source; 2B+ keyword DB |
| **DataForSEO Labs API** | Same | Same | ~$0.01–$0.50/1K | Difficulty, related keywords, SERP positions, competitor gap | Yes (bulk endpoint) | Official | Proprietary difficulty metric; paired with Keywords Data |
| **Keywords Everywhere** | No | ~$10 for 100K credits ($0.10/1K) | $0.10–$0.12/1K credits | Volume, CPC, competition, 12-month trends | No | No | Credits expire 1 year; API exists but browser-extension origin; limited bulk scale |
| **SE Ranking API** | 14-day trial (100K credits) | $318/year (~$26.50/mo) for 24M credits | ~$0.013/1K queries (10 credits/query) | Volume, difficulty, CPC, trends, SERP data | Yes | No | Best standalone affordable keyword API with difficulty; MCP server available |
| **Moz API** | No | ~$49/mo (API Level 1) | Varies by rows/plan | Volume, difficulty, CTR opportunity, priority | Yes (keyword difficulty) | No | Row-based; separate from Moz Pro subscription; primarily DA/PA/links reputation |
| **Ahrefs API** | No | $449/mo minimum (Advanced plan req.) | Varies by units | Volume, difficulty, clicks, SERP history | Yes | No | Prohibitively expensive for MVP; 50-unit minimum per request |
| **Semrush API** | No | Business plan req. ($499.95/mo) + unit purchase | ~10 units/keyword line ($50/M units) | Volume, difficulty, CPC, intent, trends | Yes | No | Requires $499.95/mo Business plan just to get API key; unit costs add on top |
| **Wordtracker API** | 10-day trial | ~$27–99/mo (Bronze–Gold) | Varies | Volume (Google + YouTube + Amazon + eBay), CPC | No | No | Unique multi-platform data (not just Google); affordable; no difficulty metric |
| **Google Ads Keyword Planner API** | Free (with restrictions) | Free | Free (with caveats) | Volume (rounded), CPC, competition | No | Official (Google Ads API) | Requires active Google Ads account with spend; data ranges not exact; 15K ops/day limit |

---

### Provider Deep Dives — Keyword Data APIs

#### 1. DataForSEO Keywords Data API + Labs API

**Keywords Data API:**
- Backed by Google Keyword Planner data (DataForSEO abstracts GKP limits)
- Search volume, CPC, competition, impressions for up to 1,000 keywords per request
- Keywords for Keywords endpoint: give 20 seed keywords, get up to 20,000 suggestions
- Pricing: ~$0.05–$0.50/1,000 keywords (depends on endpoint; live vs. batch)
- Database: 2B+ keywords enriched with PPC metrics
- Supports 200+ locations, multiple languages

**DataForSEO Labs API (keyword difficulty, competitive analysis):**
- Bulk Keyword Difficulty endpoint: up to 1,000 keywords per request
- Related keywords, SERP gap analysis, ranked keywords by domain
- Keyword difficulty is DataForSEO's proprietary metric based on SERP analysis
- Pricing: ~$0.01–$0.50/1,000 depending on endpoint

**Why it matters for MVP:** A single DataForSEO account gives you both SERP data and keyword data. You don't need a second vendor.

---

#### 2. SE Ranking API

**Pricing:**
- Standalone: $318/year (~$26.50/mo) for 24 million credits
- 14-day free trial with 100,000 credits (no credit card required)
- Simple SERP requests: 10 credits each = **$0.013/1K queries at the annual rate**

**Data available:** Keyword search volume, difficulty score, CPC, seasonal trends, 188+ regions, backlink data, domain analytics, rank tracking.

**Why it matters:** This is the most affordable all-in-one keyword data API with a difficulty metric and no requirement to buy an expensive SEO platform subscription. The MCP server integration with Claude/AI tools is a bonus for AI-assisted workflows.

**Weaknesses:** No C# SDK; keyword data not as deep as DataForSEO's Google Ads pipeline; smaller dataset than Ahrefs/Semrush.

---

#### 3. Keywords Everywhere API

**Pricing (credit-based, 1 credit = 1 keyword):**
- Bronze: ~$10 for 100,000 credits = **$0.10/1K**
- Silver: ~$45 for 500,000 = **$0.09/1K**
- Gold: ~$79 for 1M credits = **$0.079/1K**
- Platinum: ~$199 for 5M credits = **$0.040/1K**
- Credits valid 1 year

**Data:** Volume, CPC, competition, 12-month trend (Google, Bing, YouTube, Amazon).

**Weaknesses:** No keyword difficulty metric. API exists but the product is primarily a browser extension — bulk API usage patterns are not first-class. Watch real credit consumption: PAA and related search expansions on a single search page can burn 20-30 credits per lookup.

---

#### 4. Moz API

**Pricing:**
- API Level 1: ~$49/month (basic DA/PA, link data, limited keyword rows)
- Row-based model: each response row consumes from monthly row allocation
- Keyword data (volume, difficulty, CTR, priority) available; row limits reset monthly
- Moz Pro subscription does NOT automatically include API access

**Data:** Domain Authority, Page Authority, monthly search volume, keyword difficulty, organic CTR opportunity, priority scores.

**Weaknesses:** Primarily known for DA/PA link metrics, not keyword data depth. Row limits can be hit quickly on bulk operations. Pricing is opaque — not straightforward per-keyword cost.

---

#### 5. Ahrefs API

**Bottom line for bootstrapped builders:** Not viable for MVP.

- Lite ($129/mo) and Standard ($249/mo): NO API access
- Advanced ($449/mo): Required just to purchase separate API access
- Full API: Enterprise plan, $1,249/mo minimum
- API unit-based billing on top of plan cost; 50-unit minimum per request

**Use case:** Ahrefs API is for established SEO platforms or agencies doing high-volume programmatic analysis. Not for an MVP.

---

#### 6. Semrush API

**Bottom line for bootstrapped builders:** Not viable for MVP.

- API key requires Business plan ($499.95/mo minimum)
- API units purchased separately on top
- 10 units per line for organic keyword data (live); 50 units/line for historical
- 1M units = ~$50; analyzing 1,000 keywords costs 10,000 units ($0.50)

**Use case:** Same as Ahrefs — for established platforms with the budget to absorb $500+/mo in base costs before any data is purchased.

---

#### 7. Wordtracker API

**Pricing (platform plans, API bundled):**
- Bronze: $27/mo (1,000 keyword results, 7 territories)
- Silver: $69/mo (5,000 results, 15 territories)
- Gold: $99/mo (10,000 results, 200+ territories)
- Trial: $10 first month

**Data:** Search volume from Google, YouTube, Amazon, and eBay. This multi-platform view is unique. CPC data included. No keyword difficulty.

**Strengths:** Affordable. Multi-platform (useful if you want to show Amazon/YouTube search intent alongside Google).

**Weaknesses:** No difficulty metric. Data depth is modest compared to DataForSEO. Limited territory coverage on lower plans.

---

#### 8. Google Ads Keyword Planner API (free, with asterisks)

**Cost:** Free through the Google Ads API.

**The catches:**
1. Requires a Google Ads account with an active campaign history and ad spend. Accounts with no recent spend show volume as broad ranges (e.g., "1K–10K"), not exact numbers. Running $50–$100/mo in ads restores exact figures.
2. Rate limits: 15,000 operations/day at basic access. Aggressive keyword research burns this quickly.
3. Terms of service prohibit building third-party tools that resell or republish this data at scale. DataForSEO's Keywords Data API wraps GKP data within terms-compliant usage — using GKP directly for a SaaS product serving many users has ToS risk.
4. Data is CPC/PPC-focused (impressions, search volume, competition, CPC) — no organic difficulty metric.

**Verdict:** Useful for your own research and testing. Not suitable as the primary data source for a SaaS product with many users without carefully reading Google's API ToS and ensuring your usage model complies.

---

## Part 3: Key Decision Questions Answered

### What does a developer building an MVP use at ~100 users doing ~10 queries/day each?

- **Total queries/day:** 1,000 (100 users × 10 queries)
- **Total queries/month:** ~30,000

**SERP:** DataForSEO standard queue at $0.60/1K = **~$18/month**. Or Serper.dev Starter pack ($50 for 50K credits covers 1.6 months of this volume).

**Keyword data:** DataForSEO Keywords Data API — included in same account, same $50 deposit. SE Ranking API at $26.50/mo is a credible alternative if you want separate keyword difficulty data.

**Total MVP API cost at 100 users, 10 queries/day:** ~$18–$45/month depending on keyword API choice.

---

### What does it cost at scale: 1,000 users × 10 queries/day?

- **Total queries/month:** 300,000

**SERP API monthly cost estimates at 300,000 queries:**

| Provider | Cost | Notes |
|---|---|---|
| DataForSEO (standard queue) | $180/mo | Batch, ~5 min delay |
| DataForSEO (live mode) | $600/mo | Real-time |
| Serper.dev (Standard pack) | $225/mo | $0.75/1K at 500K pack |
| SerpApi | ~$750–1,000+/mo | Subscription; credits expire |
| ValueSERP | ~$150–200/mo | Monthly plan |
| Zenserp | ~$700+/mo | Expensive per-query |
| Bright Data | ~$450/mo | $1.50/1K PAYG |

---

### Estimated Monthly API Costs at 3 Scales

Assumptions:
- Each user query = 1 SERP call + 1 keyword data lookup
- Standard/batch SERP (acceptable latency for content scoring workflows)
- DataForSEO stack for all estimates (single vendor, C# SDK available)

| Scale | Queries/Month | SERP Cost (DataForSEO standard) | Keyword Cost (DataForSEO Keywords API ~$0.10/1K avg) | Total Estimate |
|---|---|---|---|---|
| 100 users × 10/day | 30,000 | $18 | $3 | **~$21/mo** |
| 500 users × 10/day | 150,000 | $90 | $15 | **~$105/mo** |
| 1,000 users × 10/day | 300,000 | $180 | $30 | **~$210/mo** |

If you use live mode for real-time UX (required for interactive features like "analyze this keyword right now"):

| Scale | Queries/Month | SERP Cost (DataForSEO live) | Keyword Cost | Total Estimate |
|---|---|---|---|---|
| 100 users × 10/day | 30,000 | $60 | $3 | **~$63/mo** |
| 500 users × 10/day | 150,000 | $300 | $15 | **~$315/mo** |
| 1,000 users × 10/day | 300,000 | $600 | $30 | **~$630/mo** |

**Practical note:** Not every query needs live mode. SERP data for content scoring can often use a 24-hour cache for competitive analysis — run a fresh pull once per keyword per day rather than per user session. This can reduce effective SERP API costs by 80–90% at scale.

---

### Is there a C# / .NET SDK or do you hit REST APIs directly?

| Provider | C#/.NET | Node.js |
|---|---|---|
| DataForSEO | Official C# client (GitHub: `dataforseo/CSharpClient`) | Official npm package (`dataforseo-client`) + TypeScript MCP server |
| SerpApi | No official; third-party NuGet wrapper exists | Official |
| Serper.dev | No | No (REST only) |
| ValueSERP/Scale SERP | No | No (REST only) |
| Bright Data | No | No (enterprise custom) |
| Zenserp | No | No (REST only) |
| SE Ranking | No | No (REST only) |
| Google Ads API | Yes (official Google Ads API .NET library) | Yes |

**DataForSEO is the only SERP/keyword API provider with an official C# SDK.** Use it in **GeekSeoBackend**. Cached JSON rows are persisted by GeekSeoBackend.

---

### Which tools does Surfer SEO, Frase, and NeuronWriter likely use?

Based on public information and developer documentation:

**Surfer SEO:** Performs a live Google SERP scrape on every content project creation — does not use a third-party SERP API. They operate their own scraping infrastructure. Data is fresh to within hours of use.

**Frase:** Scrapes Google directly and runs its own SERP analysis pipeline. The Frase API `/serp/analyze` endpoint handles Google result fetching, content extraction, PAA aggregation, and AI Overview detection in-house. Available on all plans ($39+/mo).

**NeuronWriter:** Scrapes Google directly for SERP composition and uses Moz's database for supplementary metrics (domain authority, keyword difficulty in some contexts).

**Takeaway:** All three competitors run their own SERP scraping infrastructure rather than paying per-query to a third-party API. This is the correct architecture at scale — but at MVP stage with <1,000 users, paying DataForSEO $18–$210/month is far cheaper than building and maintaining a scraper fleet. Plan to internalize SERP collection once you hit ~$500+/month in DataForSEO costs.

---

### Which is the best value for a bootstrapped product?

**For SERP data: DataForSEO** wins for a bootstrapped .NET SaaS builder:
- Cheapest per-query at standard queue ($0.60/1K)
- Only provider with an official C# SDK
- No monthly commitment — true pay-as-you-go
- Single vendor covers SERP + keyword data + on-page analysis + backlinks
- $1 free credit at signup to test without a credit card
- Low legal risk (not named in Google v. SerpApi lawsuit)

**For keyword data: DataForSEO Keywords Data API + Labs API** (same account):
- Wraps Google Ads Keyword Planner data without the ToS exposure of direct GKP access
- Bulk Keyword Difficulty via Labs API
- No second vendor to manage

**Runner-up for SERP data: Serper.dev**
- Cheaper at scale than DataForSEO live mode ($0.30/1K vs. $2.00/1K)
- No SDKs, but REST is simple
- Good for teams comfortable writing a thin HTTP wrapper
- 2,500 free credits to get started without any payment

---

## Part 4: Recommended Stack

**Runtime:** DataForSEO and all provider SDKs run in **GeekSeoBackend**. Cached SERP/keyword rows persist in `geek_seo` via GeekSeoBackend. See `ARCHITECTURE.md`.

### MVP Recommendation

**Primary SERP API:** DataForSEO
**Primary Keyword API:** DataForSEO Keywords Data API + DataForSEO Labs API
**C# SDK:** `dataforseo/CSharpClient` (official) — referenced by **GeekSeoBackend**
**Node.js SDK:** `dataforseo-client` (npm, official)

**Why DataForSEO for everything:**

1. Official C# client eliminates HTTP wrapper maintenance on a .NET backend.
2. Single vendor, single account, single billing relationship — no juggling two API keys.
3. $0.60/1K SERP queries (standard queue) is the lowest cost for a feature-rich provider — 25x cheaper than SerpApi's entry tier.
4. No monthly subscription — pay only for what you use. $50 minimum deposit is the only entry cost.
5. SERP + keywords + on-page + backlinks all in one platform — a single DataForSEO account can power most of the core features of a Surfer SEO competitor.
6. Not named in the Google lawsuit. Lower existential risk than SerpApi.
7. $1 in free credit on signup for testing before committing any real payment.

**Suggested caching strategy for cost control:**
- Cache SERP results per keyword per region for 24 hours (configurable)
- Cache keyword volume/CPC for 7 days (volume doesn't change hourly)
- Store results in your own PostgreSQL (Supabase) table — reduces redundant API calls when multiple users research the same keyword
- With aggressive caching, real-world cost at 100 users is likely $5–12/month, not $21

**When to add Serper.dev:** If you build a real-time "analyze right now" feature that users trigger interactively (not batch), consider Serper.dev at $0.30–$1.00/1K as a fast-response complement to DataForSEO's slower standard queue. DataForSEO's live mode ($2.00/1K) is the alternative within the same account.

**Avoid for MVP:**
- SerpApi — active Google lawsuit; 25x more expensive than DataForSEO; credits expire monthly
- Ahrefs API — minimum $449/mo before any data purchased
- Semrush API — requires $499.95/mo Business plan before API key access
- Zenserp — $3.40–$6.00/1K is 5–10x more expensive than DataForSEO with fewer features
- Bright Data — enterprise-oriented complexity and pricing; over-engineered for MVP

**Watch list:**
- SE Ranking API ($26.50/mo standalone) — if you want a second opinion on keyword difficulty scoring that doesn't require DataForSEO Labs
- Google Ads Keyword Planner API (free) — for supplementary CPC data during development/testing, with awareness of ToS constraints at scale

---

## Summary Scorecard

### SERP APIs

| Provider | Cost/1K | C# SDK | Legal Risk | MVP Fit | Score |
|---|---|---|---|---|---|
| DataForSEO | $0.60 (batch) / $2.00 (live) | Official | Low | Excellent | ★★★★★ |
| Serper.dev | $0.30–$1.00 | None | Medium | Very Good | ★★★★☆ |
| ValueSERP/TrajectData | $1.00–$2.59 | None | Medium | Good | ★★★☆☆ |
| Scale SERP/TrajectData | $0.29–$1.60 | None | Medium | Good | ★★★☆☆ |
| Bright Data | $1.30–$1.80 | None | Low | Fair | ★★★☆☆ |
| Zenserp | $3.40–$6.00 | None | Medium | Poor | ★★☆☆☆ |
| SerpApi | $8.67–$15.00 | None (3rd party) | High | Poor | ★★☆☆☆ |

### Keyword Data APIs

| Provider | Entry Cost | Has Difficulty? | C# SDK | MVP Fit | Score |
|---|---|---|---|---|---|
| DataForSEO (Keywords + Labs) | $50 deposit (no monthly fee) | Yes | Official | Excellent | ★★★★★ |
| SE Ranking API | $26.50/mo | Yes | None | Very Good | ★★★★☆ |
| Keywords Everywhere | $10 for 100K credits | No | None | Good | ★★★☆☆ |
| Wordtracker | $27–99/mo | No | None | Fair | ★★★☆☆ |
| Moz API | $49/mo | Yes | None | Fair | ★★★☆☆ |
| Google Ads KP | Free (with restrictions) | No | Official | Fair (ToS risk at scale) | ★★★☆☆ |
| Ahrefs API | $449/mo minimum | Yes | None | Not viable for MVP | ★☆☆☆☆ |
| Semrush API | $500/mo minimum | Yes | None | Not viable for MVP | ★☆☆☆☆ |

---

*Document created May 2026 for Geek SEO MVP planning. Pricing is current as of research date and subject to change. Verify current rates at each provider's official pricing page before committing to an API in production.*

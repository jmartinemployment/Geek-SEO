# Data Sources and APIs for an AI SEO Content Platform

> Research Date: May 2026
> Purpose: Evaluate available APIs for SERP data, NLP analysis, and keyword data to power a Surfer SEO competitor built on .NET 10 / ASP.NET Core.  
> **Runtime:** External APIs are called from **GeekSeoBackend** only. Cached results are stored in `geek_seo` by GeekSeoBackend. See `ARCHITECTURE.md`.

---

## Part 1: SERP Data APIs

These APIs provide Google (and other engine) search result data — the foundational input for content scoring, competitor analysis, and keyword research.

---

### 1. DataForSEO

**Website:** dataforseo.com
**Model:** Pay-as-you-go. No subscriptions.

#### Pricing (May 2026)

| Queue Type | Cost per Query |
|------------|---------------|
| Standard Queue | $0.0006 (~$0.60/1,000) |
| Priority Queue | $0.0012 (~$1.20/1,000) |
| Live Mode | $0.002 (~$2.00/1,000) |
| AI Summary endpoint | $0.01 per task |

- Minimum deposit: $50
- Free trial: Unlimited trial period with $1 credit on signup
- No monthly minimums

#### What You Get

- Google, Bing, Yahoo, DuckDuckGo SERP results
- Organic results, featured snippets, People Also Ask, Knowledge Graph, Local Pack, Images, News, Shopping
- Full result pages including titles, URLs, descriptions, and SERP features
- Keyword data: search volume, keyword difficulty, CPC, competition, search trends
- Rank Tracker, On-Page analysis, Backlink data
- Domain and keyword analytics

#### Pricing Multipliers

Some parameters multiply the cost:
- Depth parameter (results beyond default 10): doubles price per extra 100 results
- Additional SERP features (images, news, etc.) apply multipliers

#### Verdict for Our Product

**Best choice for a bootstrapped or self-funded build.** Pay-as-you-go pricing is predictable and scales with usage. $0.60/1,000 standard SERP queries is the lowest credible cost in the market. DataForSEO is already used as the data backbone for several competing tools (Rankability, others). Covers all data types needed: SERP, keywords, on-page, backlinks.

---

### 2. Serper.dev

**Website:** serper.dev
**Model:** Credit-based

#### Pricing (May 2026)

| Volume | Cost per 1,000 Queries |
|--------|------------------------|
| Entry | $1.00/1,000 |
| Scale | $0.30/1,000 (at volume) |

- 2,500 free credits on signup (valid 6 months)
- No monthly minimums
- Credits don't expire for 6 months

#### What You Get

- Google SERP only — no other search engines
- Titles, URLs, snippets (150–300 character snippets)
- Search metadata: position, site name
- Does not provide: AI Overviews, full page content, keyword volume data
- LangChain integration and many AI framework integrations

#### Performance

- Industry-leading 1–2 second response time
- Very high uptime
- Minimal response structure — easy to parse

#### Limitations

- Google only
- Very thin result data (no page-level metrics, no SERP features beyond basic organic)
- N o keyword data, backlink data, or on-page data

#### Verdict for Our Product

**Best for lightweight use cases** (e.g., a quick "what are the top 10 URLs" lookup). Not sufficient as the primary SERP data source — missing the depth, SERP feature coverage, and keyword data that a content scoring engine needs. Good for supplemental or real-time preview use at low cost.

---

### 3. SerpAPI

**Website:** serpapi.com
**Model:** Monthly subscription with credit rollover limitations

#### Pricing (May 2026)

| Plan | Monthly Cost | Queries/Month |
|------|-------------|---------------|
| Hobby | ~$50 | 5,000 |
| Production | ~$130 | 15,000 |
| Business | ~$250 | 30,000 |
| Enterprise | Custom | 100,000+ |

Effective per-query cost: $10–$25/1,000 depending on plan. Unused credits do not roll over — this inflates effective costs by 30–50% for variable workloads.

#### What You Get

- Google, Bing, DuckDuckGo, Yahoo, Yandex, Baidu
- Organic results, SERP features, Knowledge Graph, Local results, Images
- Google Trends, Google Maps, Google Shopping
- Google Jobs, Google Flights
- Structured JSON responses

#### Verdict for Our Product

**Too expensive relative to alternatives for the same data.** DataForSEO provides equivalent or superior data at 10–50x lower per-query cost. SerpAPI's fixed subscription model penalizes variable workloads. Only consider if their specific Google Jobs/Flights data is needed.

---

### 4. ValueSERP

**Website:** valueserp.com
**Model:** Subscription, usage-based

#### Pricing (May 2026)

- Starts under $1/1,000 queries
- Targets the budget Google SERP market

#### What You Get

- Google organic results, answer boxes, related questions, local results, images
- Clean JSON response
- Country and language targeting
- Minimal frills

#### Limitations

- Less brand recognition and ecosystem than SerpAPI
- Reliability and uptime SLAs not prominently documented
- Smaller team/support infrastructure

#### Verdict for Our Product

**A fallback option** if DataForSEO pricing increases or if redundant SERP sources are needed. Less recommended as a primary source due to uncertainty around long-term reliability and feature roadmap.

---

### 5. Bright Data SERP API

**Website:** brightdata.com
**Model:** Pay-per-result with minimum monthly commitments

#### Pricing (May 2026)

- $1–$3 per 1,000 requests (published pricing)
- Actual entry: $499+/month minimum commitment
- Self-service pricing is not straightforward; enterprise sales process required

#### What You Get

- Google, Bing, DuckDuckGo, Yandex
- Ads, featured snippets, local packs, organic results
- Real browser rendering (handles JavaScript-heavy pages)
- Proxies and scraping infrastructure

#### Verdict for Our Product

**Not appropriate for a bootstrapped build.** The $499+/month minimum makes it non-viable at early stage. Bright Data is an infrastructure company primarily serving enterprise data pipelines, not developer-friendly API products. Consider only at scale (100K+ queries/month) if DataForSEO is insufficient.

---

### SERP API Comparison Summary

| API | Cost/1K Queries | Min Commitment | Data Depth | Best For |
|-----|-----------------|----------------|------------|---------|
| DataForSEO | $0.60 (standard) | $50 deposit | Excellent — full SERP features + keywords + backlinks | Primary data source |
| Serper.dev | $0.30–$1.00 | None | Minimal — titles + snippets only | Lightweight real-time lookups |
| SerpAPI | $10–$25 | $50/month | Good — Google + other engines | If Bing/Yahoo data needed |
| ValueSERP | <$1 | Variable | Good — Google only | Budget fallback |
| Bright Data | $1–$3 | $499+/month | Excellent — rendered pages | Enterprise scale only |

**Recommendation:** DataForSEO as primary source. Serper.dev as a secondary real-time option for lightweight queries.

---

## Part 2: NLP and Semantic Analysis APIs

These APIs enable content scoring, entity extraction, topic modeling, and semantic similarity — the intelligence layer that powers optimization recommendations.

---

### 1. OpenAI Embeddings API

**Website:** platform.openai.com
**Purpose:** Semantic similarity, topic clustering, content classification, RAG pipelines

#### Models and Pricing (May 2026)

| Model | Cost per 1M Tokens | Notes |
|-------|-------------------|-------|
| text-embedding-3-small | $0.02/1M tokens | Best for most production use cases |
| text-embedding-3-large | $0.13/1M tokens | Higher accuracy; larger vectors |
| text-embedding-ada-002 | $0.10/1M tokens | Legacy; replaced by 3-small |
| Batch API (3-small) | $0.01/1M tokens | 50% off; async |

- New accounts: $5 in free credits (~250M tokens)
- Embeddings only charge for input tokens — no output tokens
- 500-token document ≈ $0.00001 to embed with 3-small

#### Use Cases for Our Product

- Semantic similarity scoring (compare content against top-ranking pages beyond keyword matching)
- Topic cluster detection (group semantically similar keywords without SERP data)
- Content gap identification (embedding your content vs. top competitor content and computing distance)
- Entity relationship mapping
- RAG (Retrieval-Augmented Generation) for AI writing grounded in real data

#### Verdict

**Essential building block.** At $0.02/1M tokens, this is effectively free for most content-scale operations. A typical article analysis (5,000 words) costs less than $0.001. Use for all semantic comparison and classification work. OpenAI's embedding models score highly on MTEB benchmarks.

---

### 2. Google Cloud Natural Language API

**Website:** cloud.google.com/natural-language
**Purpose:** Entity recognition, sentiment analysis, syntax analysis, content classification

#### Pricing (May 2026)

| Feature | Cost per 1,000 Units | Free Monthly |
|---------|---------------------|--------------|
| Sentiment Analysis | $1.00–$2.00/1K requests | 5,000 |
| Entity Analysis | $1.00/1K requests | 5,000 |
| Syntax Analysis | $0.50/1K requests | 5,000 |
| Content Classification | $0.10/1K requests (text classify v1) | 30,000 |
| Entity Sentiment Analysis | $2.00/1K requests | 5,000 |

- Units = rounded to nearest 1,000 Unicode characters per request
- Text Moderation: rounded to nearest 100 characters

#### Use Cases for Our Product

- Entity extraction: identify key named entities (people, places, products, concepts) in content vs. competitors
- Content classification: automatically categorize pages by topic
- Sentiment analysis: analyze tone of content
- Syntax parsing: identify parts of speech for term importance weighting

#### Relevance to Clearscope's Approach

Clearscope uses Google Cloud NLP as one of its three scoring models. This is public knowledge from IBM case studies. It confirms the API is production-proven for content optimization workflows.

#### Verdict

**High strategic value — and already proven in a top competitor.** The fact that Clearscope and Rankability both use Google NLP validates it as an accurate NLP source for SEO content analysis. The 30,000 free text classifications per month allows substantial testing. Use alongside OpenAI embeddings for a multi-signal NLP scoring system.

---

### 3. IBM Watson Natural Language Understanding

**Website:** ibm.com/cloud/natural-language-understanding
**Purpose:** Concept extraction, entity recognition, semantic roles, keyword extraction

#### Pricing (May 2026)

- Free tier: 30,000 NLU items/month
- Pay-as-you-go: varies by feature type
- Generally $0.003–$0.006 per item beyond free tier

#### Use Cases for Our Product

- Concept extraction from competitor pages (what concepts does Google associate with this topic?)
- Entity salience scoring (how important is each entity to the content?)
- Semantic role labeling

#### Verdict

**Valid but may be redundant.** Clearscope uses Watson specifically because its concept salience scoring proved accurate for SEO content optimization. However, OpenAI embeddings + Google Cloud NLP can cover similar ground with better developer ergonomics and lower cost. IBM Watson NLU is an enterprise product with some bureaucratic setup overhead. Evaluate whether the Watson-specific salience model is meaningfully better than Google NLP for your use case before committing.

---

### 4. OpenAI Chat Completions API (GPT-4o / GPT-4.1)

**Website:** platform.openai.com
**Purpose:** AI article generation, outline creation, content gap analysis, brief writing

#### Pricing (May 2026 — approximate)

| Model | Input (per 1M tokens) | Output (per 1M tokens) |
|-------|----------------------|------------------------|
| GPT-4o | $2.50 | $10.00 |
| GPT-4.1 | $2.00 | $8.00 |
| GPT-4o-mini | $0.15 | $0.60 |
| o3-mini | $1.10 | $4.40 |

- GPT-4o-mini is the best cost-performance model for routine content generation
- GPT-4o / 4.1 for higher-quality output where quality justifies cost
- Prompt caching (for repeated system prompts) provides up to 50% discount

#### Verdict

**Core AI writing engine.** The Claude API (Anthropic) is a strong alternative with competitive pricing and excellent long-context performance (100K token context window). For a product built by a Claude Code user, Anthropic's API deserves evaluation alongside OpenAI. Recommend offering model selection as a backend configuration option.

---

### 5. Anthropic Claude API

**Website:** anthropic.com/api
**Purpose:** AI article generation, research summarization, content analysis

#### Pricing (May 2026 — Claude Sonnet 4)

| Model | Input (per 1M tokens) | Output (per 1M tokens) |
|-------|----------------------|------------------------|
| Claude Opus 4 | $15.00 | $75.00 |
| Claude Sonnet 4 | $3.00 | $15.00 |
| Claude Haiku 3.5 | $0.80 | $4.00 |

- Prompt caching provides significant savings for repeated context (system prompts, tool definitions)
- 200K token context window — excellent for long document analysis

#### Verdict

**Strong alternative to OpenAI for content generation.** Claude excels at long-form writing quality and following complex structured instructions (critical for SEO content where you need to enforce heading structures, term coverage, word count targets). Consider Claude Haiku 3.5 for cost-sensitive bulk generation and Claude Sonnet 4 for high-quality output.

---

### NLP API Comparison Summary

| API | Primary Use | Cost | Proven In |
|-----|-------------|------|-----------|
| OpenAI text-embedding-3-small | Semantic similarity, clustering | $0.02/1M tokens | Industry standard |
| Google Cloud NLP | Entity extraction, classification | $0.10–$2.00/1K requests | Clearscope, Rankability |
| IBM Watson NLU | Concept salience, entity scoring | $0–$0.006/item | Clearscope |
| OpenAI GPT-4o-mini | AI content generation | $0.15/$0.60 per 1M in/out | Industry standard |
| Anthropic Claude | AI content generation | $0.80–$3.00/1M in | Geek-SEO product stack |

---

## Part 3: Keyword Data APIs

These APIs provide keyword search volume, difficulty, CPC, trends, and related keywords — needed for keyword research and content planning features.

---

### 1. DataForSEO Keywords Data API

**Website:** dataforseo.com
**Part of the same DataForSEO platform as their SERP API**

#### Pricing

| Data Type | Cost |
|-----------|------|
| Google Ads keyword data | $0.0005–$0.003 per keyword |
| Keyword search volume (live) | $0.0010 per keyword |
| Keyword suggestions | $0.0010–$0.0030 per response |
| Keyword difficulty | Bundled with keyword data |

#### What You Get

- Search volume (global and locale-specific)
- Keyword difficulty
- CPC and competition
- Search trend data (12-month volume history)
- Related keywords and suggestions
- SERP features for each keyword
- People Also Ask data
- Keyword clustering (via API parameters)

#### Verdict

**Best value for keyword data.** Combines with their SERP API under one account. Single API provider for SERP, keywords, and on-page data simplifies architecture. This is how most indie SEO tools are built.

---

### 2. Ahrefs API

**Website:** ahrefs.com/api
**Model:** Usage-based billing on top of subscription

#### Pricing Requirements

- API access requires Advanced plan minimum: **$449/month**
- Usage billed in "rows" per API response
- Overage: $0.35–$1.00 per 1,000 rows

#### What You Get

- Keywords Explorer data (volume, difficulty, CPC, SERP overview)
- Site Explorer (organic keywords, backlinks, referring domains)
- Rank Tracker, Site Audit, Brand Radar
- Among the most accurate keyword volume data in the industry

#### Verdict

**Best data quality; prohibitive cost for a bootstrapped product.** The $449/month minimum before API overage makes this a Phase 2 integration at best. Negotiate a reseller or data-as-a-service arrangement if Ahrefs data quality is essential to your product differentiation. Not recommended for MVP.

---

### 3. Semrush API

**Website:** developer.semrush.com
**Model:** Unit-based, bundled with Business plan subscription

#### Pricing Requirements

- API access requires Business plan: **$499.95/month**
- 1 API unit ≈ $0.00005 (~$50/1M units)
- 10 units per domain keyword row (live), 50 units (historical)
- Units do not roll over monthly

#### What You Get

- Organic keyword rankings for any domain
- Keyword overview (volume, difficulty, CPC, trend)
- Advertising research
- Backlink analytics
- Competitive domain analysis

#### Verdict

**Not viable at entry stage.** $499.95/month before any usage costs is a steep floor. Semrush's data is excellent (used by ContentShake to power its keyword scoring) but the API access barrier limits this to large-scale or enterprise products. Consider a reseller arrangement if Semrush data is a feature differentiator.

---

### 4. Moz API

**Website:** moz.com/products/api
**Model:** Subscription or entry-level access

#### Pricing

| Tier | Monthly | Rows/Month |
|------|---------|-----------|
| Entry | $20/month | 20,000 rows |
| Medium | $149/month | 50,000 rows |
| Large | $249/month | 100,000 rows |

- API access available at **$20/month** — dramatically more accessible than Ahrefs or Semrush
- Row-based; overages are throttled (not extra billed)

#### What You Get

- Domain Authority, Page Authority, Spam Score (Moz's proprietary metrics)
- Link Explorer: backlinks, anchor text, referring domains
- Keyword Explorer: search volume, difficulty, priority score
- SERP analysis per keyword

#### Limitations

- Domain Authority is Moz's own metric — not universally trusted for absolute values
- Keyword volume data less comprehensive than Ahrefs/Semrush
- Smaller link index than Ahrefs

#### Strategic Note

NeuronWriter integrates Moz data natively — this is how they provide authority signals (PA/DA) at low cost. For a product targeting small businesses, Moz's $20/month entry API is a credible starting point for domain authority signals.

#### Verdict

**Best entry-level keyword and authority API.** For an MVP, Moz at $20/month provides good-enough data for:
- Domain Authority scoring in SERP analysis
- Basic keyword volume and difficulty
- Link data for competitor authority context

Upgrade to DataForSEO's keyword data as primary, Moz as authority-signal supplement.

---

### 5. Google Ads Keyword Planner (via DataForSEO)

Google does not offer a direct Keyword Planner API for third-party tools. DataForSEO wraps Google Ads data through their platform. This is the most common approach for accessing Google's keyword volume data in a SaaS product.

---

### Keyword API Comparison Summary

| API | Entry Cost | Data Quality | Best Use |
|-----|------------|-------------|---------|
| DataForSEO Keywords | $0.001/keyword + $50 deposit | Good | Primary — best cost + coverage |
| Moz API | $20/month | Moderate | Domain authority signals supplement |
| Semrush API | $499.95/month | Excellent | Phase 2 / enterprise tier |
| Ahrefs API | $449/month | Best-in-class | Phase 2 / enterprise tier |

---

## Recommended Architecture for MVP

### Tier 1 (MVP — Months 1–6)

| Function | API | Estimated Monthly Cost (1,000 users, 5 reports each) |
|----------|-----|------------------------------------------------------|
| SERP data for content scoring | DataForSEO Standard Queue | ~$3.00 (5,000 queries) |
| Keyword volume + difficulty | DataForSEO Keywords | ~$5.00 (5,000 lookups) |
| NLP entity extraction | Google Cloud NLP (free tier first) | $0–$10 |
| Semantic similarity | OpenAI text-embedding-3-small | ~$1.00 |
| AI article generation | Claude Haiku 3.5 or GPT-4o-mini | ~$5–20 |
| Domain authority signals | Moz API | $20/month |
| **Total estimated API cost** | | **~$35–60/month at MVP scale** |

### Tier 2 (Growth — Months 7–18)

- Add DataForSEO On-Page API for deeper technical analysis
- Add DataForSEO Backlinks for competitive link analysis
- Evaluate Ahrefs or Semrush API reseller arrangements for premium tiers
- Add GPT-4o or Claude Sonnet 4 for higher-quality AI writing tier

### Key Observation

The biggest competitive tools (Surfer, Clearscope, NeuronWriter) all use DataForSEO or similar scraped SERP data at their core. The NLP differentiation (Google NLP, Watson, OpenAI embeddings) is where scoring quality diverges. A three-signal NLP scoring approach (OpenAI embeddings + Google Cloud NLP + custom domain analysis) can match or exceed the scoring accuracy of established competitors at very low cost.

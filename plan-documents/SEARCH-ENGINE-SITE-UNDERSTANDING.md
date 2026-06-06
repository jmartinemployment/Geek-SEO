# Search Engine Site Understanding Research
## How Major Search Engines Extract Data and Determine What a Site Is About

*Research date: June 2026*
*Purpose: Inform GeekSEO SaaS feature parity — mirror real search engine site understanding behavior*

---

## 1. The Problem Search Engines Solve

Every major search engine must answer a fundamental question for every URL it crawls:

> **"What is this page / site actually about — and with what confidence?"**

This is not a keyword problem. Modern engines resolve this at the **entity and topic level**, not the string level. A page about "accounting software for restaurants" is not just matching those words — it is a node in a topic graph connecting:
- Entity: Software
- Sub-entity: Accounting
- Vertical: Restaurant industry
- Intent: Commercial / SaaS evaluation

GeekSEO must replicate this same multi-signal, entity-aware site understanding pipeline.

---

## 2. Major Search Engines — At a Glance

| Engine | Market Share | Index Source | Key Differentiator |
|--------|-------------|-------------|-------------------|
| **Google** | ~90% global | Own (Googlebot) | BERT + MUM + Knowledge Graph; entity salience scoring |
| **Bing** | ~4% global | Own (Bingbot) | Powers Yahoo, DuckDuckGo, Copilot; "Whole Page Algorithm" |
| **Yahoo** | ~1% global | Powered by Bing | No independent crawl; Bing results + Yahoo editorial |
| **DuckDuckGo** | ~1% global | Bing + DuckDuckBot + Wikipedia/Wolfram Alpha | Privacy-first ranking layer over Bing index |
| **Yandex** | ~2% Russia | Own (YandexBot) | Heavy user engagement signals; weaker backlink weight |
| **Baidu** | ~65% China | Own (Baiduspider) | Chinese NLP; mobile-first; government compliance signals |
| **Ecosia / Brave** | <1% | Bing-powered | Thin layers over Bing; same signals apply |

**Key takeaway for GeekSEO:** Mirror Google + Bing = cover ~98% of the signal space. Yahoo, DuckDuckGo, and Brave inherit from Bing. Yandex and Baidu are separate implementations but use the same fundamental signal categories.

---

## 3. The Universal Site Understanding Pipeline

All major engines follow this pipeline, with variations in implementation:

```
[Crawl] → [Parse] → [Extract] → [Classify] → [Entity-Link] → [Score] → [Index]
```

### 3.1 Crawl Discovery

How engines find pages:
- **Sitemap.xml** — Declared URL inventory. Engines trust sitemaps with all-200 URLs that match canonical tags. Sitemaps with 404s or noindex URLs degrade trust.
- **robots.txt** — Crawl permission + sitemap declaration. Sitemaps in robots.txt are discovered 3× faster by secondary crawlers.
- **Internal links** — Most important discovery mechanism. Link equity flows depth-first from homepage. Pages reachable in fewer hops (PageRank proximity) are crawled more frequently and weighted higher.
- **External links** — Backlinks signal trust and discovery.
- **Structured data sitemaps** — Specialized sitemaps for news, images, video.

**GeekSEO mirror:** `SitemapExtractor` + robots.txt parse + `InternalLinkExtractor` (Phase B). These are the same signals.

### 3.2 Parse

Engines render pages at two fidelity levels:
1. **HTTP/HTML parse** — Fast, no JS execution. Used for initial crawl.
2. **Full render (JS execution)** — Google uses a two-wave approach: HTML first, JS render queued. Bing renders JS for important pages.

Content extracted at parse time:
- `<title>`, `<meta description>`, `<meta keywords>` (low weight)
- `<h1>` through `<h6>` — heading hierarchy
- `<body>` text — tokenized, entity-scanned
- `<a href>` — all internal + external links with anchor text
- `<img alt>` — image semantic signals
- `<nav>` — navigation structure
- JSON-LD, Microdata, RDFa (structured data)
- `<link rel="canonical">` — authoritative URL signal
- `<link rel="alternate" hreflang>` — geo/language signals

**GeekSEO mirror:** `HomepageHeadingsExtractor`, `NavMenuExtractor`, `PageContentExtractor`, `SchemaOrgExtractor`. Playwright handles JS rendering. All four are Phase A complete.

### 3.3 Extract — What Engines Pull From Content

#### 3.3.1 Structured Data (Highest Signal Weight)

Schema.org JSON-LD is the most trusted signal because it is **explicit owner declaration**. Engines do not have to infer — the site owner states facts directly.

**Google processes:**
- `@type` — page/entity classification (Organization, LocalBusiness, Product, Article, etc.)
- `knowsAbout` — explicit topic declarations
- `hasOfferCatalog` / `makesOffer` — service/product inventory
- `serviceType` — service classification
- `areaServed` — geographic scope
- `name`, `alternateName` — brand entity
- `sameAs` — Knowledge Graph disambiguation (Wikipedia, Wikidata, LinkedIn URLs)
- `description` — semantic fallback
- `aggregateRating` — trust signal
- `breadcrumb` — site hierarchy
- `FAQ`, `HowTo` — rich result eligibility

**Bing processes:** Same schema.org vocabulary. Additionally, Bing gives higher weight to `Organization` schema with `SameAs` identifiers for entity disambiguation in Copilot/AI answers.

**Post-March 2026:** `Organization` + `Person` schema with `SameAs` identifiers became the highest-leverage implementation type. Sites with clear entity disambiguation show measurable improvements in AI Mode citations and Knowledge Panel accuracy.

**GeekSEO mirror:** `SchemaOrgExtractor` covers `serviceType`, `knowsAbout`, `offerCatalog`, `areaServed`, `name`. **Gap:** `sameAs` extraction not yet implemented — critical for entity disambiguation scoring.

#### 3.3.2 Named Entity Recognition (NER)

After extracting raw text, engines apply NLP pipelines to identify entities:

**Entity types extracted:**
- Organizations (companies, brands)
- Locations (city, state, country, neighborhood)
- People (authors, founders, staff)
- Products / Services
- Concepts / Topics
- Events
- Dates / Time references

**Google's NLP stages:**
1. **Tokenization** — split text into tokens
2. **Parsing** — syntactic structure (dependency tree)
3. **Entity extraction** — NER model tags entities
4. **Entity resolution** — match extracted entity to Knowledge Graph node
5. **Salience scoring** — rank entity prominence (0.0–1.0); how central is this entity to the page?
6. **Sentiment scoring** — positive/negative association with entity

**Salience score** is the key output. A page about "IT Support for Small Business" has IT Support as a high-salience entity. If the entire site clusters around this entity, the domain earns topical authority for it.

**BERT role:** BERT enables bidirectional context understanding. "Apple" in a tech context vs food context is disambiguated. This means keyword stuffing fails — the model understands the topic graph around a term, not just the term.

**MUM role (Google):** Multimodal Understanding Model. Processes text + images + video together. Can understand service offering from an image alt tag combined with surrounding text.

**GeekSEO gap:** No explicit salience scoring on extracted entities. The confidence score in `TopicFusionEngine` is a functional proxy, but a true NER pass over page body text would strengthen Phase B/C.

#### 3.3.3 Internal Link Graph Analysis

The anchor text of internal links is one of the three inputs to Google's topicality signal (internally referenced as **T\***), alongside body text and clicks.

**What engines infer from internal links:**
- **Pillar pages** — pages with many inbound internal links are treated as topic hubs
- **Anchor text distribution** — consistent anchor text around a term = topical signal for that term
- **Link depth** — pages linked from homepage carry more weight than deep orphan pages
- **Siloing** — if `/services/accounting/` links only to accounting-related pages, the silo reinforces topical focus
- **Crawl priority** — highly internally-linked pages get crawled more frequently

**URL pattern as signal:**
- `/services/accounting-software` → "accounting software" = service offering
- `/blog/how-to-file-taxes` → informational intent around "taxes"
- `/locations/miami-fl` → geographic targeting signal

**GeekSEO roadmap:** Phase B — `InternalLinkExtractor` (anchor text → topic evidence) and `UrlPatternExtractor` (path segments → topic boost). These directly mirror real engine behavior.

#### 3.3.4 Navigation Structure

Engines treat the visible navigation menu as an editorial declaration of site content hierarchy. The nav is the site owner saying "these are the top-level things we do."

**Signals extracted:**
- Top-level nav items = pillar topics
- Dropdown sub-items = subtopics under each pillar
- Footer links = secondary topic signals (lower weight)
- Breadcrumb nav = content hierarchy confirmation

**GeekSEO mirror:** `NavMenuExtractor` — Playwright-based, handles mobile hamburger menus and dropdowns. Full Phase A implementation.

#### 3.3.5 Heading Hierarchy (H1–H6)

Headings are the document outline. Engines assign progressively lower weight down the hierarchy:

| Heading | Signal Weight | Usage |
|---------|--------------|-------|
| H1 | Highest | Page's primary topic — should match title intent |
| H2 | High | Major sections / primary subtopics |
| H3 | Medium | Sub-sections; service/product names at section level |
| H4–H6 | Low | Granular detail; less topic signal |

**Pattern engines look for:**
- H1 declares primary entity → H2s expand subtopics → H3s detail specifics
- Consistent heading topics across multiple pages = site-level topical authority
- H1 ≠ page title = potential signal mismatch (content coherence gap)

**GeekSEO mirror:** `HomepageHeadingsExtractor` + `HeadingPillarBuilder`. H3s treated as "page verticals" (service sub-areas) in `PageContentExtractor`.

### 3.4 Classify — Topic and Intent Assignment

After extraction, engines classify at two levels:

#### Site-Level Classification
- **Vertical** (e.g., "Local Business > IT Services")
- **Topical cluster** (e.g., "Managed IT, Cybersecurity, Cloud Services")
- **Geographic scope** (national, regional, local)
- **Business type** (B2B, B2C, eCommerce, Publisher, etc.)

#### Page-Level Intent Classification
- **Informational** — "how to" content, blog posts
- **Navigational** — branded queries, finding a specific site
- **Commercial** — comparison, research before purchase
- **Transactional** — buy, sign up, contact

**Bing's new GEO labels (February 2026):** Bing Webmaster Tools now exposes "grounding query topic labels" — the topic categories Bing assigns to pages that appear in Copilot answers. This is the engine revealing its classification system publicly for the first time.

**GeekSEO implication:** The `NicheRootEntityBuilder` + pillar system maps to site-level classification. Page-level intent classification is not yet implemented — needed for content gap analysis (Phase C/D).

### 3.5 Entity Linking — Knowledge Graph Connection

The most sophisticated part of modern search engine site understanding:

**Process:**
1. Extract entity from page (e.g., "Geek At Your Spot")
2. Disambiguate — is this a known entity in the Knowledge Graph?
3. If match found → inherit existing entity attributes (category, location, related entities)
4. If no match → create candidate node (requires multiple trustworthy source confirmations)
5. Link entity to related entities (competitors, industry, location, etc.)

**Signals that strengthen entity linking:**
- `sameAs` schema property pointing to Wikipedia, Wikidata, LinkedIn, Crunchbase
- Consistent brand name + URL across Google Business Profile, Yelp, industry directories
- Brand mentions (without links) on authoritative sites ("unlinked citations")
- Author bylines matching Google Scholar / LinkedIn entities
- Content with 15+ connected entities shows 4.8× higher citation probability in AI Overviews

**GeekSEO gap:** No `sameAs` extraction or Knowledge Graph disambiguation check. This is a Phase C/D addition that would dramatically improve entity confidence scoring.

### 3.6 Confidence Scoring — How Engines Weight Agreement

Engines compute confidence that a given topic applies to a site by counting corroborating signals:

**Google's confidence heuristic (reconstructed from public signals and patents):**

| Signal | Approximate Weight |
|--------|------------------|
| Schema `knowsAbout` / `offerCatalog` explicit declaration | Very high |
| Dedicated URL + sitemap presence | High |
| Nav menu item | High |
| H1/H2 on homepage | Medium-high |
| H3 on multiple pages | Medium |
| Body text frequency (salience) | Medium |
| Internal links with relevant anchor text | Medium |
| External links with relevant anchor text | Medium |
| GSC query impression data | High (owner-connected) |
| SERP ranking for topic-related queries | Validation signal |
| Knowledge Graph existing node | Trust multiplier |
| `sameAs` entity disambiguation | Trust multiplier |

**Bing's Content Quality Framework (publicly stated):**
- **Authority** — domain trust, entity establishment
- **Utility** — does content fully answer the topic?
- **Presentation** — technical quality, structure, UX signals

**GeekSEO mirror:** `TopicEvidenceWeights` in `TopicFusionEngine` directly models this. Current weights: Schema=0.35, Sitemap=0.25, Nav=0.20, Page=0.15, Heading=0.10. These align well with real engine behavior. **Phase B** will add InternalLinks weight.

---

## 4. Engine-Specific Differences

### 4.1 Google

- **Entity salience** is the core primitive — not keyword frequency
- BERT + MUM provide deep semantic understanding of context
- **Knowledge Graph** stores entity relationships; sites with KG nodes rank more stably
- Two-wave rendering: HTML first, JS queued (Googlebot Wave 2)
- **E-E-A-T** (Experience, Expertise, Authoritativeness, Trustworthiness) applied at entity + domain level
- GSC integration is the only "owner-connected" signal Google officially acknowledges
- AI Overviews (2026): citations favor sites with structured entity declarations and 15+ connected entities

### 4.2 Bing / Microsoft Copilot

- Owns Yahoo and DuckDuckGo results (same index, different ranking layers)
- **"Whole Page Algorithm"** — evaluates page holistically, not just the targeted keyword
- **AI Performance Metrics** (Feb 2026): Bing now exposes citation share + grounding query topic labels in Webmaster Tools
- `sameAs` + `Organization` schema with external identifiers are highest-leverage signals for Copilot citations
- More weight on **social signals** (Facebook shares, Twitter/X engagement) than Google
- **DeepLinks** (featured nav links in SERP) require clean site structure + breadcrumb schema

### 4.3 DuckDuckGo

- Inherits Bing index + applies privacy-first re-ranking
- Adds Wikipedia Instant Answer (DuckDuckGo Answers) — Wikipedia entity linkage is critical
- DuckDuckBot crawls independently for Instant Answers enrichment
- Wolfram Alpha integration for factual queries
- **No personalization** — ranking is purely signal-based, no user history

### 4.4 Yandex

- Heavy user engagement signals (CTR, dwell time, bounce rate) relative to backlinks
- Particularly strong Russian NLP
- **ICS (Index of Commercial Sites)** — separate index for commercial entities with geo-targeting
- Location signals (address, phone, region code) weighted high for local queries
- Less emphasis on link authority vs Google

### 4.5 Baidu

- Chinese character tokenization (different NLP problem than English)
- **Baidu Baike** (Chinese Wikipedia equivalent) — entity linked to Baike entries
- Mobile-first indexing (mobile traffic is dominant in China)
- Government compliance signals affect indexing eligibility
- MIP (Mobile Instant Pages) = Baidu's AMP equivalent — strong crawl preference
- Less transparent algorithm; high weight on domain age and Baidu-hosted content (Baijiahao)

---

## 5. Signals GeekSEO Must Implement (Prioritized)

The following table maps real engine signals to GeekSEO implementation status:

| Signal | Engines Using | GeekSEO Status | Priority |
|--------|--------------|---------------|----------|
| Schema.org `knowsAbout` / `offerCatalog` | Google, Bing, all | ✅ Implemented | Done |
| Sitemap URL structure / path hierarchy | All | ✅ Implemented | Done |
| Navigation menu structure | All | ✅ Implemented | Done |
| Heading hierarchy (H1–H3) | All | ✅ Implemented | Done |
| Homepage body text / list items | All | ✅ Implemented | Done |
| Internal link graph + anchor text | Google, Bing | 📋 Phase B | **High** |
| URL slug pattern extraction | All | 📋 Phase B | **High** |
| `sameAs` entity disambiguation | Google, Bing | ❌ Not planned | **High** |
| Page-level intent classification | Google, Bing | ❌ Not planned | **Medium** |
| Entity salience scoring (NER pass) | Google | ❌ Not planned | **Medium** |
| Keyword demand validation (SERP signals) | Google, Bing | 📋 Phase C | Medium |
| GSC query clustering | Google | 📋 Phase D | Medium |
| Knowledge Graph entity linkage check | Google, Bing | ❌ Not planned | Low (future) |
| Social signal integration | Bing, Yandex | ❌ Not planned | Low |
| User engagement signals | Yandex, Google | ❌ Not planned | Low (future) |
| Mobile rendering / CWV signals | All | ❌ Not planned | Low |

---

## 6. The "What Is This Site About" Answer — Engine vs GeekSEO

### How Google answers it:
1. Crawl → parse HTML + JS-rendered content
2. Extract entities from schema, headings, body text
3. Score salience of each entity
4. Link entities to Knowledge Graph nodes
5. Aggregate across all pages → site-level topical authority graph
6. Confirm via backlink anchor text + GSC query data
7. Result: entity node with confidence, vertical classification, geographic scope

### How GeekSEO answers it today (Phase A):
1. Fetch homepage + top sitemap URLs (Playwright)
2. Extract from schema, sitemap, nav, headings, page content (6 extractors)
3. Pool all TopicCandidates with stacked evidence
4. Fuse through 3 gates (scope, similarity, relevance)
5. Rank by confidence; apply pillar cap
6. Result: `FusedSiteUnderstanding` with `SelectedPillars` + provenance

**Gap analysis:** GeekSEO Phase A covers Tier 1 (freely observable) signals well. The primary gaps relative to Google's full pipeline are:
- No entity resolution (linking to external KG nodes)
- No salience score from NER model
- No internal link graph
- No multi-page crawl at scale (currently limited to homepage + N sitemap pages)
- No `sameAs` extraction

---

## 7. Recommended Next Signals to Implement

### Immediate (Phase B — Structure Signals)

**`InternalLinkExtractor`**
- Crawl all pages returned by SitemapExtractor
- Extract `<a href>` → count inbound links per URL
- Extract anchor text → add as evidence for target page's topic candidates
- Evidence source: `"internal_link"`, weight: 0.18 (between nav and page body)
- Signal: pages with many inbound links = pillar candidates; anchor text = topic confirmation

**`UrlPatternExtractor`**
- Parse URL slugs for service/topic terms
- `/services/accounting-software` → topic: "Accounting Software", source: `"url_pattern"`, weight: 0.12
- Boost confidence of candidates whose slugs appear in body/nav/schema
- Degrade confidence of candidates whose topic never appears in a URL

### Near-Term (Phase B.5 — Entity Disambiguation)

**`SameAsExtractor`**
- Extract `sameAs` array from JSON-LD
- Check against known entity databases: Wikipedia, Wikidata, LinkedIn, Google Business Profile URLs
- If `sameAs` URL found → mark entity as "KG-linked" with higher base confidence
- Evidence source: `"same_as"`, weight: 0.30 (nearly as strong as schema declaration)

**Why:** Engines weight `sameAs` very heavily for entity disambiguation. A site declaring `sameAs: https://en.wikipedia.org/wiki/Geek_At_Your_Spot` gets near-certain entity resolution. GeekSEO detecting this same signal should reward the site accordingly.

### Phase C — Demand Validation (already planned)

- Keyword volume enrichment per pillar
- SERP position check per pillar
- Competitor SERP overlap analysis

### Phase D — Owner Augmentation (already planned)

- GSC query clustering → map queries to pillars
- GA4 high-traffic page analysis → confirm pillar strength

---

## 8. Architectural Principle — Mirror, Don't Guess

The core design principle for GeekSEO's site understanding layer:

**Every signal weight in `TopicEvidenceWeights` should have a documented justification tied to a real engine behavior.**

Current weights are well-reasoned. To evolve them rigorously:
1. When a new extractor is added, document which engine(s) use it and at what relative weight
2. When updating weights, note the engine behavior driving the change
3. Version every weight change in `FusionVersion` (currently `"sul-1.0"`)

This makes GeekSEO's engine an auditable implementation of real search engine behavior — not a black box.

---

## 9. Sources and References

- [Google Search Central — Crawling & Indexing Documentation](https://developers.google.com/search/docs/crawling-indexing)
- [Google Structured Data — Introduction](https://developers.google.com/search/docs/appearance/structured-data/intro-structured-data)
- [Bing Webmaster Guidelines](https://www.bing.com/webmasters/help/webmaster-guidelines-30fba23a)
- [Bing AI Performance in Webmaster Tools (Feb 2026)](https://blogs.bing.com/webmaster/February-2026/Introducing-AI-Performance-in-Bing-Webmaster-Tools-Public-Preview)
- [Entity Extractions for Knowledge Graphs — Go Fish Digital](https://gofishdigital.com/blog/entity-extractions-knowledge-graphs/)
- [Google NLP — BERT, NER & MUM Process](https://squin.org/semantic-seo/how-google-nlp-works/)
- [Entity-Based SEO: Knowledge Graph](https://thecontentbeacon.com/blog/understanding-entity-based-seo/)
- [Schema Markup After March 2026](https://www.digitalapplied.com/blog/schema-markup-after-march-2026-structured-data-strategies/)
- [Topic Cluster Content Architecture 2026](https://www.digitalapplied.com/blog/topic-cluster-content-architecture-2026-seo-methodology)
- [AI Search Entity Recognition — iPullRank](https://ipullrank.com/ai-search-entity-recognition)
- [How AI Ranking Signals Affect Google Search 2025 — Single Grain](https://www.singlegrain.com/artificial-intelligence/how-ai-ranking-signals-might-change-google-search-in-2025/)
- [Search Engine Differences — Lawrence Hitches](https://www.lawrencehitches.com/search-engine-differences/)
- [Named Entity Recognition Enhanced Ranking — ThatWare](https://thatware.co/named-entity-recognition/)
- [Bing SEO Guide 2025 — SEO Sherpa](https://seosherpa.com/bing-seo/)

---

*Document created: 2026-06-06*
*Status: Research complete — ready to inform Phase B and Phase B.5 implementation planning*

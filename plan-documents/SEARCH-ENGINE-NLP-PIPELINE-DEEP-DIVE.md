# Search Engine NLP Pipeline — Deep Dive
## Entity Extraction, Topic Classification, and the Entity-to-Keyword Bridge

*Research date: June 2026*
*Companion to: [`SEARCH-ENGINE-SITE-UNDERSTANDING.md`](SEARCH-ENGINE-SITE-UNDERSTANDING.md)*
*Purpose: Technical depth for GeekSEO to mirror real engine extraction behavior*

---

## Overview

This document answers a specific question:

> **How do search engines go from raw HTML to knowing what a site is about — and how does that entity-level understanding produce keyword-level ranking outcomes?**

The answer is an 8-stage pipeline operating at three levels simultaneously: token level, entity level, and topic/graph level. Each level feeds the next. GeekSEO must mirror all three.

---

## Stage 1: Crawl and Render

**Input:** URL  
**Output:** Rendered DOM (HTML string)

Google runs two rendering passes:
1. **Wave 1 (immediate):** HTTP fetch → raw HTML parse. Fast, no JS. Used for link discovery, canonical detection, robots signals.
2. **Wave 2 (queued):** JavaScript execution via Web Rendering Service (WRS). Googlebot runs Chromium headless. Rendered DOM replaces raw HTML for NLP processing.

**Why this matters for GeekSEO:** Playwright in `PageContentExtractor` and `NavMenuExtractor` mirrors Wave 2 exactly — full JS render. HTTP fallback in `PageContentExtractor` mirrors Wave 1. This dual-mode approach is architecturally correct.

**Bing rendering:** Bing renders JS for pages with sufficient PageRank. For thin/new sites, it relies on Wave 1 only. Same dual-mode applies.

**Signal captured at this stage:**
- Raw text content (all stages feed from this)
- Link graph (all `<a href>` elements)
- Structured data (JSON-LD, Microdata, RDFa)
- Meta signals (canonical, hreflang, robots directives)

---

## Stage 2: Tokenization

**Input:** Rendered text  
**Output:** Token stream with positions

Tokenization splits text into the smallest meaningful units. Critical details:

**Sub-word tokenization (BERT/WordPiece):**
- "SEO" → single token
- "unknownword" → ["unknown", "##word"] (## = continuation token)
- "IT support" → ["IT", "support"] — two tokens, but treated as unit by later stages
- "IT-support" → ["IT", "-", "support"] — punctuation splits are significant

**Multi-word expressions (MWEs):**
Modern engines recognize compound entities as single tokens: "Google Search Console", "small business IT support", "managed service provider" → each treated as one semantic unit, not three separate words.

**Position encoding:**
Every token carries positional metadata. Tokens in H1 position are weighted differently from tokens in body paragraph 7. This is how heading-level weight is applied without explicit rules — it's baked into the position encoding.

**GeekSEO implication:** The system currently treats H1, H2, H3 as distinct sources with different evidence weights. This is the correct approximation of position encoding for a rule-based system.

---

## Stage 3: Part-of-Speech (POS) Tagging

**Input:** Token stream  
**Output:** Token stream + POS label per token

Every token receives a grammatical role:
- `NN` = noun (singular)
- `NNS` = noun (plural)  
- `NNP` = proper noun (singular) — most important for entity detection
- `NNPS` = proper noun (plural)
- `VB` = verb
- `JJ` = adjective
- etc.

**Why engines do this:**
Entities are almost always nouns or noun phrases (`NNP`, `NN`, `NNS`). POS tagging pre-filters the token stream for NER — only noun-tagged tokens and their surrounding context enter the NER pipeline. This is a massive efficiency gain.

**Practical effect:**
"We provide **accounting software** for **restaurants**" — "accounting software" and "restaurants" are noun phrases → NER candidates. "provide", "for" are filtered. "We" is a pronoun → passes to coreference, not NER.

**GeekSEO implication:** The system doesn't run POS tagging. This is acceptable for Phase A (rule-based extraction) but becomes a gap when doing body text entity extraction. Phase B's NER pass will need this.

---

## Stage 4: Dependency Parsing

**Input:** Token stream + POS tags  
**Output:** Dependency tree (directed graph of grammatical relationships)

Dependency parsing maps how words relate to each other:

```
"Geek At Your Spot provides IT support in Miami"
                    ↑
         nsubj ──► provides ◄── dobj
         │                        │
    "Geek At Your Spot"      "IT support"
                                    │
                              prep (in)
                                    │
                                 "Miami"
```

**Critical for entity-attribute assignment:**
- "Geek At Your Spot" = subject (NSUBJ) of "provides" → the entity doing the action
- "IT support" = direct object (DOBJ) of "provides" → the service offered
- "Miami" = prepositional object (POBJ) of "in" → geographic attribute

Without dependency parsing, "IT support in Miami" and "Miami IT support" could be treated identically. Parsing assigns "Miami" as an attribute of the service, not a separate entity.

**BERT's improvement over earlier parsers:**
BERT uses bidirectional attention — it reads the full sentence before assigning relationships. Older sequential parsers read left-to-right only. BERT correctly handles: "The support, which was IT-focused, was provided in Miami" even though "support" and "IT-focused" are separated.

**GeekSEO implication:** GeekSEO doesn't parse dependency trees. For the current signal set (schema, nav, sitemap, headings), this is not needed — these sources are already structured. For Phase B body text NER, a lightweight dependency parse would improve attribute assignment (e.g., correctly associating "Miami" with "IT support" rather than as a standalone topic).

---

## Stage 5: Named Entity Recognition (NER)

**Input:** Dependency-parsed token stream  
**Output:** Labeled entity spans with type classification

NER identifies spans of text that refer to real-world entities and classifies them:

**Google's entity types (confirmed via Natural Language API):**
| Type | Examples |
|------|---------|
| `PERSON` | Jeff Martin, Elon Musk |
| `ORGANIZATION` | Google, Geek At Your Spot |
| `LOCATION` | Miami, Broward County, Florida |
| `EVENT` | Super Bowl, Tax Season |
| `CONSUMER_GOOD` | iPhone, QuickBooks |
| `WORK_OF_ART` | specific named products/publications |
| `ADDRESS` | 123 Main St, Fort Lauderdale FL |
| `DATE` | Q1 2026, March 15 |
| `NUMBER` | quantities, measurements |
| `PRICE` | $150/hr, $99/month |
| `OTHER` | concepts not fitting above categories |

**How the model works (transformer-based NER):**
1. Each token is encoded as a contextual vector (embedding)
2. A classification head predicts entity type + span boundaries
3. Output: `[(start_token, end_token, entity_type, confidence)]`
4. Multi-word spans: "Fort Lauderdale" → one entity span, not two

**Training data source:**
Models trained on Wikipedia, Freebase, and web crawl data. Wikipedia anchors (hyperlinks) are gold-standard training labels — a Wikipedia link is explicit entity annotation at massive scale.

**Domain-specific accuracy:**
General NER models achieve 85-92% precision on standard types (PERSON, LOCATION, ORGANIZATION). Accuracy drops for domain-specific concepts like "managed service provider" or "offerCatalog" — these require custom entity registries or fine-tuned models.

**GeekSEO current implementation vs. true NER:**
The current extractors (schema, nav, sitemap, headings) are structured-source parsers — they read declared values, not free text. True NER over body text is Phase B territory. The `PageContentExtractor` currently extracts H3 headings and list items (structured elements), not unstructured body paragraphs. This is intentionally conservative and architecturally correct for Phase A.

---

## Stage 6: Entity Resolution (Entity Linking)

**Input:** NER entity spans  
**Output:** Entity spans + Knowledge Graph node IDs (MIDs)

Entity resolution is where raw text mentions become graph nodes. This is the most complex stage and the key differentiator between engines.

**The problem it solves:**
"Apple" in "Apple IT support for small businesses" ≠ "Apple" in "Apple pie recipe". The string is identical; the entity is completely different. Entity resolution disambiguates using context.

**Google's resolution mechanism:**
1. **Candidate retrieval:** For entity mention "Apple", retrieve all KG nodes matching that surface form (Apple Inc., apple (fruit), Apple Records, etc.)
2. **Context scoring:** Score each candidate by contextual fit — surrounding entities (IT, support, business) strongly predict Apple Inc.
3. **PageRank-initialized ranking:** Candidates pre-ranked by their KG node's importance (Apple Inc. has massive PageRank → default candidate)
4. **Iterative refinement:** Score inter-candidate consistency — "Apple Inc." and "IT support" and "small business" form a coherent entity set; "apple (fruit)" does not
5. **Assignment:** Highest-scoring candidate is assigned. Unresolvable mentions receive no MID.

**Google's MID system:**
- `/m/` prefix — Freebase-era identifiers (legacy, still valid)
- `/g/` prefix — Google-native identifiers (newer entities)
- No MID = unresolved entity. Google may know the mention exists but can't link it to a KG node.

**Consequence of unresolved entities:**
An entity with high salience but no MID is known to Google as a text pattern, not a concept. It cannot inherit KG attributes (category, location, related entities). This is why `sameAs` schema markup is critical — it provides an explicit resolution path.

**`sameAs` as a resolution shortcut:**
```json
{
  "@type": "Organization",
  "name": "Geek At Your Spot",
  "sameAs": [
    "https://en.wikipedia.org/wiki/Geek_At_Your_Spot",
    "https://www.linkedin.com/company/geek-at-your-spot",
    "https://www.google.com/maps/place/..."
  ]
}
```
This tells the engine: "Geek At Your Spot" = this specific KG node. No disambiguation needed. The entity inherits all attributes already in the KG for that node.

**Multi-source confirmation requirement:**
Google creates new KG nodes only when multiple trustworthy sources agree. A single site declaring itself an entity is not sufficient. Wikipedia articles, industry directories, news mentions, and Google Business Profile all contribute to node creation.

**GeekSEO gap (critical):**
The system extracts `sameAs` arrays but does not use them for entity resolution scoring. A site with proper `sameAs` markup should receive higher entity confidence across all extracted topics — the brand entity is resolved, and by association, its service entities inherit higher confidence. This is Phase B.5 work.

---

## Stage 7: Salience Scoring

**Input:** Resolved entity set (with MIDs where available)  
**Output:** Salience score (0.0–1.0) per entity

Salience measures how central an entity is to the document — not how often it appears, but how much the document is fundamentally about that entity.

**Salience factors (confirmed via Google NL API documentation and research):**

| Factor | Weight | Description |
|--------|--------|-------------|
| **Position** | High | Title, H1, early paragraph → higher salience |
| **Frequency** | Medium | More mentions → higher salience, with diminishing returns |
| **Co-occurrence strength** | High | Entity co-occurs with topic-confirming entities → higher salience |
| **Dependency role** | Medium | Subject of sentence vs. peripheral mention |
| **KG relationship density** | Medium | Entity has many KG relationships relevant to page context |
| **Section prominence** | Medium | Appears in multiple H2 sections vs. single paragraph |

**The competitive scaling rule:**
Salience is relative, not absolute. Every entity on the page competes for salience budget. If a page about "IT support" also extensively discusses "coffee" (unrelated), the coffee mentions reduce IT support's salience even if IT support appears more often. This is why topic focus matters — dilution is real.

**Salience as a content quality proxy:**
A page where the primary topic entity (e.g., "IT support") holds salience 0.85+ is strongly topically focused. A page where the primary entity holds 0.40 and 12 other entities each hold 0.05 is unfocused. Focused pages rank more stably.

**Google NL API formula (reconstructed from research):**
```
salience(entity_i) = (
  position_weight(entity_i) ×
  frequency_normalized(entity_i) ×
  co_occurrence_score(entity_i, page_entity_set)
) / sum(salience_raw for all entities)
```
Normalized so all entity saliences sum to 1.0.

**GeekSEO mapping:**
The confidence score in `TopicFusionEngine` is a structural proxy for salience:
- Schema evidence weight = 0.35 (high — structured declaration = position weight equivalent)
- Sitemap evidence weight = 0.25 (dedicated URL = section prominence equivalent)
- Nav evidence weight = 0.20 (navigation = structural prominence equivalent)
- Page evidence weight = 0.15 (body text frequency equivalent)
- Heading evidence weight = 0.10 (heading position, lower than schema)

This maps well. The key missing piece: body text is currently only extracted as structured elements (lists, H3s), not as full NER over paragraphs. Adding raw paragraph NER would enable true salience measurement from body text.

---

## Stage 8: Semantic Indexing

**Input:** All resolved entities with salience scores  
**Output:** Index entry: page → entity set with weights + topic classification

This is the final stage before ranking. The page is stored in the index not as a keyword bag but as a **weighted entity graph**.

**Index structure (simplified):**
```
URL: https://example.com/services/it-support
Entities:
  - "IT support" (MID: /m/abc123) → salience: 0.82, type: CONSUMER_GOOD
  - "small business" (MID: /m/def456) → salience: 0.61, type: OTHER
  - "Broward County" (MID: /m/ghi789) → salience: 0.44, type: LOCATION
  - "managed services" (MID: /m/jkl012) → salience: 0.38, type: CONSUMER_GOOD
Topic classification: IT_SERVICES > MANAGED_IT > LOCAL
siteAuthority: 42 (0-100 scale)
NormalizedTopicality: 0.79
sourceType: editorial (1)
anchorTextIn: ["IT support Miami", "managed IT Broward", "computer help"]
```

**`NormalizedTopicality` (confirmed in 2024 Google API leak):**
A normalized score (0–1) representing how much of the entire document discusses the primary entity. This is the document-level equivalent of entity salience. Pages with NormalizedTopicality > 0.75 for a given topic rank more consistently for that topic's queries.

**`siteAuthority` (confirmed in 2024 Google API leak):**
Domain-wide quality score (0–100). Aggregates page-level signals site-wide. Contradicts years of Google denials about domain authority. Influences ranking potential of all pages on the domain.

**`sourceType` (confirmed in 2024 Google API leak):**
Editorial content (1) receives ~3× ranking weight advantage over user-generated content (2) or syndicated content (3). This is why scraped or AI-spun content underperforms even with correct entity coverage.

**`anchorMismatchDemotion` (confirmed in 2024 Google API leak):**
-0.15 modifier per occurrence where anchor text misaligns with target page topic. If external sites link to your "IT support" page with anchor "click here", those links reduce topical signal strength.

---

## The Entity-to-Keyword Bridge

This is the critical mechanism connecting entity-level understanding to keyword-level ranking outcomes.

### How It Works

Traditional model (pre-2012): 
```
Query: "IT support for small business Miami"
     ↓ keyword match
Page must contain those exact words
```

Entity model (current):
```
Query: "IT support for small business Miami"
     ↓ NLP parse
Entities: {IT_support, small_business, Miami}
     ↓ entity resolution
KG nodes: {/m/it_support, /m/small_biz, /m/miami_fl}
     ↓ entity index lookup
Pages indexed for these KG nodes → ranked by salience + authority
```

**The key insight:** A page that never contains the exact phrase "IT support for small business Miami" can rank for that query if its entity index entry contains `/m/it_support`, `/m/small_biz`, and `/m/miami_fl` with high salience scores.

### Query Expansion via Entity Space

When Google receives a query, it expands it through the Knowledge Graph:

```
Query entity: IT support (/m/abc123)
KG expansion:
  - Related entities: managed services, helpdesk, technical support, MSP
  - Co-occurring entities: small business, cybersecurity, cloud services
  - Geographic variants: [city] IT support, [city] computer repair
  - Intent variants: IT support cost, IT support near me, IT support company
```

This expansion is why a page about "IT support" naturally ranks for "helpdesk services" and "managed services" — those KG nodes co-occur reliably with the primary entity across the training corpus.

**Practical effect:**
A site with high topical authority for "IT support" (high siteAuthority + high NormalizedTopicality across multiple pages) ranks for hundreds of keyword variants it has never explicitly targeted, because those keywords resolve to the same entity cluster in query time.

### Topical Authority as Ranking Multiplier

Site-level topical authority functions as a multiplier on individual page rankings:

```
page_rank_score = base_relevance × (1 + topical_authority_bonus)
```

Where `topical_authority_bonus` comes from:
1. **Site coverage breadth** — how many subtopics of the primary entity does the site cover?
2. **Internal link coherence** — do internal links form a consistent entity cluster?
3. **siteAuthority** — domain-wide quality score
4. **External validation** — do backlinks with relevant anchor text confirm the entity cluster?

**Coverage score (confirmed via research, not leaked):**
```
coverage_score = entities_present_on_site / entities_expected_for_topic
```
Sites with coverage_score > 0.80 for a given topic receive the topical authority multiplier. Below 0.60 → "entity-thin" classification → reduced topical multiplier.

**What "entity-thin" means in practice:**
A site covering "IT support" but with zero content about "cybersecurity", "cloud backup", "network monitoring", or "helpdesk software" has low entity coverage for the IT Services topic cluster. Google's KG knows these entities co-occur on authoritative IT support sites. Missing them = incomplete topical signal = lower authority multiplier.

### Vector Space: The Mathematical Underpinning

Engines represent entities, queries, and documents as vectors in high-dimensional semantic space:

**Word2Vec / Entity2Vec model:**
- Each entity → dense vector (e.g., 300 dimensions)
- Vectors trained on co-occurrence in the web corpus
- "IT support" vector is close to "managed services", "helpdesk", "MSP" in vector space
- "IT support" vector is far from "accounting", "plumbing", "restaurant" in vector space

**Document scoring:**
```
relevance(doc, query) = cosine_similarity(doc_vector, query_vector)
```

Where `doc_vector` = weighted sum of entity vectors, weighted by salience scores.

**Two-phase retrieval:**
1. **Phase 1 (recall):** BM25 / keyword matching retrieves candidate documents quickly
2. **Phase 2 (rerank):** Entity vector similarity reranks candidates by semantic relevance

This is why keyword presence is still necessary (Phase 1 gates) but not sufficient (Phase 2 decides). A page must contain the terms to be retrieved, but entity vector alignment determines final ranking.

### Navboost — User Signal Layer

After entity-based ranking, Navboost applies user behavior signals as a re-ranking filter:

**Confirmed signals (2024 API leak):**
- **lastLongestClicks** — users who spent significant time on the page without returning to SERP → positive signal
- **pogo-sticking** — users who immediately returned to SERP → strong negative signal
- **CTR** — click-through rate vs. expected for position
- **chromeInTotal** — Chrome browser interactions beyond search (direct navigation, bookmarks)

**What this means for entity understanding:**
If a page has correct entity coverage but poor user engagement, Navboost will suppress it. If a page has slightly lower entity coverage but excellent user engagement, Navboost will boost it. User behavior acts as ground truth that validates or overrides the entity-based ranking.

**GeekSEO implication:** Navboost signals require GSC/GA4 integration (Phase D). But the entity-based foundation must be solid before engagement signals can help — you can't Navboost your way past fundamentally wrong entity coverage.

---

## Full Pipeline: End-to-End Example

**Target site:** A local IT support company in Miami

### What the engine receives:
```html
<h1>IT Support for Small Businesses in Miami</h1>
<script type="application/ld+json">{
  "@type": "LocalBusiness",
  "name": "TechHelp Miami",
  "serviceType": "IT Support",
  "knowsAbout": ["Managed IT Services", "Cybersecurity", "Cloud Backup"],
  "areaServed": "Miami-Dade County",
  "sameAs": "https://en.wikipedia.org/wiki/..."
}</script>
<nav>
  <a href="/managed-it">Managed IT</a>
  <a href="/cybersecurity">Cybersecurity</a>
  <a href="/cloud-backup">Cloud Backup</a>
</nav>
```

### Stage-by-stage output:

**Stage 2 (Tokenize):** ["IT", "Support", "for", "Small", "Businesses", "in", "Miami", ...]

**Stage 3 (POS):** IT(NNP), Support(NNP), Small(JJ), Businesses(NNS), Miami(NNP) → noun phrases selected

**Stage 4 (Dependency):** "IT Support" ← subject, "Small Businesses" ← beneficiary, "Miami" ← location attribute

**Stage 5 (NER):**
- "IT Support" → CONSUMER_GOOD, confidence: 0.94
- "Small Businesses" → OTHER/ORG_TYPE, confidence: 0.87
- "Miami" → LOCATION, confidence: 0.99
- "TechHelp Miami" → ORGANIZATION, confidence: 0.91

**Stage 6 (Entity Resolution):**
- "IT Support" → /m/it_support_services (resolved)
- "Miami" → /m/miami_florida (resolved, high-confidence location)
- "Managed IT Services" → /m/managed_services (resolved from schema)
- "Cybersecurity" → /m/cybersecurity (resolved from schema)
- "TechHelp Miami" → no MID (new entity, unresolved without external KG node)
- `sameAs` Wikipedia URL → if valid, TechHelp Miami resolves to KG node

**Stage 7 (Salience):**
- "IT Support" → 0.82 (H1 + schema + nav + body)
- "Miami" → 0.61 (schema + H1 + body)
- "Managed IT" → 0.44 (schema + nav + dedicated URL)
- "Cybersecurity" → 0.38 (schema + nav + dedicated URL)
- "Small Businesses" → 0.29 (H1 + body)
- "Cloud Backup" → 0.31 (schema + nav + dedicated URL)

**Stage 8 (Index entry):**
```
NormalizedTopicality: 0.82 (IT Services topic)
Primary entity: IT Support (/m/it_support_services)
Entity cluster: {managed_services, cybersecurity, cloud_backup, small_business}
Geographic scope: Miami-Dade County (/m/miami_dade)
siteAuthority: [computed from link graph]
sourceType: editorial (1)
Topic classification: IT_SERVICES > LOCAL_MSP > MIAMI
```

**Query time:** "best IT support company Miami" →
- Query entities: {IT_support, Miami} resolved to {/m/it_support_services, /m/miami_florida}
- Index lookup: all pages with high salience for these entity pair
- TechHelp Miami page is a strong candidate (salience: IT_support=0.82, Miami=0.61)
- Navboost layer applies user signal adjustments
- Final rank determined

**The page never needed to contain "best IT support company Miami" verbatim.**

---

## What This Means for GeekSEO — Mirroring the Pipeline

### Current alignment (Phase A):

| Engine Stage | GeekSEO Equivalent | Fidelity |
|-------------|-------------------|---------|
| Stage 1: Crawl/Render | Playwright + HTTP fallback | ✅ High |
| Stage 2: Tokenization | Implicit in C# string parsing | ✅ Adequate |
| Stage 3: POS Tagging | Not implemented | ⚠️ Gap (acceptable for Phase A) |
| Stage 4: Dependency Parsing | Not implemented | ⚠️ Gap (acceptable for Phase A) |
| Stage 5: NER | Schema/Nav/Sitemap parsing (structured sources) | ✅ High (structured) / ❌ Gap (free text) |
| Stage 6: Entity Resolution | `sameAs` extracted but not scored | ⚠️ Partial |
| Stage 7: Salience Scoring | `TopicEvidenceWeights` + confidence stacking | ✅ Good proxy |
| Stage 8: Semantic Indexing | `FusedSiteUnderstanding` → `NichePillar[]` | ✅ High |

### Priority gaps to close:

**Gap 1 — Free text NER (Phase B):**
The engine runs NER over all body text, not just structured elements. GeekSEO currently processes H3 headings and list items from page content. Adding a basic NER pass over `<p>` text would surface entities that appear in structured body copy but not in schema/nav/headings.
- Implementation: integrate a lightweight NER library (e.g., spaCy via Python sidecar, or Azure Cognitive Services NL API, or Google Cloud NL API)
- Cost: Google NL API free tier = 5,000 requests/month. Paid after that.
- Alternative: build a simple rule-based entity extractor using noun phrase patterns (cheaper, lower fidelity)

**Gap 2 — Entity Resolution via `sameAs` (Phase B.5):**
Extract `sameAs` URLs from schema. Check against known authority sources (Wikipedia, Wikidata, LinkedIn, Google Maps). If match found, mark entity as "resolved" with higher base confidence.
- Resolved entity confidence multiplier: +0.20 added to base schema weight
- Source: `"same_as"`, weight: 0.30

**Gap 3 — Coverage Score vs. Topic Cluster (Phase C):**
The engine compares a site's entity set against the expected entity set for a topic (derived from what authoritative sites in that category cover). GeekSEO should do the same — take the pillar topic, fetch top SERP results for it, extract their entity sets, compare.
- `coverage_score = site_entities_present / competitor_entities_expected`
- Score < 0.60 → flag as "entity-thin" with specific gap list
- Score > 0.80 → high topical authority confirmed

**Gap 4 — NormalizedTopicality per pillar (Phase C):**
For each selected pillar, compute a normalized score: what fraction of the site's content (pages × word count) addresses this pillar vs. other topics? This mirrors Google's `NormalizedTopicality` signal.
- Sites with one strong pillar and no dilution rank more stably than sites with 12 equal pillars and no depth
- Report: "Accounting: 34% of site content, IT Support: 22%, Marketing: 8% — Accounting is your primary topical signal"

**Gap 5 — Internal link entity graph (Phase B):**
When `InternalLinkExtractor` runs, it should not just count links — it should build an entity graph:
- Node = topic/pillar
- Edge = internal link between pages covering those topics
- Edge weight = anchor text relevance to target entity
- Output: which pillar pages are properly interlinked (entity cluster coherence)
- Output: which pillars are orphaned (no internal linking = weak topical signal)

---

## Confidence Score Recalibration Recommendation

Based on this research, the current `TopicEvidenceWeights` are well-designed but could be tuned:

**Current:**
```
Schema = 0.35
Sitemap = 0.25  
Nav = 0.20
Page = 0.15
Heading = 0.10
```

**Recommended revision (post-Phase B):**
```
Schema = 0.35       (keep — explicit declaration, highest engine weight)
SameAs = 0.30       (NEW — entity resolution confirmation, very high signal)
Sitemap = 0.22      (slight reduce — structural but not semantic)
Nav = 0.18          (slight reduce)
InternalLink = 0.18 (NEW — T* signal, Google-confirmed)
UrlPattern = 0.12   (NEW — slug-level entity confirmation)
Page = 0.14         (slight reduce)
Heading = 0.10      (keep)
PageVertical = 0.12 (slight increase — H3 section structure)
```

Stacking remains capped at 1.0. New sources don't reduce existing signal value — they provide additional paths to higher confidence.

---

## Appendix: Key Technical Terms

| Term | Definition |
|------|-----------|
| **MID** | Machine Identifier — Google's unique ID for a Knowledge Graph entity |
| **NER** | Named Entity Recognition — ML model that identifies entity spans in text |
| **Entity Resolution** | Mapping a text mention to a specific KG node (disambiguation) |
| **Salience** | How central an entity is to a document (0.0–1.0 scale, competitive) |
| **NormalizedTopicality** | What fraction of a document addresses a topic (0.0–1.0) |
| **siteAuthority** | Google's domain-wide quality score (0–100, confirmed in 2024 leak) |
| **Navboost** | User behavior re-ranking layer applied after entity-based ranking |
| **BM25** | Classical term-frequency retrieval model used for Phase 1 candidate recall |
| **Entity2Vec** | Dense vector representation of entities trained on co-occurrence |
| **Coverage Score** | Site entity set / expected topic entity set — topical completeness metric |
| **T\* Signal** | Google's internal topicality signal combining anchor text, body text, clicks |
| **Wave 1 / Wave 2** | Google's two-pass rendering: HTML (immediate) + JS render (queued) |
| **POS Tagging** | Part-of-speech classification per token (noun, verb, adjective, etc.) |
| **Dependency Parsing** | Grammatical relationship mapping between tokens in a sentence |
| **Coreference Resolution** | Identifying that "it", "they", "the company" all refer to the same entity |

---

## Sources

- [Google NLP SEO: How BERT, NER & MUM Process Content](https://squin.org/semantic-seo/how-google-nlp-works/)
- [Entity Extractions for Knowledge Graphs — Go Fish Digital](https://gofishdigital.com/blog/entity-extractions-knowledge-graphs/)
- [Google: A New Entity Salience Task with Millions of Training Examples](https://research.google/pubs/a-new-entity-salience-task-with-millions-of-training-examples/)
- [How Google uses NLP to understand queries and content — Search Engine Land](https://searchengineland.com/how-google-uses-nlp-to-better-understand-search-queries-content-387340)
- [Entity-Based SEO: Topical Authority Technical Guide — DEV Community](https://dev.to/marcus_agentic/entity-seo-for-topical-authority-a-technical-implementation-guide-3ebb)
- [How Google identifies documents via entities, NLP & vector space analysis](https://www.kopp-online-marketing.com/entities-nlp-vector-space-analysis)
- [Entity Salience: Google's Content Appraisal System — BullZeye](https://thebullzeye.com/entity-salience-google-content-appraisal-system/)
- [May 2024 Google Content Warehouse API Leak — Complete SEO Playbook](https://williejiang.com/en/blog/the-may-2024-google-content-warehouse-api-leak-your-complete-seo-playbook/)
- [NavBoost Unpacked — SEO Stack Blog](https://www.seo-stack.io/blog/navboost-unpacked-what-the-google-content-warehouse-leak-actually-tells-us-about-click-based-ranking/)
- [Google Search Pipeline: Retrieval to Ranking & AI Answers](https://www.szymonslowik.com/how-search-engines-work-retrieval-ranking-ai/)
- [Entity-Based Internal Linking: Topical Authority Framework](https://ranktraq.com/blog/entity-based-internal-linking-a-framework-for-topical-authority-and-sema)
- [Semantic Depth in SEO — Search Engine Land](https://searchengineland.com/guide/semantic-depth)
- [Knowledge Graph Node Engineering — SearchEngineZine](https://searchenginezine.com/technical/structure/knowledge-graph-node/)
- [AI Search Entity Recognition — iPullRank](https://ipullrank.com/ai-search-entity-recognition)

---

*Document created: 2026-06-06*
*Status: Research complete*
*Next: Use Gap 1–5 to scope Phase B and Phase B.5 implementation plan*

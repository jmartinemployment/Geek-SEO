# Content Writer — Future Features
## Derived from Competitive Reverse Engineering (2026-06-10)

Sources: Frase.io, NeuronWriter, Clearscope, Semrush Content Platform, MarketMuse.  
Existing Surfer/SurferAI analysis: [`competitor-analysis.md`](competitor-analysis.md).  
Build queue: [`TODO.md`](TODO.md).

---

## Key Finding Across All Five Tools

Every tool is built on the same data sources we already have:
- **SERP data** → Serper.dev (we have this)
- **Competitor page content** → crawl/scrape top-10 URLs
- **GSC** → free, licensed, our cleanest data source
- **LLM** → Claude API (we have this)

Margin is presentation + workflow, not proprietary data. We can replicate all of it.

---

## Feature 1 — SERP-Grounded Term Analysis (Scoring v2)

**Replaces:** Current `ContentScoringService` v1 (keyword word-split, six bars)  
**Modeled on:** Surfer NLP terms · NeuronWriter NLP capsules · Clearscope term importance bars · MarketMuse topic model

### What it does
Fetch top-10 SERP results for the target keyword + geo. Extract headings and entities from each competitor page. Build a co-occurrence matrix — terms appearing in 40%+ of top results are "required," 20%+ are "important," 10%+ are "supplemental." Score the draft against that set in real time.

### Editor sidebar
- Term table: each term shows type (Primary / Semantic / LSI), recommended count, actual count, fill bar
- Filter: All / Used / Missing
- Missing filter is the core workflow — shows exactly what to add next
- Score updates live as terms are typed

### Data model additions
```
TopicModels       keyword + geo + ModelJson (required/important/optional)
SubTopics         term, co-occurrence score, tier, recommended mentions
ContentAudits     page vs. topic model score + gap list
```

### Grading (Clearscope-style)
Replace 0–100 integer with letter grade: A++ → A+ → A → B+ → B → C+ → C → D → F  
Thresholds: A = 80+, B = 64+, C = 45+, D = 35+, F < 35.  
Letter grade is more motivating UX than a raw number.

### Readability dimension
Add Flesch-Kincaid as a scored dimension alongside SEO score.  
Formula: `206.835 − 1.015(words/sentences) − 84.6(syllables/words)`  
Show: score + grade level label (e.g. "Standard — 8th grade").

### Build notes
- Serper.dev SERP call already in pipeline; add competitor URL fetch
- Phase 1 MVP: use Serper.dev snippets + headings from result (no full page crawl)
- Phase 2: fetch full competitor page content via Playwright or DataForSEO content API (~$0.002/page) for true co-occurrence matrix
- Claude prompt: extract entities + headings from snippet array → return required/important/optional JSON

---

## Feature 2 — PAA Database + Editor Integration

**Modeled on:** Frase PAA tab · NeuronWriter PAA insert · Clearscope questions panel

### What it does
Replace `SeoSerpResult.PeopleAlsoAskJson` blob with dedicated normalized tables. Surface PAA questions in the editor sidebar — click to insert as H2 with a 40–60 word answer prompt below it.

### Data model (new tables)
```
PaaSeedQuery          query text, geo, scraper source, scraped at
PaaQuestion           question text, hash (dedup), first/last seen
PaaSeedQueryQuestion  junction: position, depth (0=top-level, 1=expanded), parent
PaaSnapshot           snippet text, source URL, source domain, snapshot at
PaaAnswerAnalysis     word count, heading match type, answer pattern, has FAQ schema
```

### Answer patterns Google rewards
| Question type | Winning opener |
|---------------|---------------|
| "What is..." | "X is a/the..." |
| "How much does..." | "X typically costs..." |
| "How do I..." | "To [verb], you..." |
| "Why does..." | "X [verbs] because..." |

Target: 40–60 words, declarative, starts with subject, single paragraph.

### FAQ schema generation
When a PAA question is inserted and answered, auto-generate `FAQPage` JSON-LD block. Inject on publish. Directly feeds AEO/GEO citation readiness.

### Source
Serper.dev returns PAA in same call as organic results — zero additional cost per query. DataForSEO for depth-1 expansion (when clicked PAAs load more PAAs) if needed later.

---

## Feature 3 — Content Brief Generator

**Modeled on:** Frase brief · Semrush SEO Content Template · MarketMuse briefs

### What it does
Given a target keyword + geo, generate a structured brief that tells Claude exactly how to write the article.

### Brief data structure
```json
{
  "keyword": "managed IT services Miami",
  "geo": "Fort Lauderdale, FL",
  "intent": "commercial_investigation",
  "targetWordCount": 2100,
  "contentScoreTarget": 45,
  "requiredSubtopics": [
    { "term": "IT support contracts", "mentions": 3, "headingRequired": true }
  ],
  "suggestedH2s": ["What Does Managed IT Include?", "How Much Does IT Support Cost?"],
  "paaQuestions": ["How much does managed IT support cost?"],
  "competitors": [{ "url": "...", "contentScore": 67, "wordCount": 2400 }],
  "localEntities": ["Miami-Dade", "Brickell", "South Florida"]
}
```

### EF Core entity additions
Extend existing `SeoContentDocument` or create `SeoContentBrief`:
- `RequiredSubtopicsJson`
- `SuggestedH2sJson`
- `PaaQuestionsJson`
- `CompetitorBenchmarksJson`
- `LocalEntitiesJson`
- `TargetWordCount`
- `ContentScoreTarget`

### Claude prompt injection
Feed brief fields directly into the article generation prompt:
```
Write a {targetWordCount}-word article about "{keyword}" for {geo}.
Required subtopics to cover: {requiredSubtopics}
Structure headings around these questions: {paaQuestions}
Answer each question in 40–60 words before expanding.
Local entities to reference naturally: {localEntities}
```

---

## Feature 4 — Topic Clustering (MarketMuse-style)

**Modeled on:** MarketMuse clusters · Semrush Topic Research · Surfer Topical Map

### What it does
Takes a keyword universe (from GSC + seed keywords), groups them into topic clusters, assigns pillar vs. supporting page roles, generates internal link map.

### Claude-powered clustering (no vector DB needed at this scale)
```
System: You are a topic clustering engine. Given a keyword list for a {industry} 
site serving {geo}, group into clusters with one pillar keyword (broadest/highest 
intent) and supporting keywords. Return JSON only.
```
Achieves 80% of MarketMuse's result at 5% of the complexity. Works up to ~500 keywords before needing embeddings.

### Data model
```
TopicClusters     name, pillar keyword, authority score, site ID
ClusterPages      cluster ID, URL, title, content score, word count, status
```

### Personalized difficulty (MarketMuse differentiator)
```
personalized_difficulty = generic_serp_difficulty × (1 / site_topic_authority)
```
Site authority = count of published pages in cluster × avg content score. Proxy for "how hard is this keyword for THIS site specifically." Show alongside generic KD.

---

## Feature 5 — Site Content Audit (MarketMuse inventory)

**Modeled on:** MarketMuse content audit · Frase content monitoring · Semrush post tracking

### What it does
GSC-driven scan of all indexed pages. Score each page against its topic model. Surface prioritized action list sorted by ROI (high-traffic pages with low content scores = highest priority).

### Action types
| Action | Trigger |
|--------|---------|
| `HIGH_ROI_OPTIMIZE` | ContentScore < 40 AND GscClicks > 50 |
| `EXPAND` | GapTopicsCount > 5 |
| `MAINTAIN` | ContentScore > 70 |
| `MONITOR` | Everything else |

### Cannibalization detection
Two+ pages in same cluster with ContentScore > 30 = cannibalization risk. Flag with merge/redirect recommendation.

### Striking distance alerts
Pages at GSC position 7–15 for a keyword but missing 2–3 required subtopics. "Add these sections to move from position 11 to top 5."

---

## Feature 6 — AI Visibility Tracking (GEO)

**Modeled on:** Frase GEO · Semrush AIO · Surfer AI Visibility Tracker

### What it does
Scheduled probes that query ChatGPT, Perplexity, Gemini, Claude with brand-relevant prompts. Parse responses for brand mentions, competitor mentions. Track share of voice over time.

### Metrics
- **Appearance rate** — % of probes where brand appears
- **Authority rate** — % where brand is cited as a source
- **Share of voice** — brand mentions / total brand + competitor mentions
- **Momentum** — 30-day trend

### Our advantage over Semrush AIO
We don't audit for AEO compliance after the fact — we bake it into generation by default:
1. Direct answer paragraph in first 100 words
2. FAQ section with schema-ready Q&A pairs
3. Headings that mirror PAA phrasing
4. `llms.txt` guidance

Our generated content is AEO-ready out of the box. Semrush charges enterprise pricing to tell you it isn't.

### Build notes
- Extend existing `SeoGeoTrackingQuery` + `SeoGeoMentionSnapshot` tables (already in schema)
- `SeoMaintenanceWorker` runs probes on schedule
- Multi-LLM currently waived (#20 in TODO.md) — pick up here

---

## Feature 7 — Content Monitoring / Decay Alerts

**Modeled on:** Frase Content Guard · Semrush post tracking · MarketMuse content audit

### What it does
GSC-connected decay detection. Page loses >20% impressions or drops >3 positions over 30 days = alert. Surfaces: topic model gap causing the drop + recommended fix.

### Alert types
- **Decay alert** — traffic/ranking drop detected
- **Opportunity alert** — page at position 7–15 (striking distance)
- **Gap alert** — required subtopics added to topic model since last publish

Already partially built as `PublishedContentAuditService` + `SeoContentGuardPolicy`. Extend with topic model diff.

---

## Feature 8 — Programmatic SEO Templates

**Modeled on:** Frase programmatic SEO · Semrush content templates

### What it does
Template → bulk generation. One template + data source (CSV, DB table, location list) → 100+ pages.

Use cases:
- `{service} in {city}` — location pages within service radius
- `{service} vs {competitor}` — comparison pages
- `{keyword} cost in {city}` — pricing pages

### Build notes
- Lower priority for current market (SMBs, not agencies at scale)
- Ties into Local Service Area feature — radius-derived city list feeds location page templates
- `SeoBulkJob` entity already exists; extend for template-based generation

---

## Architecture Notes

### Where this code lives
All content writer features belong in **GeekSeoBackend** (not a new service):
- Topic model building → new `TopicModelService`
- Brief generation → extend `IContentBriefService`  
- Cluster scoring → new `TopicClusteringScorer`
- PAA persistence → new HTTP repo + tables via GeekRepository

### Cost per analysis (estimated)
| Step | Source | Cost |
|------|--------|------|
| SERP fetch (10 results + PAA) | Serper.dev | ~$0.001 |
| Competitor page fetch (10 pages) | Playwright self-hosted | ~$0.00 |
| Topic model extraction | Claude API | ~$0.02 |
| Content brief generation | Claude API | ~$0.03 |
| Article generation | Claude API | ~$0.08 |
| **Total per full brief + article** | | **~$0.13** |

At $49/month pricing with 20 articles/month = $2.60 API cost per user. Healthy margin.

### Geo advantage (SaaS-wide, not just local)
All five tools build topic models from national SERPs. Our Serper.dev integration includes `gl`, `hl`, and `location` params — topic models reflect what actually ranks in the user's target market. This applies to any geo, not just South Florida.

---

## Priority Order

| Priority | Feature | Depends on |
|----------|---------|------------|
| P0 | SERP-grounded term analysis (Scoring v2) | Serper.dev wired (TODO P0) |
| P0 | PAA tables + editor tab | Serper.dev |
| P1 | Content Brief entity + generation | Topic model + PAA |
| P1 | Letter grade (A++ → F) | Scoring v2 |
| P1 | Flesch-Kincaid readability dimension | Scoring v2 |
| P2 | Topic clustering (Claude-powered) | GSC OAuth (#27) |
| P2 | Site content audit + ROI sort | GSC OAuth + topic models |
| P2 | Striking distance alerts | GSC OAuth |
| P3 | AI visibility tracking (multi-LLM) | Existing GEO infra |
| P3 | Programmatic SEO templates | Local service area Phase 3 |
| P4 | Cannibalization detection (v2) | Topic clustering |
| P4 | Content atomization (social variants) | Article generation |

---

*Last updated: 2026-06-10 — consolidated from session competitive research (Frase, NeuronWriter, Clearscope, Semrush, MarketMuse).*

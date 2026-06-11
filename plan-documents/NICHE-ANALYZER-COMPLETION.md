# Niche Analyzer — Completion Plan
*Last updated: 2026-06-11*

What exists, what's broken, what's missing, and build order.

---

## What Works Today

| Feature | Status |
|---------|--------|
| Pillar discovery (schema, sitemap, crawl, nav, GSC) | ✓ Shipped |
| SERP validation per pillar (Serper.dev) | ✓ Shipped |
| National + local SERP split | ✓ Shipped |
| PAA + related searches per pillar (stored) | ✓ Shipped — needs re-run |
| Competitor domain extraction | ✓ Shipped |
| Competitor domain blocklist | ✓ Shipped |
| Competitor scope tagging (national/local/both) | ✓ Shipped |
| SERP insights UI panel (national vs local) | ✓ Shipped — needs re-run |
| Profile dedup (no more duplicate runs) | ✓ Fixed 2026-06-11 |
| Step log accurate pillar count | ✓ Fixed 2026-06-11 |

---

## Broken Right Now

| Issue | Fix |
|-------|-----|
| All PAA/SERP data empty — pillars analyzed pre-Serper.dev | Re-run analysis after deploy |
| `DefaultLocation = "United States"` on project | Set to `"Fort Lauderdale, Florida, United States"` in project settings |
| Historical duplicate profiles (5 runs × 63 pillars = 315 rows) | Run cleanup SQL below |
| Frontend not redeployed | Vercel deploy |

**Cleanup SQL** (run once in Railway DB):
```sql
-- Keep only the latest complete profile per project, supersede the rest
WITH ranked AS (
  SELECT "Id", "ProjectId",
    ROW_NUMBER() OVER (PARTITION BY "ProjectId" ORDER BY "AnalyzedAt" DESC NULLS LAST, "CreatedAt" DESC) AS rn
  FROM geek_seo.niche_profiles
  WHERE "Status" = 'complete'
)
UPDATE geek_seo.niche_profiles
SET "Status" = 'superseded'
WHERE "Id" IN (SELECT "Id" FROM ranked WHERE rn > 1);
```

---

## Missing — Should Be Built

### 1. Competitor Page Crawling (P0 — blocks everything below)

Competitor domains are stored but never fetched. No content = no topic model.

**What to build:**
- `CompetitorPageCrawler` service — given a list of competitor domains from `NicheCompetitor`, fetch their ranking pages for each pillar keyword
- Use existing `SitePageCrawler` + `PlaywrightCrawlerProvider` infrastructure
- Fetch top 3 organic results per pillar from `SeoSerpResult` (already cached from SERP validation)
- Extract: H1/H2/H3 headings, body text, word count, FAQ schema presence
- Store in existing `SeoCompetitorPage` entity (already has all needed columns)

**Data already available:** `SeoSerpResult.ResultsJson` has the organic URLs per keyword — no extra SERP call needed.

**Cost:** Playwright crawls, no API cost.

---

### 2. Topic Co-occurrence Model (P1 — needs #1)

**What to build:**
- `TopicModelBuilder` service — reads crawled competitor pages, extracts NLP terms
- Co-occurrence: term appears in 40%+ of top-10 results → required; 20%+ → important; 10%+ → supplemental
- Cache per keyword+geo in `seo_serp_deep_cache` or new `topic_models` table
- Claude prompt: extract entities + headings from each competitor page → return term JSON

**Output:** `TopicModel` with required/important/optional term lists per pillar

---

### 3. Content Brief Generator (P1 — needs #1 + #2)

**What to build:**
- Extend `SeoContentDocument` or create `SeoContentBrief` entity with:
  - `RequiredSubtopicsJson`
  - `SuggestedH2sJson`
  - `PaaQuestionsJson` (from pillar PAA)
  - `LocalPaaQuestionsJson`
  - `CompetitorBenchmarksJson`
  - `TargetWordCount`
  - `LocalEntitiesJson`
- API endpoint: `POST /api/seo/content-brief/generate` — takes pillar ID → returns brief
- Claude system prompt injects: required subtopics + PAA questions + local entities + competitor word count targets

---

### 4. Scoring v2 — SERP-Grounded Term Analysis (P1 — needs #2)

**What to build:**
- Replace `ContentScoringService` v1 (keyword word-split) with term coverage against topic model
- Editor sidebar: term table with used/missing/recommended count + fill bars
- Real-time score updates as terms are added
- Letter grade A++ → F (not raw 0–100)
- Add Flesch-Kincaid readability as scored dimension

**Spec:** `plan-documents/CONTENT-WRITER-FEATURES.md` Feature 1

---

### 5. Project DefaultLocation UI (P0 — non-code)

**Problem:** `SeoProject.DefaultLocation = "United States"` default means local SERP never fires.

**Fix:** Project settings page must expose `DefaultLocation` field. User sets it once to their market. SERP validation uses it automatically.

Currently the column exists on `seo_projects`. No UI to edit it.

**Build:** Add `DefaultLocation` + `ServiceRadiusMiles` inputs to project settings form. Wire to `PUT /api/projects/{id}`.

---

### 6. Competitor Fetch Pipeline in Niche Analyzer (P1)

Wire competitor page crawling into the niche analysis flow as Step 10 (between content coverage and authority scoring):

```
Step 9  — SERP validation (competitor domains identified)
Step 10 — Competitor page fetch (NEW — crawl top 3 pages per pillar)
Step 11 — Local geography
Step 12 — Content coverage
Step 13 — Authority scoring
Step 14 — Complete
```

Store results in `seo_competitor_pages` (entity already exists). Feed into topic model builder.

---

### 7. Site Content Audit (P2 — needs GSC OAuth #27)

**What to build:**
- GSC-driven page inventory: all indexed URLs + their top queries, clicks, impressions, position
- Score each page against its topic model
- ROI-sorted action list: `HIGH_ROI_OPTIMIZE` (low score + high traffic), `EXPAND`, `MAINTAIN`, `MONITOR`
- Striking distance alerts: pages at position 7–15 missing 2–3 required subtopics

**Spec:** `plan-documents/CONTENT-WRITER-FEATURES.md` Feature 5

---

### 8. AI Visibility Tracking — Multi-LLM (P3)

Extend existing `SeoGeoTrackingQuery` / `SeoGeoMentionSnapshot` to query ChatGPT, Perplexity, Gemini (not just Google AIO).

Track: appearance rate, authority rate, share of voice vs competitors.

**Spec:** `plan-documents/CONTENT-WRITER-FEATURES.md` Feature 6

---

## Build Order

```
NOW
  ├── Fix DefaultLocation UI (project settings)
  ├── Cleanup duplicate profiles (SQL above)
  └── Re-run analysis (geekatyourspot.com)

NEXT — P0
  └── Competitor page crawling (#1)
      → feeds everything below

THEN — P1
  ├── Topic co-occurrence model (#2)
  ├── Content brief generator (#3)
  └── Scoring v2 (#4)

LATER — P2
  └── Site content audit (#7) — needs GSC OAuth first

LATER — P3
  └── Multi-LLM GEO tracking (#8)
```

---

## Notes

- Competitor page fetching has zero API cost — uses existing Playwright infra
- `SeoCompetitorPage` entity already has all needed columns (headings, word count, structured data)
- `SeoSerpResult` already caches the organic URLs — no extra SERP spend to get competitor page URLs
- All PAA questions from Serper.dev now stored per pillar — feed directly into brief generation

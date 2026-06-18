# SERP Research Agent Prompt — Content Writing Input Pack

**Purpose:** Copy-paste prompt for an external research agent. Output is a **SERP Research Pack** (JSON) that feeds Geek SEO Content Writing — **not** Niche Analyzer.

**Related:** [`content-writing-prompt.md`](./content-writing-prompt.md), [`CONTENT-WRITING-PROMPT-SPEC.md`](./CONTENT-WRITING-PROMPT-SPEC.md)

**Recovered:** 2026-06-17 (session interrupted before this file was saved)

---

## Why this exists

Content Writing ranks on **keyword + location + live SERP evidence**, not a site-wide niche profile.

| Signal | Code | What it drives |
|--------|------|----------------|
| **PAA** | People Also Ask | Closing FAQ (5), overflow H3s, GEO structure |
| **PASF** | People Also Search For | Related searches → semantic terms, extra subtopics |
| **PAF** | Primary Answer Feature | Featured snippet / AI Overview / answer box → direct-answer block + format |

Niche Analyzer is **not** on the must-have list for producing rank-ready content.

---

## Content Writing improvement plan (decoupled from Niche Analyzer)

### Principle

**One input contract:** keyword + location + **SERP Research Pack** + **Competitor Page Pack** → brief → outline → draft → score → publish.

Project record keeps only: business name, URL, location, author/org for schema.

### Phase 1 — SERP Research Pack (highest ROI)

- Add `SerpResearchPack` import (JSON paste or file upload on brief step)
- Brief builds FAQ from **pack PAA first**, then pack extra questions, then templates
- `RecommendedTerms` from PASF + competitor headings + AI on snippets
- Drop `NicheContext` from brief UI and prompts

### Phase 2 — Map SERP signals into structure

| Signal | Use |
|--------|-----|
| PAA | Top 5 → closing FAQ; overflow → H3s under relevant movement |
| PASF | Semantic terms + optional extra H3s |
| PAF | Direct-answer block: beat or match format (paragraph / list / table) |
| Organic titles | Title length benchmark + variants |
| Competitor H2–H4 | Heading families per methodology movement |
| Intent | Movement emphasis + word-count target |

### Phase 3 — Scoring aligned to pack (not niche)

Term coverage, word count, title/meta, GEO structure, GEO depth, citations — all tied to pack fields.

### What to cut from Content Writing when trimming Niche Analyzer

- `NicheContext` on `ContentBrief`
- Gap topics in FAQ builder
- Niche tags → schema software (keep keyword-based extraction only)
- Brief UI “Optional niche context” card

---

## Minimum must-haves to produce content that ranks

1. Exact **target keyword** + **location**
2. Live SERP: top 10 organic + **PAA** + **PASF** + **PAF**
3. **Competitor outlines** (H1–H3) for top 3–5
4. **Word-count benchmark** from those pages
5. **5 FAQ questions** (PAA-first)
6. **8–12 semantic terms** from SERP language
7. **Four movements** with topic-specific H2s from SERP gaps
8. **Direct answer** that beats or matches PAF format
9. **Title + meta** near SERP median length
10. **FAQPage + TechArticle schema** on export

---

## Agent prompt (copy everything below)

```markdown
# Mission

You are a **SERP research agent** for content writing. Your job is NOT to write the article. You gather **live, keyword-specific search intelligence** so a content-writing system can build a brief, outline, and draft that match what Google is already rewarding.

Do not invent data. If you cannot access live SERP results, say so and stop — do not hallucinate PAA or rankings.

---

## Inputs (required from user)

1. **Primary keyword** (exact phrase to target): `{KEYWORD}`
2. **Search location** (city/region/country as Google would use): `{LOCATION}`
3. **Language**: `en` (unless specified)
4. **Business context** (2–4 sentences): who they are, what they sell, service area — for intent filtering only, NOT for replacing SERP facts
5. **Optional:** 3–5 competitor URLs they care about (if SERP top 10 is blocked)

---

## Acronyms (collect all)

| Code | Meaning | What to capture |
|------|---------|-----------------|
| **PAA** | People Also Ask | Every visible question; expand PAA boxes if possible (nested questions too) |
| **PASF** | People Also Search For | Related searches at bottom of SERP (and "Searches related to" variants) |
| **PAF** | Primary Answer Feature | The main instant-answer block: featured snippet, AI Overview summary, knowledge panel text, or "answer box" — whichever dominates above the fold. Include **format** (paragraph, list, table, video) and **verbatim or near-verbatim text** |

Also note other **SERP features**: local pack, ads, videos, forums (Reddit), shopping, sitelinks, FAQ rich results on competitors.

---

## Research steps

1. Run a **desktop web search** for `{KEYWORD}` with localization `{LOCATION}`.
2. Record **top 10 organic results**: position, URL, title, meta description if visible, estimated content type (guide, service page, tool, forum).
3. For **positions 1–5**, open pages and extract:
   - H1
   - All H2 and H3 in order (outline)
   - Approximate word count (estimate from scroll depth or word count tool if available)
   - Schema types if visible (FAQPage, HowTo, Article, LocalBusiness, etc.)
4. Collect **PAA** (all questions; note if answers are shown in SERP).
5. Collect **PASF** (all related searches).
6. Capture **PAF** (featured snippet / AI overview / answer box): text + format + source URL if cited.
7. Infer **search intent**: informational | commercial | transactional | local — with one-sentence justification from SERP mix.
8. Propose **5 closing FAQ questions** for the article: prioritize real PAA; only invent if fewer than 5 exist, and mark invented ones `"source": "suggested"`.
9. Propose **8–12 semantic terms/phrases** a ranking page should include (from snippets, headings, PASF — not generic fluff).
10. Propose **target word count** = median of top-5 competitor word counts (or reasonable estimate with note).

---

## Output format

Return **only** valid JSON (no markdown fences in your final message) matching this schema:

{
  "meta": {
    "keyword": "",
    "location": "",
    "language": "en",
    "researchedAt": "ISO-8601 datetime",
    "searchEngine": "Google",
    "device": "desktop",
    "dataQuality": "live | partial | unavailable",
    "notes": []
  },
  "intent": {
    "primary": "informational | commercial | transactional | local",
    "justification": ""
  },
  "paf": {
    "type": "featured_snippet | ai_overview | knowledge_panel | none",
    "format": "paragraph | list | table | video | mixed",
    "text": "",
    "sourceUrl": "",
    "beatStrategy": "one sentence: how to outperform this answer"
  },
  "paa": [
    { "question": "", "serpAnswerPreview": "", "depth": 1 }
  ],
  "pasf": ["related search 1", "related search 2"],
  "serpFeatures": ["local_pack", "ads", "videos", "forums", "shopping", "faq_rich_results"],
  "organic": [
    {
      "position": 1,
      "url": "",
      "domain": "",
      "title": "",
      "snippet": "",
      "contentType": "guide | service | product | forum | other"
    }
  ],
  "competitorOutlines": [
    {
      "url": "",
      "position": 1,
      "h1": "",
      "headings": [{ "level": 2, "text": "" }],
      "estimatedWordCount": 0,
      "schemaTypes": []
    }
  ],
  "benchmarks": {
    "medianWordCountTop5": 0,
    "medianTitleLengthTop10": 0,
    "dominantContentFormat": "long_guide | comparison | how_to | local_service | mixed"
  },
  "recommendedTerms": ["phrase1", "phrase2"],
  "closingFaqQuestions": [
    { "question": "", "source": "paa | pasf | suggested" }
  ],
  "directAnswerBlock": {
    "instruction": "How the article should open (definition + outcome in 2-3 sentences)",
    "mustBeatPaf": true
  },
  "methodologyHints": [
    {
      "movement": 1,
      "label": "Business Objectives",
      "suggestedH2": "topic-specific h2 for this keyword",
      "subtopicsFromSerp": ["h3 ideas from gaps/PAA"]
    },
    {
      "movement": 2,
      "label": "Data Quality Assessment",
      "suggestedH2": "",
      "subtopicsFromSerp": []
    },
    {
      "movement": 3,
      "label": "Tech Selection",
      "suggestedH2": "",
      "subtopicsFromSerp": []
    },
    {
      "movement": 4,
      "label": "Pilot Implementation Strategy",
      "suggestedH2": "",
      "subtopicsFromSerp": []
    },
    {
      "movement": 5,
      "label": "Scaling Safety",
      "suggestedH2": "",
      "subtopicsFromSerp": []
    }
  ]
}

---

## Quality rules

- Prefer **verbatim SERP strings** for PAA, PASF, titles, PAF text.
- If two questions are duplicates, dedupe.
- Flag **low confidence** in `meta.notes` (e.g. geo bias, personalized results, blocked crawl).
- Do **not** use a site-wide "niche analysis" or business guess to fill SERP fields.
- `closingFaqQuestions` must be exactly **5** items when possible.

---

## Handoff line (include at end)

> Research pack complete for `{KEYWORD}` @ `{LOCATION}`. Feed this JSON to Geek SEO Content Writing (brief import) or to the writing agent for outline + draft generation.
```

---

## How to use today (until brief import is built)

1. Run the agent with your keyword + location → get JSON.
2. Start brief in Geek SEO (`/app/content-writing`) — SERP API still runs in parallel.
3. Paste back to the writing agent: **PAA list, PASF list, PAF text, top 5 competitor headings, 5 FAQ picks** — or paste the full JSON once Phase 1 import ships.

---

*Last updated: 2026-06-17*
